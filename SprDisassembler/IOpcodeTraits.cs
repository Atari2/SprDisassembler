using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprDisassembler {
    interface IJumpable {
        static IEnumerable<string> Jumpables { get; }
        int Destination { get; set; }
        public void UpdateDestination(Rom rom, ref int curpc);
        public int GetDestination();
    }

    interface IReturnable {
        static IEnumerable<string> Returnables { get; }
        int ReturnAddress { get; set; }
        public Returnable PushReturn(Stack<byte> romStack);
    }

    interface ISwitchableMode {
        static IEnumerable<string> Modifiers { get; }
        Opcode Op { get; set; }
        public void UpdateProcessorFlags(ref ProcessorFlags flags, Stack<byte> romStack);
    }

    interface IReturning {
        public abstract Stack<byte> RomStack { get; set; }
        public static IEnumerable<string> ReturningOps { get; }
        public void Return(ref int curpc);
    }
}
