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
            db.DeleteElement("a5");
            data = db.ReadElement("a5");
            if(data == null)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }
        }
    }
}
