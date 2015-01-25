using System;
using System.IO;

namespace BfToIr
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) {
                Console.Error.WriteLine("Missing input argument");
                return;
            }

            StreamReader fin;

            try
            {
                fin = new StreamReader(File.Open(args[0], FileMode.Open));
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("Could not open file.");
                return;
            }

            var lexer = new Lexer(fin);
            var rootNode = Parser.run(lexer);

            Generator.generate("genned.exe", rootNode);
        }
    }
}
