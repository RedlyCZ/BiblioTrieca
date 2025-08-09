namespace BiblioTrieca
{
    interface TrieDatabase
    {
        void AddElement(string key, byte[] data);
        byte[] ReadElement(string key);
        void OverrideElement(string key, byte[] data);
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

        public void AddElement(string key, byte[] data)
        {
            uint activeIndex = 0;
            FileStream fileStream = new FileStream(adress,FileMode.Open, FileAccess.ReadWrite);


        }
    }
}
