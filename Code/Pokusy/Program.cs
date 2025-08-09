using BiblioTrieca;

namespace Pokusy
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            TrieInFile db = new TrieInFile("zkousime");
            Console.WriteLine("mozna se i povedlo");
        }
    }
}
