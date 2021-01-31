using System;
using System.IO;

namespace SprDisassembler {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Invalid number of command line arguments");
            }
            Parser parser = new Parser(args[0], Convert.ToInt32(args[1], 16));
            TextWriter output = Console.Out;
            if (args.Length > 2) {
                output = File.CreateText(args[^1]);
            }
            parser.Explore(output);
            output.Close();
        }
    }
}
