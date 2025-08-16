using System.Drawing;
using System.IO;

namespace BiblioTrieca
{
    public interface TrieDatabase
    {
        void AddElement(string key, byte[] data, bool replace = true);
        byte[] ReadElement(string key);
        void RemoveElement(string key);
        int[] ReadMetadata(string key);
    }

    public class TrieInFile:TrieDatabase
    {
        const int recordLength = 256;
        //(26 chars + 10 numericals) * 4B + 111B data = 256 B

        byte[] emptyRecord = new byte[256];

        string adress;
        public uint nmbRecordsInDB; //In reality is one less than the true number of records in DB, shows last active record index

        int cacheSize;
        string[] cacheKeys;
        byte[][] cacheValues;
        int activeCacheIndex = 0;


        public TrieInFile(string adress, int cacheSize = 0)
        {
            this.adress = adress;
            this.cacheSize = cacheSize;
            if (File.Exists(adress))
            {
                this.nmbRecordsInDB = (uint)new System.IO.FileInfo(adress).Length/256-1;
            }
            else
            {
                this.nmbRecordsInDB = 0;
                FileStream fileStream = new FileStream(adress, FileMode.OpenOrCreate, FileAccess.Write);
                BinaryWriter writer = new BinaryWriter(fileStream);
                writer.Write(emptyRecord);
                writer.Close();
                fileStream.Close();
            }
            if (cacheSize != 0)
            {
                cacheKeys = new string[cacheSize];
                cacheValues = new byte[cacheSize][];
            }
        }

        private static int CharToIndex(char c)
        //Based on char in key return byte offset in record
        //Doesnt distinguish between capitalized letters
        {
            if(c > 47 && c < 58)
            {
                return (c - 48);
            }
            if(c > 64 && c < 91)
            {
                return (c - 55);
            }
            if(c > 96 && c < 123)
            {
                return (c - 87);
            }
            throw new Exception("invalid character in key");
        }

        private uint KeyToRecordIndex(string key)
        //Navigates through the trie and returns index of record with designated key (or zero if record doesnt exist)
        {
            uint activeRecordIndex = 0;
            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fileStream);

                //Navigating to designated record
                for (int keyCharIndex = 0; keyCharIndex < key.Length; keyCharIndex++)
                {
                    char activeChar = key[keyCharIndex];
                    long charDataByteOffset = CharToIndex(activeChar) * 4 + activeRecordIndex * recordLength;
                    fileStream.Position = charDataByteOffset;
                    uint nextRecordIndex = Convert.ToUInt32(reader.ReadInt32());
                    if (nextRecordIndex != 0)
                    //If the way exists
                    {
                        activeRecordIndex = nextRecordIndex;
                    }
                    else
                    {
                        reader.Close();
                        return 0;
                    }
                }
                reader.Close();
                return activeRecordIndex;
            }
        }

        public void AddElement(string key, byte[] data, bool replace = true)
        {
            uint activeRecordIndex = 0;
            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryWriter writer = new BinaryWriter(fileStream);
                BinaryReader reader = new BinaryReader(fileStream);
                
                //First navigate to the destination and create records on the way if necessary
                //KeyToIndexRecordIndex cant be used, because here we also need to create new records in the navigation process
                for (int keyCharIndex = 0; keyCharIndex < key.Length; keyCharIndex++)
                {
                    char activeChar = key[keyCharIndex];
                    long charDataByteOffset = CharToIndex(activeChar)*4 + activeRecordIndex*recordLength;
                    fileStream.Position = charDataByteOffset;
                    uint nextRecordIndex = Convert.ToUInt32(reader.ReadInt32());
                    if (nextRecordIndex == 0)
                    //If there is no record that continues this way, we have to create it
                    {
                        //First update metas in old record
                        fileStream.Position = charDataByteOffset;
                        writer.Write(nmbRecordsInDB+1);
                        //Move to end of file
                        fileStream.Position = nmbRecordsInDB * recordLength + recordLength;
                        //Create new empty record
                        writer.Write(emptyRecord);
                        nmbRecordsInDB++;
                        activeRecordIndex = nmbRecordsInDB;
                    }
                    else
                    //If there is, then just continue to it
                    {
                        activeRecordIndex = nextRecordIndex;
                    }
                }
                //Jump to the data section of the record
                fileStream.Position = activeRecordIndex * recordLength + 4 * 36;
                byte indicationByte = reader.ReadByte();
                if (indicationByte == 0)
                {
                    //Set the indication byte
                    fileStream.Position = activeRecordIndex * recordLength + 4 * 36;
                    writer.Write((byte)1);
                    //Then write the data in designated record
                    writer.Write(data);
                }
                else
                {
                    if (replace)
                    {
                        writer.Write(data);
                    }
                }
                writer.Close();
                reader.Close();
            }
        }
        
        public byte[] ReadElement(string key)
        {
            uint activeRecordIndex = 0;

            //Navigating to designated record
            activeRecordIndex = KeyToRecordIndex(key);

            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fileStream);

                if (activeRecordIndex == 0 && key != "")
                //If record doesnt exist
                {
                    return null; 
                }

                //Jump to data section of the record
                fileStream.Position = activeRecordIndex * recordLength + 4 * 36;
                if(reader.ReadByte() == 1)
                //If the record isnt deleted -> its indication byte is set
                {
                    //Then read data from the record
                    byte[] recordData = reader.ReadBytes(111);
                    //111 is the number of data bytes in each record
                    reader.Close();

                    //If cache is activated, then add this element to it
                    if (cacheSize != 0)
                    {
                        activeCacheIndex = (activeCacheIndex + 1) % cacheSize;
                        cacheKeys[activeCacheIndex] = key;
                        cacheValues[activeCacheIndex] = recordData;
                    }
                    return recordData;
                }
                else
                {
                    reader.Close();
                    return null;
                }
                
            }
        }
        
        public byte[] ReadTryCache(string key)
        {
            //Go through cache keys and try to find cache hit
            for (int i = 0; i < cacheSize; i++)
            {
                if (cacheKeys[i] == key)
                {
                    return cacheValues[i];
                }
            }
            //If there isnt one then start standart procedure
            return ReadElement(key);
        }
        
        public void RemoveElement(string key)
        {
            uint activeRecordIndex = 0;

            //Navigating to designated record
            activeRecordIndex = KeyToRecordIndex(key);

            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryReader reader = new BinaryReader(fileStream);
                BinaryWriter writer = new BinaryWriter(fileStream);
                if (activeRecordIndex != 0 || key == "")
                //If record exists
                {
                    //Jump to data section of the record
                    fileStream.Position = activeRecordIndex * recordLength + 4 * 36;
                    //Clear the indication byte
                    writer.Write((byte)0);
                    reader.Close();
                    writer.Close();
                }
            }
        }
        
        public int[] ReadMetadata(string key)
        //Returns record indexes of children and indication byte (all as integers)
        {
            uint activeRecordIndex = 0;

            //Navigating to designated record
            activeRecordIndex = KeyToRecordIndex(key);

            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fileStream);

                int[] metas = new int[37];

                if (activeRecordIndex == 0 && key != "")
                //If record doesnt exist
                {
                    reader.Close();
                    return null;
                }
                else
                {
                    //Jump to the indication byte section of the record
                    fileStream.Position = activeRecordIndex * recordLength + 36 * 4;
                    metas[0] = Convert.ToInt32(reader.ReadByte());
                    //Jump to the metadata section of the record
                    fileStream.Position = activeRecordIndex * recordLength;
                    for (int i = 0; i < 36; i++)
                    {
                        metas[i + 1] = reader.ReadInt32();
                    }
                    reader.Close();
                    return metas;
                }

            }
        }
    
        public void RemoveBranch(string key)
        //Invalidates record with this key, and all records which start with this key (using BFS)
        {   
            System.Collections.Generic.Queue<uint> bfsQueue = new Queue<uint>();
            bfsQueue.Enqueue(KeyToRecordIndex(key));

            while (bfsQueue.Count > 0)
            {
                uint activeIndex = bfsQueue.Dequeue();
                using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.ReadWrite))
                {
                    BinaryReader reader = new BinaryReader(fileStream);
                    BinaryWriter writer = new BinaryWriter(fileStream);

                    //First clear indication byte of this record
                    fileStream.Position = activeIndex*recordLength + 36*4;
                    writer.Write((byte)0);

                    //Then load info about this records children and add to queue
                    fileStream.Position = activeIndex*recordLength;
                    for (int i = 0; i<36; i++)
                    {
                        uint childIndex = reader.ReadUInt32();
                        if(childIndex != 0)
                        {
                            bfsQueue.Enqueue((uint)childIndex);
                        }
                    }
                    writer.Close();
                }
            }
        }

        public uint BranchSize(string key)
        //Returns number of active (having data) records in this branch
        {
            uint size = 0;
            System.Collections.Generic.Queue<uint> bfsQueue = new Queue<uint>();
            bfsQueue.Enqueue(KeyToRecordIndex(key));

            while (bfsQueue.Count > 0)
            {
                uint activeIndex = bfsQueue.Dequeue();
                using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.Read))
                {
                    BinaryReader reader = new BinaryReader(fileStream);

                    //First increment size if indication byte is set
                    fileStream.Position = activeIndex * recordLength + 36 * 4;
                    if(reader.ReadByte() != 0)
                    {
                        size++;
                    }

                    //Then load info about this records children and add to queue
                    fileStream.Position = activeIndex * recordLength;
                    for (int i = 0; i < 36; i++)
                    {
                        uint childIndex = reader.ReadUInt32();
                        if (childIndex != 0)
                        {
                            bfsQueue.Enqueue((uint)childIndex);
                        }
                    }
                }
            }
            return size;
        }
    
        public string[] AutoComplete(string key, int numberOfCompletions)
        {
            char[] charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];
            
            //For autocomplete not to return own value
            numberOfCompletions++;

            string[] completions = new string[numberOfCompletions];
            int completed = 0;


            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0 && completed<numberOfCompletions)
            {
                string activeKey = bfsQueue.Dequeue();
                int[] activeMetas = this.ReadMetadata(activeKey);
                
                //Check if record is active (indication byte set)
                if(activeMetas[0] == 1)
                {
                    //If so, add it to completions
                    completions[completed] = activeKey;
                    completed++;
                }

                //Then continue to children
                for(int i = 1; i < activeMetas.Length; i++)
                {
                    if(activeMetas[i] != 0) //If child exists
                    {
                        bfsQueue.Enqueue(activeKey+charsInRecord[i-1]);
                    }
                }
            }
            return completions[1..];
        }
        
        public void ConsolePrint(string key, int depth, int recursionDepth = 0)
        //Uses DFS (recursional) to go through subtree and on its way draws the tree
        //Only shows keys, because showing data was confusing, displays whether record has some data
        {
            char[] charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

            for (int i = 0; i < recursionDepth; i++)
            {
                Console.Write("        |");
            }
            Console.Write("> - ");
            Console.Write(key);
            Console.Write(" - ");
            if(this.ReadElement(key) != null)
            {
                Console.Write("HAS DATA\n");
            }
            else
            {
                Console.Write("NO DATA\n");
            }
            if(depth > 0)
            {
                int[] metas = this.ReadMetadata(key);
                for (int i = 1; i < metas.Length; i++)
                {
                    if (metas[i] != 0) //If child exists
                    {
                        this.ConsolePrint(key + charsInRecord[i-1], depth-1, recursionDepth+1);
                    }
                }
            }
        }

        
        public void GarbageCollector()
        //Goes through the file and copies all non-empty branches into a new file
        //This way all records in empty branches are removed
        {
            char[] charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

            //Create new trie
            TrieInFile newTrie = new TrieInFile(adress + "_new");

            //Now use BFS and add every active element from the old to the new trie
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");
            while(bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                int[] activeMetas = this.ReadMetadata(activeKey);
                byte[] activeData = this.ReadElement(activeKey);

                //Check if record is active (indication byte set)
                if (activeMetas[0] == 1)
                {
                    //If so, add it to the new trie
                    newTrie.AddElement(activeKey, activeData);
                }

                for (int i = 1; i < 36; i++)
                {
                    if (activeMetas[i] != 0) //If child exists
                    {
                        bfsQueue.Enqueue(activeKey + charsInRecord[i - 1]);
                    }
                }
            }

            //Now we have new file created which is garbage collected version of the old one
            //We delete the old one and rename the new to the old
            File.Delete(adress);
            File.Move(adress + "_new", adress);
            this.nmbRecordsInDB = (uint)new System.IO.FileInfo(adress).Length / 256 -1;
        }
    }
}
