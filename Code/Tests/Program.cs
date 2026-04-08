using BiblioTrieca;
using System.ComponentModel.Design;
using log4net.Config;
using System.IO;

namespace Tests
{
    internal class Program
    {
        static void PrintTestResult(int testNumber, bool passed)
        {
            Console.Write($"Test {testNumber} : ");
            if (passed)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Passed\n");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("Failed\n");
            }
            Console.ForegroundColor = ConsoleColor.White;
        }

        static void DeleteTestTemps()
        {
            File.Delete("savedRAMTrie");
            File.Delete("savedRAMTrieWOGB");
            File.Delete("TrieInFileFile");
            File.Delete("TrieInFileBitWise");
            File.Delete("savedRAMTrieBitWise");
            File.Delete("savedRAMTrieWOGBBitWise");
        }

        static void TrieInFileTests()
        //Tests various methods of TrieInFile from BiblioTrieca
        //Two test are disabled, because they are not easily evaluated
        {
            int nmbPass = 0;
            Console.WriteLine("Testing TrieInFile");
            TrieInFile db = new TrieInFile("TrieInFileFile", 1);

            db.AddElement("a", [0xAE, 0x59, 0x48, 0xF5, 0x2A]);
            db.AddElement("", [0xBB]);
            db.AddElement("A5", [100, 15, 4]);

            //Test 1
            byte[] data = db.ReadElement("a5");
            if (data[0] == 100 && data[1] == 15 && data[2] == 4 && data[3] == 0 && data[65] == 0)
            {
                PrintTestResult(1, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(1, false);
            }

            //Test 2
            db.RemoveElement("a5");
            data = db.ReadElement("a5");
            if (data == null)
            {
                PrintTestResult(2, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(2, false);
            }

            db.AddElement("a52", []);
            db.AddElement("ab", []);

            //Test 3
            uint[] metas = db.ReadMetadata("A5");
            if (metas[0] == 0)
            {
                PrintTestResult(3, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(3, false);
            }

            //Test 4
            db.AddElement("", [10, 12]);
            if (db.ReadElement("")[0] == 10)
            {
                db.AddElement("", [0xAC, 0xDC], false);
                if (db.ReadElement("")[1] == 12)
                {
                    PrintTestResult(4, true);
                    nmbPass++;
                }
                else
                {
                    PrintTestResult(4, false);
                }
            }
            else
            {
                PrintTestResult(4, false);
            }

            //Test 5
            db.AddElement("aut", [1]);
            db.AddElement("autarkie", [2]);
            db.AddElement("auto", [3]);
            db.AddElement("autodrom", [4]);
            db.AddElement("automat", [5]);
            if (db.BranchSize("aut") == 5)
            {
                PrintTestResult(5, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(5, false);
            }

            //Test 6
            string[] autocompletions = db.AutoComplete("a", 4);
            if (autocompletions[0] == "ab" && autocompletions[1] == "a52" && autocompletions[2] == "aut" && autocompletions[3] == "auto")
            {
                PrintTestResult(6, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(6, false);
            }


            //Test 7
            db.RemoveBranch("aut");
            if (db.ReadElement("aut") == null && db.ReadElement("autarkie") == null && db.ReadElement("auto") == null
                && db.ReadElement("automat") == null && db.ReadElement("autodrom") == null)
            {
                PrintTestResult(7, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(7, false);
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
                    PrintTestResult(8, true);
                    nmbPass++;
                }
                else
                {
                    PrintTestResult(8, false);
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
                PrintTestResult(9, true);
                nmbPass++;
            }

            //Test 10
            if (db.AutoComplete("nonexistent element", 5) == null)
            {
                PrintTestResult(10, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(10, false);
            }
            Console.WriteLine($"All tests in this branch completed, {(nmbPass*100)/10} % of tests passed");
            Console.WriteLine("--------------------------------------------------------------");
        }

        static void TrieInRamTests()
        {
            int nmbPass = 0;
            Console.WriteLine("Testing TrieInRAM");
            TrieInRAM db2 = new TrieInRAM();

            //Test 1
            db2.AddElement("aho j", [14, 15, 19, 45]);
            byte[] data = db2.ReadElement("aho j");
            if (data[0] == 14 && data[2] == 19 && data[3] == 45)
            {
                PrintTestResult(1, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(1, false);
            }

            //Test 2
            db2.AddElement("ahojda", [55]);
            db2.RemoveElement("aho j");
            if(db2.ReadElement("aho j") == null)
            {
                PrintTestResult(2, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(2, false);
            }

            //Test 3
            db2.AddElement("pot", [1]);
            db2.AddElement("po tom", [2]);
            db2.AddElement("pote", [3]);
            db2.AddElement("potomek", [4]);
            uint a = db2.BranchSize("pot");
            if (db2.BranchSize("pot") == 3) 
            {
                PrintTestResult(3, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(3, false);
            }

            //Test 4
            string[] completed = db2.AutoComplete("poto", 3);
            if (completed[0] == "potomek" && completed[2] == null)
            {
                PrintTestResult(4, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(4, false);
            }

            db2.AddElement("polak", []);

            //Test 5
            db2.RemoveBranch("pot");
            if (db2.ReadElement("pot") == null && db2.ReadElement("pote") == null && db2.ReadElement("potomek") == null)
            {
                PrintTestResult(5, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(5, false);
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
                PrintTestResult(6, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(6, false);
            }

            //Test 7
            if(File.Exists("savedRAMTrie") && File.Exists("savedRAMTrieWOGB"))
            {
                if (new FileInfo("savedRAMTrie").Length < new FileInfo("savedRAMTrieWOGB").Length)
                {
                    PrintTestResult(7, true);
                    nmbPass++;
                }
                else
                {
                    PrintTestResult(7, false);
                }
            }
            else
            {
                PrintTestResult(7, false);
            }

            //Test 8
            if (db3.AutoComplete("nonexistent element", 5) == null)
            {
                PrintTestResult(8, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(8, false);
            }

            //Test 9
            LinkedListRAMTrie db6 = db2.ConvertToLinkedListBased();
            if (db6.ReadElement("ahojda")[0] == 55)
            {
                PrintTestResult(9, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(9, false);
            }

            Console.WriteLine($"All tests in this branch completed, {(nmbPass * 100) / 9} % of tests passed");
            Console.WriteLine("--------------------------------------------------------------");
        }

        static void LinkedListRAMTrieTests()
        {
            int nmbPass = 0;
            Console.WriteLine("Testing LinkedListRAMTrieTest");
            
            LinkedListRAMTrie db4 = new LinkedListRAMTrie();

            //Test 1
            db4.AddElement("aho j", [14, 15, 19, 45]);
            byte[] data = db4.ReadElement("aho j");
            if (data[0] == 14 && data[2] == 19 && data[3] == 45)
            {
                PrintTestResult(1, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(1, false);
            }

            //Test 2
            db4.AddElement("ahojda", [55]);
            db4.RemoveElement("aho j");
            if (db4.ReadElement("aho j") == null)
            {
                PrintTestResult(2, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(2, false);
            }

            //Test 3
            db4.AddElement("pot", [1]);
            db4.AddElement("po tom", [2]);
            db4.AddElement("pote", [3]);
            db4.AddElement("potomek", [4]);
            uint a = db4.BranchSize("pot");
            if (db4.BranchSize("pot") == 3)
            {
                PrintTestResult(3, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(3, false);
            }

            //Test 4
            string[] completed = db4.AutoComplete("poto", 3);
            if (completed[0] == "potomek" && completed[2] == null)
            {
                PrintTestResult(4, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(4, false);
            }

            db4.AddElement("polak", []);

            //Test 5
            db4.RemoveBranch("pot");
            if (db4.ReadElement("pot") == null && db4.ReadElement("pote") == null && db4.ReadElement("potomek") == null)
            {
                PrintTestResult(5, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(5, false);
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
                PrintTestResult(6, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(6, false);
            }

            //Test 8
            if (db4.AutoComplete("nonexistent element", 5) == null)
            {
                PrintTestResult(7, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(7, false);
            }

            Console.WriteLine($"All tests in this branch completed, {(nmbPass * 100) / 7} % of tests passed");
            Console.WriteLine("--------------------------------------------------------------");

        }
        
        static void TrieInFileBitWiseTests()
        {
            int nmbPass = 0;
            Console.WriteLine("Testing TrieInFileBitWise");

            File.Delete("TrieInFileBitWise"); //Force delete, to make size tests work the same
            TrieInFile db7 = new TrieInFile("TrieInFileBitWise", 0, true);

            //Test 1
            db7.AddElement("", [12, 14]);
            db7.AddElement("01", [56, 120, 13]);
            db7.AddElement("011", [10, 11, 12]);

            if (db7.ReadElement("01")[0] == 56 && db7.ReadElement("01")[2] == 13)
            {
                PrintTestResult(1, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(1, false);
            }

            //Test 2
            db7.AddElement("010", [13, 14, 15]);
            uint[] metas = db7.ReadMetadata("01");
            if (metas[1] == 4 && metas[2] == 3)
            {
                PrintTestResult(2, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(2, false);
            }

            //Test 3
            if(db7.nmbRecordsInDB == 4 && new FileInfo("TrieInFileBitWise").Length == 640)
            {
                PrintTestResult(3, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(3, false);
            }

            //Test 4
            db7.AddElement("11", [0, 1]);
            db7.AddElement("101", [1, 2]);
            db7.AddElement("111", [1, 3]);
            db7.RemoveBranch("0");
            db7.GarbageCollector();
            if (db7.nmbRecordsInDB == 5 && new FileInfo("TrieInFileBitWise").Length == 768)
            {
                PrintTestResult(4, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(4, false);
            }

            Console.WriteLine($"All tests in this branch completed, {(nmbPass * 100) / 4} % of tests passed");
            Console.WriteLine("--------------------------------------------------------------");
        }

        static void TrieInRAMBitWiseTests()
        {
            int nmbPass = 0;
            Console.WriteLine("Testing TrieInRAMBitWise");

            TrieInRAM db9 = new TrieInRAM(true);
            //Test 1
            db9.AddElement("", [12, 14]);
            db9.AddElement("01", [56, 120, 13]);
            db9.AddElement("011", [10, 11, 12]);

            if (db9.ReadElement("01")[0] == 56 && db9.ReadElement("01")[2] == 13)
            {
                PrintTestResult(1, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(1, false);
            }

            //Test 2
            db9.AddElement("010", [55]);
            db9.RemoveElement("011");
            if (db9.ReadElement("011") == null)
            {
                PrintTestResult(2, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(2, false);
            }

            //Test 3
            db9.AddElement("111", [1]);
            db9.AddElement("1110", [2]);
            db9.AddElement("1111", [3]);
            db9.AddElement("111010", [4]);
            uint a = db9.BranchSize("11");
            if (a == 4)
            {
                PrintTestResult(3, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(3, false);
            }

            //Test 4
            string[] completed = db9.AutoComplete("111", 3);
            if (completed[0] == "1110" && completed[2] == "111010")
            {
                PrintTestResult(4, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(4, false);
            }

            //Test 5
            db9.RemoveBranch("1110");
            if (db9.ReadElement("1110") == null && db9.ReadElement("111010") == null && db9.ReadElement("1111")[0] == 3)
            {
                PrintTestResult(5, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(5, false);
            }

            //Test 6
            db9.SaveToFile("savedRAMTrieWOGBBitWise", false);
            db9.SaveToFile("savedRAMTrieBitWise");
            TrieInRAM db10 = new TrieInRAM(true);
            db10.LoadFromFile("savedRAMTrieBitWise");
            if (db10.ReadElement("01") != null && db10.ReadElement("1111")[0] == 3)
            {
                PrintTestResult(6, true);
                nmbPass++;
            }
            else
            {
                PrintTestResult(6, false);
            }

            //Test 7
            if (File.Exists("savedRAMTrieBitWise") && File.Exists("savedRAMTrieWOGBBitWise"))
            {
                if (new FileInfo("savedRAMTrieBitWise").Length < new FileInfo("savedRAMTrieWOGBBitWise").Length)
                {
                    PrintTestResult(7, true);
                    nmbPass++;
                }
                else
                {
                    PrintTestResult(7, false);
                }
            }
            else
            {
                PrintTestResult(7, false);
            }

            Console.WriteLine($"All tests in this branch completed, {(nmbPass * 100) / 7} % of tests passed");
            Console.WriteLine("--------------------------------------------------------------");

        }
        static void Main(string[] args)
        {
            XmlConfigurator.Configure(new FileInfo("log4net.config"));
            Console.ForegroundColor = ConsoleColor.White;
            TrieInFileTests();
            TrieInRamTests();
            LinkedListRAMTrieTests();
            TrieInFileBitWiseTests();
            TrieInRAMBitWiseTests();
            DeleteTestTemps();
        }
    }
}
