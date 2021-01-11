using System;
using System.Collections.Generic;
using System.Text;

namespace SprDisassembler {
    class Routine {
        int Position { get; set; }
        int Size;

        byte[] RoutineData;

        public Routine(Rom rom, int addr, int size) {
            RoutineData = rom.GetBuffer(addr, size);
            Position = addr;
            Size = size;
        }

        // TODO: Make disassemble function for routines
        public string Disassemble() {
            throw new NotImplementedException();
        }
    }
}
