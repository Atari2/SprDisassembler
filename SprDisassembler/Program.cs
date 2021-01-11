using System;

namespace SprDisassembler {
    class Program {
        static void Main(string[] args) {
            Parser.Opcodes.ForEach(x => Console.WriteLine(x.ToString()));
        }
    }
}
