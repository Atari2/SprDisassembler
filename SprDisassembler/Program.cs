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
            Parser parser = new Parser(args[0], entrypoint);
            OutputData output;
            if (args.Length > 2) {
                output = new OutputData(args[^1]);
            } else {
                output = new OutputData();
            }
            try {
                output.WriteLine($"\n;;\n;; Entrypoint at {entrypoint:X06}\n;;\n{new Label(parser.Pc, parser.Rom)}:", parser.Rom.SnesToPc(entrypoint));
                parser.Explore(output);
            } catch (InvalidOperationException) {
                Console.WriteLine("Recursion limit reached");
            }
            output.Close();
        }
    }
}
