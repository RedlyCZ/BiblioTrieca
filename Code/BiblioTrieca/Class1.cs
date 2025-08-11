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


        public TrieInFile(string adress)
        {
            this.adress = adress;
            this.nmbRecordsInDB = 0;
            FileStream fileStream = new FileStream(adress, FileMode.Create, FileAccess.Write);
            BinaryWriter writer = new BinaryWriter(fileStream);
            writer.Write(emptyRecord);
            writer.Close();
            fileStream.Close();
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
                    return recordData;
                }
                else
                {
                    reader.Close();
                    return null;
                }
                
            }
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
            char[] arrayOfCharsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];
            
            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                this.RemoveElement(activeKey);
                int[] childrenIndexes = this.ReadMetadata(activeKey);
                for (int i = 1; i < childrenIndexes.Length; i++)
                {
                    if(childrenIndexes[i] != 0)
                    //If there is a child on this record index
                    {
                        string newKey = activeKey + arrayOfCharsInRecord[i-1];
                        bfsQueue.Enqueue(newKey);
                    }
                }
            }
        }

        public uint BranchSize(string key)
        //Returns number of active (having data) records in this branch
        {
            uint size = 0;

            char[] arrayOfCharsInRecord = ['0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c',
                'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z'];

            System.Collections.Generic.Queue<string> bfsQueue = new Queue<string>();
            bfsQueue.Enqueue(key);

            while (bfsQueue.Count > 0)
            {
                string activeKey = bfsQueue.Dequeue();
                int[] childrenIndexes = this.ReadMetadata(activeKey);
                if(childrenIndexes[0] != 0)
                {
                    size++;
                }
                for (int i = 1; i < childrenIndexes.Length; i++)
                {
                    if (childrenIndexes[i] != 0)
                    //If there is a child on this record index
                    {
                        string newKey = activeKey + arrayOfCharsInRecord[i - 1];
                        bfsQueue.Enqueue(newKey);
                    }
                }
            }
            return size;
        }
    }
}
