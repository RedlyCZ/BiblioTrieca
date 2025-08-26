using BiblioTrieca;

namespace Tests
{
    internal class Program
    {
        static void TrieInFileTests()
        //Tests various methods of TrieInFile from BiblioTrieca
        //Two test are disabled, because they are not easily evaluated
        {
            Console.WriteLine("Testing TrieInFile");
            TrieInFile db = new TrieInFile("TrieInFileFile", 1);

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
            if (data == null)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }

            db.AddElement("a52", []);
            db.AddElement("ab", []);

            //Test 3
            int[] metas = db.ReadMetadata("A5");
            if (metas[0] == 0)
            {
                Console.WriteLine("Test 3 : Passed");
            }
            else
            {
                Console.WriteLine("Test 3 : Failed");
            }

            //Test 4
            db.AddElement("", [10, 12]);
            if (db.ReadElement("")[0] == 10)
            {
                db.AddElement("", [0xAC, 0xDC], false);
                if (db.ReadElement("")[1] == 12)
                {
                    Console.WriteLine("Test 4 : Passed");
                }
                else
                {
                    Console.WriteLine("Test 4 : Failed");
                }
            }
            else
            {
                Console.WriteLine("Test 4 : Failed");
            }

            //Test 5
            db.AddElement("aut", [1]);
            db.AddElement("autarkie", [2]);
            db.AddElement("auto", [3]);
            db.AddElement("autodrom", [4]);
            db.AddElement("automat", [5]);
            if (db.BranchSize("aut") == 5)
            {
                Console.WriteLine("Test 5 : Passed");
            }
            else
            {
                Console.WriteLine("Test 5 : Failed");
            }

            //Test 6
            string[] autocompletions = db.AutoComplete("a", 4);
            if (autocompletions[0] == "ab" && autocompletions[1] == "a52" && autocompletions[2] == "aut" && autocompletions[3] == "auto")
            {
                Console.WriteLine("Test 6 : Passed");
            }
            else
            {
                Console.WriteLine("Test 6 : Failed");
            }


            //Test 7
            db.RemoveBranch("aut");
            if (db.ReadElement("aut") == null && db.ReadElement("autarkie") == null && db.ReadElement("auto") == null
                && db.ReadElement("automat") == null && db.ReadElement("autodrom") == null)
            {
                Console.WriteLine("Test 7 : Passed");
            }
            else
            {
                Console.WriteLine("Test 7 : Failed");
            }

            //Test 7.5 - graphical
            /**
            db.AddElement("Jan", [24, 06]);
            db.AddElement("Jaromir", [24, 09]);
            db.AddElement("Jaroslav", [27, 04]);
            db.AddElement("John", [24, 09, 14, 92]);
            db.AddElement("Jonas", [60, 06, 05]);
            db.ConsolePrint("", 8);
            **/

            //Test 7.75 - speed test
            /**
            var watch = new System.Diagnostics.Stopwatch();
            db.AddElement("Nejneobhospodarovavatelnejsi", [1, 2, 3, 4]);
            watch.Start();
            db.ReadElement("Nejneobhospodarovavatelnejsi");
            watch.Stop();
            Console.WriteLine($"Cache hit ReadElement time: {watch.ElapsedTicks}");
            watch.Restart();
            watch.Start();
            db.ReadTryCache("Nejneobhospodarovavatelnejsi");
            watch.Stop();
            Console.WriteLine($"Cache hit ReadElement time: {watch.ElapsedTicks}");
            watch.Restart();
            watch.Start();
            db.ReadElement("Nejneobhospodarovavatelnejsi");
            watch.Stop();
            Console.WriteLine($"Cache hit ReadElement time: {watch.ElapsedTicks}");
            **/

            //Test 8
            if (db.nmbRecordsInDB == 19)
            {
                db.GarbageCollector();
                if (db.nmbRecordsInDB == 4)
                {
                    Console.WriteLine("Test 8 : Passed");
                }
                else
                {
                    Console.WriteLine("Test 8 : Failed");
                }
            }

            //Test 9
            byte[] bigData = new byte[150];
            for(int i = 0; i < bigData.Length; i++)
            {
                bigData[i] = (byte)i;
            }
            try
            {
                db.AddElement("velke dato", bigData);
            }
            catch (Exception)
            {
                Console.WriteLine("Test 9 : Passed");
            }

            //Test 10
            if(db.AutoComplete("nonexistent element", 5) == null)
            {
                Console.WriteLine("Test 10 : Passed");
            }
            else
            {
                Console.WriteLine("Test 10 : Failed");
            }
        }

        static void TrieInRamTests()
        {
            Console.WriteLine("Testing TrieInRAM");
            TrieInRAM db2 = new TrieInRAM();

            //Test 1
            db2.AddElement("aho j", [14, 15, 19, 45]);
            byte[] data = db2.ReadElement("aho j");
            if (data[0] == 14 && data[2] == 19 && data[3] == 45)
            {
                Console.WriteLine("Test 1 : Passed");
            }
            else
            {
                Console.WriteLine("Test 1 : Failed");
            }

            //Test 2
            db2.AddElement("ahojda", [55]);
            db2.RemoveElement("aho j");
            if(db2.ReadElement("aho j") == null)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }

            //Test 3
            db2.AddElement("pot", [1]);
            db2.AddElement("po tom", [2]);
            db2.AddElement("pote", [3]);
            db2.AddElement("potomek", [4]);
            uint a = db2.BranchSize("pot");
            if (db2.BranchSize("pot") == 3) 
            {
                Console.WriteLine("Test 3 : Passed");
            }
            else
            {
                Console.WriteLine("Test 3 : Failed");
            }

            //Test 4
            string[] completed = db2.AutoComplete("poto", 3);
            if (completed[0] == "potomek" && completed[2] == null)
            {
                Console.WriteLine("Test 4 : Passed");
            }
            else
            {
                Console.WriteLine("Test 4 : Failed");
            }

            db2.AddElement("polak", []);

            //Test 5
            db2.RemoveBranch("pot");
            if (db2.ReadElement("pot") == null && db2.ReadElement("pote") == null && db2.ReadElement("potomek") == null)
            {
                Console.WriteLine("Test 5 : Passed");
            }
            else
            {
                Console.WriteLine("Test 5 : Failed");
            }

            //Test 5.25
            //db2.ConsolePrint("", 6);

            //Test 5.5
            //db2.GarbageCollect();

            //Test 5.75
            //db2.ConsolePrint("", 6);

            //Required temp removal before test 6 and test 7
            if (File.Exists("savedRAMTrie"))
            {
                File.Delete("savedRAMTrie");
            }
            if (File.Exists("savedRAMTrieWOGB"))
            {
                File.Delete("savedRAMTrieWOGB");
            }

            //Test 6
            db2.SaveToFile("savedRAMTrieWOGB", false);
            db2.SaveToFile("savedRAMTrie");
            TrieInRAM db3 = new TrieInRAM();
            db3.LoadFromFile("savedRAMTrie");
            if(db3.ReadElement("po tom") != null && db3.ReadElement("po tom")[0] == 2)
            {
                Console.WriteLine("Test 6 : Passed");
            }
            else
            {
                Console.WriteLine("Test 6 : Failed");
            }

            //Test 7
            if(File.Exists("savedRAMTrie") && File.Exists("savedRAMTrieWOGB"))
            {
                if (new FileInfo("savedRAMTrie").Length < new FileInfo("savedRAMTrieWOGB").Length)
                {
                    Console.WriteLine("Test 7 : Passed");
                }
                else
                {
                    Console.WriteLine("Test 7 : Failed");
                }
            }
            else
            {
                Console.WriteLine("Test 7 : Failed");
            }

            //Test 8
            if (db3.AutoComplete("nonexistent element", 5) == null)
            {
                Console.WriteLine("Test 8 : Passed");
            }
            else
            {
                Console.WriteLine("Test 8 : Failed");
            }

            //Test 9
            LinkedListRAMTrie db6 = db2.ConvertToLinkedListBased();
            if (db6.ReadElement("ahojda")[0] == 55)
            {
                Console.WriteLine("Test 9 : Passed");
            }
            else
            {
                Console.WriteLine("Test 9 : Failed");
            }
        }

        static void LinkedListRAMTrieTests()
        {
            Console.WriteLine("Testing LinkedListRAMTrieTest");
            
            LinkedListRAMTrie db4 = new LinkedListRAMTrie();

            //Test 1
            db4.AddElement("aho j", [14, 15, 19, 45]);
            byte[] data = db4.ReadElement("aho j");
            if (data[0] == 14 && data[2] == 19 && data[3] == 45)
            {
                Console.WriteLine("Test 1 : Passed");
            }
            else
            {
                Console.WriteLine("Test 1 : Failed");
            }

            //Test 2
            db4.AddElement("ahojda", [55]);
            db4.RemoveElement("aho j");
            if (db4.ReadElement("aho j") == null)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }

            //Test 3
            db4.AddElement("pot", [1]);
            db4.AddElement("po tom", [2]);
            db4.AddElement("pote", [3]);
            db4.AddElement("potomek", [4]);
            uint a = db4.BranchSize("pot");
            if (db4.BranchSize("pot") == 3)
            {
                Console.WriteLine("Test 3 : Passed");
            }
            else
            {
                Console.WriteLine("Test 3 : Failed");
            }

            //Test 4
            string[] completed = db4.AutoComplete("poto", 3);
            if (completed[0] == "potomek" && completed[2] == null)
            {
                Console.WriteLine("Test 4 : Passed");
            }
            else
            {
                Console.WriteLine("Test 4 : Failed");
            }

            db4.AddElement("polak", []);

            //Test 5
            db4.RemoveBranch("pot");
            if (db4.ReadElement("pot") == null && db4.ReadElement("pote") == null && db4.ReadElement("potomek") == null)
            {
                Console.WriteLine("Test 5 : Passed");
            }
            else
            {
                Console.WriteLine("Test 5 : Failed");
            }

            //Test 5.25
            //db4.ConsolePrint("", 6);

            //Test 5.5
            //db4.GarbageCollect();

            //Test 5.75
            //db4.ConsolePrint("", 6);

            //Test 6
            TrieInRAM db5 = db4.ConvertToArrayBased();
            if (db5.ReadElement("po tom")[0] == 2 && db5.ReadElement("ahojda")[0] == 55)
            {
                Console.WriteLine("Test 6 : Passed");
            }
            else
            {
                Console.WriteLine("Test 6 : Failed");
            }

            //Test 8
            if (db4.AutoComplete("nonexistent element", 5) == null)
            {
                Console.WriteLine("Test 7 : Passed");
            }
            else
            {
                Console.WriteLine("Test 7 : Failed");
            }

        }
        
        static void TrieInFileBitWiseTests()
        {
            Console.WriteLine("Testing TrieInFileBitWise");
            File.Delete("TrieInFileBitWise"); //Force delete, to make size tests work the same
            TrieInFile db7 = new TrieInFile("TrieInFileBitWise", 0, true);

            //Test 1
            db7.AddElement("", [12, 14]);
            db7.AddElement("01", [56, 120, 13]);
            db7.AddElement("011", [10, 11, 12]);

            if (db7.ReadElement("01")[0] == 56 && db7.ReadElement("01")[2] == 13)
            {
                Console.WriteLine("Test 1 : Passed");
            }
            else
            {
                Console.WriteLine("Test 1 : Failed");
            }

            //Test 2
            db7.AddElement("010", [13, 14, 15]);
            int[] metas = db7.ReadMetadata("01");
            if (metas[1] == 4 && metas[2] == 3)
            {
                Console.WriteLine("Test 2 : Passed");
            }
            else
            {
                Console.WriteLine("Test 2 : Failed");
            }

            //Test 3
            if(db7.nmbRecordsInDB == 4 && new FileInfo("TrieInFileBitWise").Length == 640)
            {
                Console.WriteLine("Test 3 : Passed");
            }
            else
            {
                Console.WriteLine("Test 3 : Failed");
            }

            //Test 4
            db7.AddElement("11", [0, 1]);
            db7.AddElement("101", [1, 2]);
            db7.AddElement("111", [1, 3]);
            db7.RemoveBranch("0");
            db7.GarbageCollector();
            if (db7.nmbRecordsInDB == 5 && new FileInfo("TrieInFileBitWise").Length == 768)
            {
                Console.WriteLine("Test 4 : Passed");
            }
            else
            {
                Console.WriteLine("Test 4 : Failed");
            }

        }
        static void Main(string[] args)
        {
            //TrieInFileTests();
            //TrieInRamTests();
            //LinkedListRAMTrieTests();
            TrieInFileBitWiseTests();
        }
    }
}
