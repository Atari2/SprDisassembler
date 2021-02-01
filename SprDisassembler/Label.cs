using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprDisassembler {
    public record Label {
        public int Pc;
        public string Name;
        public Label(int address, Rom rom) {
            Pc = rom.PcToSnes(address);
            Name = $"Label{Pc:X06}";
        }

        public override string ToString() {
            return Name;
        }
    }
}
