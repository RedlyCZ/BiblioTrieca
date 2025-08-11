using BiblioTrieca;

namespace Pokusy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testujeme program");
            TrieInFile db = new TrieInFile("zkousime");
            
            
            db.AddElement("a", [0xAE, 0x59, 0x48, 0xF5, 0x2A]);
            db.AddElement("", [0xBB]);
            db.AddElement("A5", [100, 15, 4]);
            
            //Test 1
            byte[] data = db.ReadElement("a5");
            if (data[0] == 100 && data[1] == 15 && data[2] == 4 && data[3] == 0 && data[65] == 0)
            {
                Console.WriteLine("Test 1 : Passed");
            }
            else
            {
                Console.WriteLine("Test 1 : Failed");
            }

            //Test 2
            db.RemoveElement("a5");
            data = db.ReadElement("a5");
            if(data == null)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }

            //Test 3
            db.AddElement("a52", []);
            db.AddElement("AB", []);
            int[] metas = db.ReadMetadata("a");
            if (metas[0] == 1 && metas[6] == 2 && metas[7] == 0 && metas[12] == 4)
            {
                Console.WriteLine("Test 3 : Passed");
            }
            else
            {
                Console.WriteLine("Test 3 : Failed");
            }

            //Test 4
            metas = db.ReadMetadata("A5");
            if (metas[0] == 0){
                Console.WriteLine("Test 4 : Passed");
            }
            else
            {
                Console.WriteLine("Test 4 : Failed");
            }

            //Test 5
            db.AddElement("", [10, 12]);
            if (db.ReadElement("")[0] == 10)
            {
                db.AddElement("", [0xAC,0xDC], false);
                if (db.ReadElement("")[1] == 12)
                {
                    Console.WriteLine("Test 5 : Passed");
                }
                else
                {
                    Console.WriteLine("Test 5 : Failed");
                }
            }
            else
            {
                Console.WriteLine("Test 5 : Failed");
            }


            //Test 6
            db.AddElement("aut", [1]);
            db.AddElement("autarkie", [2]);
            db.AddElement("auto", [3]);
            db.AddElement("autodrom", [4]);
            db.AddElement("automat", [5]);
            if(db.BranchSize("aut") == 5)
            {
                Console.WriteLine("Test 6 : Passed");
            }
            else
            {
                Console.WriteLine("Test 6 : Failed");
            }

            //Test 7
            db.RemoveBranch("aut");
            if(db.ReadElement("aut")==null && db.ReadElement("autarkie") == null && db.ReadElement("auto") == null 
                && db.ReadElement("automat") == null && db.ReadElement("autodrom") == null)
            {
                Console.WriteLine("Test 7 : Passed");
            }
            else
            {
                Console.WriteLine("Test 7 : Failed");
            }


        }
    }
}
