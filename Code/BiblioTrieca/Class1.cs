using System.Drawing;
using System.IO;
using log4net;
using System.Reflection;

namespace BiblioTrieca
{
    public interface TrieDatabase
    {
        void AddElement(string key, byte[] data, bool replace = true);
        byte[] ReadElement(string key);
        void RemoveElement(string key);
        void RemoveBranch(string key);
        uint BranchSize(string key);
        string[] AutoComplete(string key, int numberOfCompletions);
    }

    public class TrieInFile : TrieDatabase
    //Holds records in file, some methods require something like log(keysize) memory in RAM (garbagecollect, autocomplete...)
    //Includes simple cache, which is recommended, but due to prefix trees works very fast even without it
    {
        //added for sw dev tools class
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        uint recordLength;
        //(26 chars + 10 numericals + space) * 4B + activation Byte + 107B data = 256 B

        char[] charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '];

        uint byteSizeForChar = 4; //How many bytes are there for each char in record

        bool bitWise;

        byte[] emptyRecord;

        string adress;
        public uint nmbRecordsInDB; //In reality is one less than the true number of records in DB, shows last active record index

        int cacheSize;
        string[] cacheKeys;
        byte[][] cacheValues;
        int activeCacheIndex = 0;


        public TrieInFile(string adress, int cacheSize = 0, bool bitWise = false)
        {
            this.adress = adress;
            this.cacheSize = cacheSize;
            this.bitWise = bitWise;
            if (bitWise)
            {
                this.recordLength = 128;
                this.charsInRecord = ['0', '1'];
                emptyRecord = new byte[this.recordLength];
            }
            else
            {
                this.recordLength = 256;
                this.charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '];
                emptyRecord = new byte[this.recordLength];
            }

            try
            {
                if (File.Exists(adress))
                {
                    this.nmbRecordsInDB = (uint)new System.IO.FileInfo(adress).Length / recordLength - 1;
                    log.Info($"Existing trie loaded from disc: {adress}. Nmb of entries: {nmbRecordsInDB}");
                }
                else
                {
                    this.nmbRecordsInDB = 0;
                    FileStream fileStream = new FileStream(adress, FileMode.OpenOrCreate, FileAccess.Write);
                    BinaryWriter writer = new BinaryWriter(fileStream);
                    writer.Write(emptyRecord);
                    writer.Close();
                    fileStream.Close();

                    log.Info($"New trie created in file: {adress}.");
                }
            }
            catch (Exception ex)
            {
                log.Fatal($"Critical error when loading trie from file: {adress}.", ex);
                throw;
            }

            if (cacheSize != 0)
            {
                cacheKeys = new string[cacheSize];
                cacheValues = new byte[cacheSize][];
            }

            this.bitWise = bitWise;
        }

        private int CharToIndex(char c)
        //Based on char in key return byte offset in record
        //Doesnt distinguish between capitalized letters
        {
            if (this.bitWise)
            {
                if (c == '0')
                {
                    return 0;
                }
                if (c == '1')
                {
                    return 1;
                }
                else
                {
                    throw new Exception("invalid character in key");
                }
            }
            else
            {
                if (c > 47 && c < 58)
                {
                    return (c - 48);
                }
                if (c > 64 && c < 91)
                {
                    return (c - 55);
                }
                if (c > 96 && c < 123)
                {
                    return (c - 87);
                }
                if (c == 32)
                {
                    return (c + 4);
                }
                log.Error($"Invalid character usage: '{c}'");
                throw new Exception("invalid character in key");
            }
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
                    long charDataByteOffset = CharToIndex(activeChar) * byteSizeForChar + activeRecordIndex * recordLength;
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
            if (data == null || data.Length <= recordLength - byteSizeForChar * charsInRecord.Length - 1)
            //If data fits in the record
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
                        long charDataByteOffset = CharToIndex(activeChar) * byteSizeForChar + activeRecordIndex * recordLength;
                        fileStream.Position = charDataByteOffset;
                        uint nextRecordIndex = Convert.ToUInt32(reader.ReadInt32());
                        if (nextRecordIndex == 0)
                        //If there is no record that continues this way, we have to create it
                        {
                            //First update metas in old record
                            fileStream.Position = charDataByteOffset;
                            writer.Write(nmbRecordsInDB + 1);
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
                    fileStream.Position = activeRecordIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                    byte indicationByte = reader.ReadByte();
                    if (indicationByte == 0)
                    {
                        //Set the indication byte
                        fileStream.Position = activeRecordIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                        if(data != null)
                        {
                            writer.Write((byte)1);
                            //Then write the data in designated record
                            writer.Write(data);
                        }
                        else
                        {
                            writer.Write((byte)0);
                        }
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
            else
            {
                log.Error($"Too big data for a key '{key}'. Size: {data.Length}B.");
                throw new Exception("Not enough space in record, potentional data overflow");
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
                fileStream.Position = activeRecordIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                if (reader.ReadByte() == 1)
                //If the record isnt deleted -> its indication byte is set
                {
                    //Then read data from the record
                    byte[] recordData = reader.ReadBytes((int)(recordLength -byteSizeForChar*charsInRecord.Length-1));
                    //calculated number is the number of data bytes in each record
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
                    fileStream.Position = activeRecordIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                    //Clear the indication byte
                    writer.Write((byte)0);
                    reader.Close();
                    writer.Close();
                }
            }
        }

        public uint[] ReadMetadata(string key)
        //Returns record indexes of children and indication byte (all as integers)
        {
            uint activeRecordIndex = 0;

            //Navigating to designated record
            activeRecordIndex = KeyToRecordIndex(key);

            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.Read))
            {
                BinaryReader reader = new BinaryReader(fileStream);

                uint[] metas = new uint[1+charsInRecord.Length];

                if (activeRecordIndex == 0 && key != "")
                //If record doesnt exist
                {
                    reader.Close();
                    return null;
                }
                else
                {
                    //Jump to the indication byte section of the record
                    fileStream.Position = activeRecordIndex * recordLength + byteSizeForChar *charsInRecord.Length;
                    metas[0] = Convert.ToUInt32(reader.ReadByte());
                    //Jump to the metadata section of the record
                    fileStream.Position = activeRecordIndex * recordLength;
                    for (int i = 0; i < charsInRecord.Length; i++)
                    {
                        metas[i + 1] = reader.ReadUInt32();
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
                    fileStream.Position = activeIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                    writer.Write((byte)0);

                    //Then load info about this records children and add to queue
                    fileStream.Position = activeIndex * recordLength;
                    for (int i = 0; i < charsInRecord.Length; i++)
                    {
                        uint childIndex = reader.ReadUInt32();
                        if (childIndex != 0)
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
                    fileStream.Position = activeIndex * recordLength + byteSizeForChar * charsInRecord.Length;
                    if (reader.ReadByte() != 0)
                    {
                        size++;
                    }

                    //Then load info about this records children and add to queue
                    fileStream.Position = activeIndex * recordLength;
                    for (int i = 0; i < charsInRecord.Length; i++)
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
            //If record isnt in trie and so doesnt have any possible completions
            if(this.ReadMetadata(key) == null)
            {
                return null;
            }

            //For autocomplete not to return own value
            if (this.ReadMetadata(key)[0] == 1)
            {
                numberOfCompletions++;
            }
            string[] completions = new string[numberOfCompletions];
            int completed = 0;


            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0 && completed < numberOfCompletions)
            {
                string activeKey = bfsQueue.Dequeue();
                uint[] activeMetas = this.ReadMetadata(activeKey);

                //Check if record is active (indication byte set)
                if (activeMetas[0] == 1)
                {
                    //If so, add it to completions
                    completions[completed] = activeKey;
                    completed++;
                }

                //Then continue to children
                for (int i = 1; i < activeMetas.Length; i++)
                {
                    if (activeMetas[i] != 0) //If child exists
                    {
                        bfsQueue.Enqueue(activeKey + charsInRecord[i - 1]);
                    }
                }
            }
            if (this.ReadMetadata(key)[0] == 1)
            {
                return completions[1..];
            }
            return completions;
        }

        public void ConsolePrint(string key, int depth, int recursionDepth = 0)
        //Uses DFS (recursional) to go through subtree and on its way draws the tree
        //Only shows keys, because showing data was confusing, displays whether record has some data
        {
            for (int i = 0; i < recursionDepth; i++)
            {
                Console.Write("        |");
            }
            Console.Write("> - ");
            Console.Write(key);
            Console.Write(" - ");
            if (this.ReadElement(key) != null)
            {
                Console.Write("HAS DATA\n");
            }
            else
            {
                Console.Write("NO DATA\n");
            }
            if (depth > 0)
            {
                uint[] metas = this.ReadMetadata(key);
                for (int i = 1; i < metas.Length; i++)
                {
                    if (metas[i] != 0) //If child exists
                    {
                        this.ConsolePrint(key + charsInRecord[i - 1], depth - 1, recursionDepth + 1);
                    }
                }
            }
        }

        public void GarbageCollector()
        //Goes through the file and copies all non-empty branches into a new file
        //This way all records in empty branches are removed
        {
            //Create new trie (if bitwise, create bitwise, for compatibility)
            TrieInFile newTrie = new TrieInFile(adress + "_new", 0, bitWise);

                //Now use BFS and add every active element from the old to the new trie
                System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");
            while (bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                uint[] activeMetas = this.ReadMetadata(activeKey);
                byte[] activeData = this.ReadElement(activeKey);

                //Check if record is active (indication byte set)
                if (activeMetas[0] == 1)
                {
                    //If so, add it to the new trie
                    newTrie.AddElement(activeKey, activeData);
                }

                for (int i = 1; i < activeMetas.Length; i++)
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
            this.nmbRecordsInDB = (uint)new System.IO.FileInfo(adress).Length / recordLength - 1;
        }
    }

    public class TrieInRAM : TrieDatabase
    {
        bool bitWise;
        char[] charsInRecord;

        Record root; //Root of trie, representing ""

        public TrieInRAM(bool bitWise = false)
        {
            this.bitWise = bitWise;
            if (bitWise)
            {
                this.charsInRecord = ['0', '1'];
            }
            else
            {
                this.charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '];
                
            }
            this.root = new Record(bitWise);
        }

        public class Record
        {
            public byte[] data;
            public Record[] children;
            public bool active;

            char[] charsInRecord;

            public Record(bool bitWise)
            {
                this.active = false;
                if (bitWise)
                {
                    charsInRecord = ['0', '1'];
                }
                else
                {
                    charsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', ' '];
                }
                this.children = new Record[charsInRecord.Length];
            }
        }

        

        private static int CharToIndex(char c)
        //Based on char in key return index in record
        //Doesnt distinguish between capitalized letters
        {
            if (c > 47 && c < 58)
            {
                return (c - 48);
            }
            if (c > 64 && c < 91)
            {
                return (c - 55);
            }
            if (c > 96 && c < 123)
            {
                return (c - 87);
            }
            if(c == 32)
            {
                return (c + 4);
            }
            throw new Exception("invalid character in key");
        }
        public virtual void AddElement(string key, byte[] data, bool replace = true)
        {
            Record activeRecord = root;
            //First navigate to the correct record
            for (int i = 0; i < key.Length; i++)
            {
                int activeChildIndex = CharToIndex(key[i]);
                if (activeRecord.children[activeChildIndex] == null)
                //If there is no child continuing this way, then we have to create our way
                {
                    activeRecord.children[activeChildIndex] = new Record(this.bitWise);
                    activeRecord = activeRecord.children[activeChildIndex];
                }
                else
                //If child already exists, simply continue through it
                {
                    activeRecord = activeRecord.children[activeChildIndex];
                }
            }
            if (!activeRecord.active || replace)
            {
                //Then add the data
                activeRecord.data = data;
            }  
            //And set activation
            if(data != null)
            {
                activeRecord.active = true;
            }
        }
        public virtual Record KeyToRecord(string key)
        {
            Record activeRecord = root;
            for (int i = 0; i < key.Length; i++)
            {
                int activeChildIndex = CharToIndex(key[i]);
                if (activeRecord != null)
                {
                    activeRecord = activeRecord.children[activeChildIndex];
                }
                else
                {
                    return null;
                }
            }
            return activeRecord;
        }

        public byte[] ReadElement(string key)
        {
            Record destination = KeyToRecord(key);
            if (destination != null)
            {
                return destination.data;
            }
            else
            {
                return null;
            }
        }

        public void RemoveElement(string key)
        {
            Record destinationRecord = KeyToRecord(key);
            if (destinationRecord != null)
            {
                destinationRecord.data = null;
                destinationRecord.active = false;
            }
        }

        public void RemoveBranch(string key)
        //Deactivates designated key record and also all records with keys beginning this way (uses DFS)
        {
            Record activeRecord = KeyToRecord(key);

            System.Collections.Generic.Queue<Record> bfsQueue = new Queue<Record>();
            bfsQueue.Enqueue(activeRecord);

            while (bfsQueue.Count > 0)
            {
                activeRecord = bfsQueue.Dequeue();
                //First "remove" this record
                activeRecord.active = false;
                activeRecord.data = null;
                //Then continue to its children
                for (int i = 0; i < charsInRecord.Length; i++)
                {
                    if (activeRecord.children[i] != null)
                    {
                        bfsQueue.Enqueue(activeRecord.children[i]);
                    }
                }
            }
        }

        public uint BranchSize(string key)
        //Returns number of active data records in branch defined by root with this key
        {
            Record activeRecord = KeyToRecord(key);
            uint branchSize = 0;
            System.Collections.Generic.Queue<Record> bfsQueue = new Queue<Record>();
            bfsQueue.Enqueue(activeRecord);

            while (bfsQueue.Count > 0)
            {
                activeRecord = bfsQueue.Dequeue();
                if (activeRecord.active)
                {
                    branchSize++;
                }
                for (int i = 0; i < charsInRecord.Length; i++)
                {
                    if (activeRecord.children[i] != null)
                    {
                        bfsQueue.Enqueue(activeRecord.children[i]);
                    }
                }
            }
            return branchSize;
        }

        public string[] AutoComplete(string key, int numberOfCompletions)
        {
            //If record isnt in trie and so doesnt have any possible completions
            if (KeyToRecord(key) == null)
            {
                return null;
            }

            if (KeyToRecord(key).active)
            {
                numberOfCompletions++; //For it not to count key itself
            }

            int completed = 0;
            string[] completions = new string[numberOfCompletions];

            string activeKey = key;
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0 && completed < numberOfCompletions)
            {
                activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                if (activeRecord.active)
                {
                    completions[completed] = activeKey;
                    completed++;
                }
                for (int i = 0; i < charsInRecord.Length; i++)
                {
                    if (activeRecord.children[i] != null)
                    {
                        bfsQueue.Enqueue(activeKey + charsInRecord[i]);
                    }
                }
            }
            if (KeyToRecord(key).active)
            {
                return completions[1..];
            }
            return completions;

        }

        public void ConsolePrint(string key, int depth, int recursionDepth = 0)
        //Uses DFS (recursional) to go through subtree and on its way draws the tree
        //Only shows keys, because showing data was confusing, displays whether record has some data
        {
            for (int i = 0; i < recursionDepth; i++)
            {
                Console.Write("        |");
            }
            Console.Write("> - ");
            Console.Write(key);
            Console.Write(" - ");
            Record activeRecord = KeyToRecord(key);
            if (activeRecord.active)
            {
                Console.Write("HAS DATA\n");
            }
            else
            {
                Console.Write("NO DATA\n");
            }
            if (depth > 0)
            {
                for (int i = 0; i < charsInRecord.Length; i++)
                {
                    if (activeRecord.children[i] != null)
                    {
                        this.ConsolePrint(key + charsInRecord[i], depth - 1, recursionDepth + 1);
                    }
                }
            }
        }

        public void GarbageCollect()
        //Goes through the trie using BFS and cuts pointers to branches whose size is zero
        {
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");

            while(bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                for(int i = 0;i < activeRecord.children.Length; i++)
                {
                    if(activeRecord.children[i] != null)
                    //Foreach existing child
                    {
                        if (this.BranchSize(activeKey + charsInRecord[i]) == 0)
                        //If branch defined by this child doesnt have any active records
                        {
                            activeRecord.children[i] = null; //Then delete the pointer and let runtime GC take over
                        }
                        else
                        {
                            bfsQueue.Enqueue(activeKey + charsInRecord[i]);
                        }
                    }
                }
            }
        }

        public void SaveToFile(string adress, bool garbageCollect = true)
        //Saves all records to file on this adress, based on protocol for TrieInFile
        //Creates TrieInFile and using BFS add every element to it
        //If GB is disabled, adds even empty (non-data) branches to the file
        {
            if (garbageCollect)
            {
                this.GarbageCollect();
            }

            TrieInFile savedTrie = new TrieInFile(adress);

            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");
            while(bfsQueue.Count > 0)
            {
                //Add this record to the file
                string activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                savedTrie.AddElement(activeKey, activeRecord.data);

                //Continue to children
                for(int i = 0; i < activeRecord.children.Length; i++)
                {
                    if(activeRecord.children[i] != null)
                    {
                        bfsQueue.Enqueue(activeKey+charsInRecord[i]);
                    }
                }
            }
        }

        public void LoadFromFile(string adress)
        //Loads TrieInRAM from its binary (TrieInFile) file
        //Using BFS goes through all the TrieInFile and adds every record to this TrieInRAM
        {
            TrieInFile loadedTrie = new TrieInFile(adress);

            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");
            while (bfsQueue.Count > 0)
            {
                //First load activeRecord to TrieInRAM
                string activeKey = bfsQueue.Dequeue();
                byte[] activeData = loadedTrie.ReadElement(activeKey);
                this.AddElement(activeKey, activeData);

                //Then continue to children
                uint[] metas = loadedTrie.ReadMetadata(activeKey);
                for(int i = 1; i < metas.Length; i++) //we skip indication byte
                {
                    if (metas[i] != 0)
                    //If child exists
                    {
                        bfsQueue.Enqueue(activeKey + charsInRecord[i-1]);
                    }
                }
            }
        }

        public LinkedListRAMTrie ConvertToLinkedListBased(bool garbageCollect = true)
        {
            if (garbageCollect)
            {
                this.GarbageCollect();
            }

            LinkedListRAMTrie returnTrie = new LinkedListRAMTrie();

            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");
            while (bfsQueue.Count > 0)
            {
                //Add this record to the file
                string activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                returnTrie.AddElement(activeKey, activeRecord.data);

                //Continue to children
                for (int i = 0; i < activeRecord.children.Length; i++)
                {
                    if (activeRecord.children[i] != null)
                    {
                        bfsQueue.Enqueue(activeKey + charsInRecord[i]);
                    }
                }
            }

            return returnTrie;
        }
    }

    public class LinkedListRAMTrie : TrieDatabase
    {
        public class Record
        {
            public byte[] data;
            public System.Collections.Generic.LinkedList<Record> children;
            public bool active;
            public char character;

            public Record()
            {
                this.children = new System.Collections.Generic.LinkedList<Record>();
                this.active = false;
            }

            public Record(char c)
            {
                this.children = new System.Collections.Generic.LinkedList<Record>();
                this.active = false;
                this.character = c;
            }
        }

        Record root = new Record(); //Root of trie, representing ""

        private static Record CharToRecord(char c, Record record)
        //Based on char in key return record in this records linked list
        //Doesnt distinguish between capitalized letters
        {
            if (c > 64 && c < 91)
            {
                //If capitalized, decapitalize it
                int int_c = (int)c;
                int_c = int_c + 32;
                c = (char)int_c;
            }
            foreach(Record activeRecord in record.children)
            {
                if(activeRecord.character == c)
                {
                    return activeRecord;
                }
            }
            if((c > 47 && c < 58) || (c > 64 && c < 91) || (c > 96 && c < 123) || c == 32)
            //If character is in the list of possible characters
            {
                return null;
            }
            //Maybe to implement return null, if character is valid, but not in the linkedlist
            throw new Exception("invalid character in key");
        }

        public void AddElement(string key, byte[] data, bool replace = true)
        {
            Record activeRecord = root;
            //First navigate to the correct record
            for (int i = 0; i < key.Length; i++)
            {
                Record childRecord = CharToRecord(key[i], activeRecord);
                if (childRecord == null)
                //If there is no child continuing this way, then we have to create our way
                {
                    Record newRecord = new Record(key[i]);
                    activeRecord.children.AddFirst(newRecord);
                    activeRecord = newRecord;
                }
                else
                //If child already exists, simply continue through it
                {
                    activeRecord = childRecord;
                }
            }
            if (!activeRecord.active || replace)
            {
                //Then add the data
                activeRecord.data = data;
            }
            //And set activation
            if (data != null)
            {
                activeRecord.active = true;
            }
        }

        public Record KeyToRecord(string key)
        {
            Record activeRecord = root;
            for (int i = 0; i < key.Length; i++)
            {
                Record activeChild = CharToRecord(key[i], activeRecord);
                if (activeChild != null)
                {
                    activeRecord = activeChild;
                }
                else
                {
                    return null;
                }
            }
            return activeRecord;
        }

        public byte[] ReadElement(string key)
        {
            Record destination = KeyToRecord(key);
            if (destination != null)
            {
                return destination.data;
            }
            else
            {
                return null;
            }
        }

        public void RemoveElement(string key)
        {
            Record destinationRecord = KeyToRecord(key);
            if (destinationRecord != null)
            {
                destinationRecord.data = null;
                destinationRecord.active = false;
            }
        }

        public void RemoveBranch(string key)
        //Deactivates designated key record and also all records with keys beginning this way (uses DFS)
        {
            Record activeRecord = KeyToRecord(key);

            System.Collections.Generic.Queue<Record> bfsQueue = new Queue<Record>();
            bfsQueue.Enqueue(activeRecord);

            while (bfsQueue.Count > 0)
            {
                activeRecord = bfsQueue.Dequeue();
                //First "remove" this record
                activeRecord.active = false;
                activeRecord.data = null;
                //Then continue to its children
                foreach(Record activeChild in activeRecord.children)
                {
                    if (activeChild != null)
                    {
                        bfsQueue.Enqueue(activeChild);
                    }
                }
            }
        }

        public uint BranchSize(string key)
        //Returns number of active data records in branch defined by root with this key
        {
            Record activeRecord = KeyToRecord(key);
            uint branchSize = 0;
            System.Collections.Generic.Queue<Record> bfsQueue = new Queue<Record>();
            bfsQueue.Enqueue(activeRecord);

            while (bfsQueue.Count > 0)
            {
                activeRecord = bfsQueue.Dequeue();
                if (activeRecord.active)
                {
                    branchSize++;
                }
                foreach(Record activeChild in activeRecord.children)
                {
                    if (activeRecord != null)
                    {
                        bfsQueue.Enqueue(activeChild);
                    }
                }
            }
            return branchSize;
        }

        public string[] AutoComplete(string key, int numberOfCompletions)
        {
            //If record isnt in trie and so doesnt have any possible completions
            if (this.KeyToRecord(key) == null)
            {
                return null;
            }
            if (KeyToRecord(key).active)
            {
                numberOfCompletions++; //For it not to count key itself
            }

            int completed = 0;
            string[] completions = new string[numberOfCompletions];

            string activeKey = key;
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0 && completed < numberOfCompletions)
            {
                activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                if (activeRecord.active)
                {
                    completions[completed] = activeKey;
                    completed++;
                }
                foreach(Record activeChild in activeRecord.children)
                {
                    if (activeChild != null)
                    {
                        bfsQueue.Enqueue(activeKey + activeChild.character);
                    }
                }
            }
            if (KeyToRecord(key).active)
            {
                return completions[1..];
            }
            return completions;

        }

        public void ConsolePrint(string key, int depth, int recursionDepth = 0)
        //Uses DFS (recursional) to go through subtree and on its way draws the tree
        //Only shows keys, because showing data was confusing, displays whether record has some data
        {
            for (int i = 0; i < recursionDepth; i++)
            {
                Console.Write("        |");
            }
            Console.Write("> - ");
            Console.Write(key);
            Console.Write(" - ");
            Record activeRecord = KeyToRecord(key);
            if (activeRecord.active)
            {
                Console.Write("HAS DATA\n");
            }
            else
            {
                Console.Write("NO DATA\n");
            }
            if (depth > 0)
            {
                foreach(Record activeChild in activeRecord.children)
                {
                    if (activeChild != null)
                    {
                        this.ConsolePrint(key + activeChild.character, depth - 1, recursionDepth + 1);
                    }
                }
            }
        }

        public void GarbageCollect()
        //Goes through the trie using BFS and cuts pointers (and nodes) to branches whose size is zero
        {
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue("");

            while (bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                System.Collections.Generic.Queue<Record> deleteQueue = new Queue<Record>();
                foreach(Record activeChild in activeRecord.children)
                {
                    if (activeChild != null)
                    //Foreach existing child
                    {
                        if (this.BranchSize(activeKey + activeChild.character) == 0)
                        //If branch defined by this child doesnt have any active records
                        {
                            deleteQueue.Enqueue(activeChild); //Add this node for deletion
                        }
                        else
                        {
                            bfsQueue.Enqueue(activeKey + activeChild.character);
                        }
                    }
                }
                //Now delete the records that were identified
                while (deleteQueue.Count > 0)
                {
                    activeRecord.children.Remove(deleteQueue.Dequeue());
                }
            }
        }
    
        public TrieInRAM ConvertToArrayBased(bool garbageCollect = true)
        //Returns TrieInRAM with the same records as this LinkedListRAMTrie
        //Does voluntary GarbageCollect before, to save space
        //Goes through every record using BFS and adds each of them to new TrieInRAM
        {
            if (garbageCollect)
            {
                this.GarbageCollect();
            }
            TrieInRAM returnTrie = new TrieInRAM();
            
            System.Collections.Generic.Queue<string> bfsQueue = new System.Collections.Generic.Queue<string>();
            bfsQueue.Enqueue("");

            while(bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                Record activeRecord = KeyToRecord(activeKey);
                returnTrie.AddElement(activeKey, activeRecord.data);
                foreach (Record activeChild in activeRecord.children)
                {
                    if (activeChild != null)
                    {
                        bfsQueue.Enqueue(activeKey + activeChild.character);
                    }
                }
            }
            return returnTrie;
        }
    }
}
