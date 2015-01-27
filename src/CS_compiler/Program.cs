using System;
using System.IO;

namespace BrainfCompiler
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 1) {
                Console.Error.WriteLine("Missing input argument");
                return;
            }

            bool o_loop = false, o_offset = false;
            string filename = null;
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i] == "-oloop") o_loop = true;
                else if (args[i] == "-ooffsets") o_offset = true;
                else if (args[i] == "-ofull")
                {
                    o_loop = true;
                    o_offset = true;
                }
                else
                {
                    filename = args[i];
                }
            }

            StreamReader fin;

            try
            {
                fin = new StreamReader(File.Open(filename, FileMode.Open));
            }
            catch (FileNotFoundException)
            {
                Console.Error.WriteLine("Could not open file {0}.", filename);
                return;
            }

            var lexer = new Lexer(fin);
            var rootNode = Parser.run(lexer);

            if (o_offset)
            {
                rootNode = Optimizer.optimize_offsets(rootNode);
            }
            if (o_loop)
            {
                rootNode = Optimizer.optimize_loops(rootNode);
            }

            Generator.run("genned.exe", rootNode);
        }
    }
}
