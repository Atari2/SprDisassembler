using System;
using System.IO;

namespace SprDisassembler {
    class Program {
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Invalid number of command line arguments, call is: 'romname entrypoint [outfile]' where entrypoint is a valid lorom snes address" +
                    "outfile is an optional filename to dump the output, otherwise it'll be printed on stdout");
                return;
            }
            int entrypoint = Convert.ToInt32(args[1], 16);
            Parser parser = new Parser(args[0], entrypoint, args.Length > 2 ? args[^1] : null);
            try {
                parser.Explore();
                parser.Close();
            } catch (LoopEncounteredException) {
                Console.WriteLine("Recursion limit reached");
            }
        }
    }
}
