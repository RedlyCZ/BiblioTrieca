namespace BiblioTrieca
{
    interface TrieDatabase
    {
        void AddElement(string key, byte[] data);
        byte[] ReadElement(string key);
        void RemoveElement(string key);
        byte[] ReadMetadata(string key);

    }

    public class TrieInFile
    {
        const int recordLength = 256;
        //(26 chars + 10 numericals) * 4B + 111B data = 256 B

        byte[] emptyRecord = new byte[256];

        string adress;
        uint nmbRecordsInDB; //In reality is one less than the true number of records in DB, shows last active record index


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

        public void AddElement(string key, byte[] data)
        {
            uint activeRecordIndex = 0;
            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryWriter writer = new BinaryWriter(fileStream);
                BinaryReader reader = new BinaryReader(fileStream);
                
                //First navigate to the destination and create records on the way if necessary
                for (int keyCharIndex = 0; keyCharIndex < key.Length; keyCharIndex++)
                {
                    char activeChar = key[keyCharIndex];
                    long charDataByteOffset = CharToIndex(activeChar)*4 + activeRecordIndex*recordLength;
                    fileStream.Position = charDataByteOffset;
                    uint nextRecordIndex = Convert.ToUInt32(reader.Read());
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
                //Set the indication byte
                writer.Write((byte)1);
                //Then write the data in designated record
                writer.Write(data);
                writer.Close();
                reader.Close();
            }
        }
        public byte[] ReadElement(string key)
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
                    uint nextRecordIndex = Convert.ToUInt32(reader.Read());
                    if (nextRecordIndex != 0)
                    //If the way exists
                    {
                        activeRecordIndex = nextRecordIndex;
                    }
                    else
                    {
                        return null;
                    }
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
                    return null;
                }
                
            }
        }
        public void DeleteElement(string key)
        {
            uint activeRecordIndex = 0;
            bool pathExists = true;
            using (FileStream fileStream = new FileStream(adress, FileMode.Open, FileAccess.ReadWrite))
            {
                BinaryReader reader = new BinaryReader(fileStream);
                BinaryWriter writer = new BinaryWriter(fileStream);

                //Navigating to designated record
                for (int keyCharIndex = 0; keyCharIndex < key.Length; keyCharIndex++)
                {
                    char activeChar = key[keyCharIndex];
                    long charDataByteOffset = CharToIndex(activeChar) * 4 + activeRecordIndex * recordLength;
                    fileStream.Position = charDataByteOffset;
                    uint nextRecordIndex = Convert.ToUInt32(reader.Read());
                    if (nextRecordIndex != 0)
                    //If the way exists
                    {
                        activeRecordIndex = nextRecordIndex;
                    }
                    else
                    {
                        pathExists = false;
                        break;
                    }
                }
                if (pathExists)
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
    }
}
