using System;
using System.Collections.Generic;
using System.Text;

namespace SprDisassembler {
    [Flags]
    public enum ProcessorFlags : byte {
        // nvmxdizc
        C = 0x01,
        Z = 0x02,
        I = 0x04,
        D = 0x08,
        X = 0x10,
        M = 0x20,
        V = 0x40,
        N = 0x80
    }

    public static class FlagsExtensions {

        public static bool IsFlagSet(this ProcessorFlags flags, ProcessorFlags other) {
            return (flags & other) != 0;
        }

        public static ProcessorFlags SetFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags |= other;
            return flags;
        }

        public static ProcessorFlags UnSetFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags &= ((ProcessorFlags)0xFF ^ other);
            return flags;
        }

        public static ProcessorFlags InvertFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags ^= other;
            return flags;
        }


    }
    class Parser {

        // TODO: finish opcode list
        public static List<Opcode> Opcodes = new List<Opcode>() {
            new Opcode(2, AddressingMode.Immediate, 0x00, "BRK"), // BRK #$xx
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0x01, "ORA"), // ORA ($xx,x)
            new Opcode(2, AddressingMode.Immediate, 0x02, "COP"), // COP #$xx
            new Opcode(2, AddressingMode.StackRelative, 0x03, "ORA" ), // ORA $xx,s
            new Opcode(2, AddressingMode.Direct, 0x04, "TRB"), // TRB $xx
            new Opcode(2, AddressingMode.Direct, 0x05, "ORA"), // ORA $xx
            new Opcode(2, AddressingMode.Direct, 0x06, "ASL"), // ASL $xx
            new Opcode(2, AddressingMode.DirectIndirectLong, 0x07, "ORA"), // ORA [$xx]
            new Opcode(1, AddressingMode.Implied, 0x08, "PHP"), // PHP
            new Opcode(3, AddressingMode.ImmediateLong, 0x09, "ORA"), // ORA #$xxxx
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x0A, "ASL"), // ASL A
            new Opcode(1, AddressingMode.Implied, 0x0B, "PHD"), // PHD
            new Opcode(3, AddressingMode.Absolute, 0x0C, "TSB"), // TSB $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x0D, "ORA"), // ORA $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x0E, "ASL"), // ASL $xxxx
            new Opcode(4, AddressingMode.AbsoluteLong, 0x0F, "ORA") // ORA $xxxxxx
        };
        Rom Rom { get; set; }
        int Pc { get; set; }
        Stack<byte> RomStack { get; set; } = new Stack<byte>();
        Stack<int> PcStack { get; set; } = new Stack<int>();
        bool Invalid { get; set; }

        // processor flags
        ProcessorFlags Flags { get; set; } = 0;
        bool A16Bit { get => Flags.IsFlagSet(ProcessorFlags.M); set => Flags = Flags.SetFlag(ProcessorFlags.M); }
        bool A8Bit { get => !Flags.IsFlagSet(ProcessorFlags.M); set => Flags = Flags.UnSetFlag(ProcessorFlags.M); }
        bool XY16Bit { get => Flags.IsFlagSet(ProcessorFlags.X); set => Flags = Flags.SetFlag(ProcessorFlags.X); }
        bool XY8Bit { get => !Flags.IsFlagSet(ProcessorFlags.X); set => Flags = Flags.UnSetFlag(ProcessorFlags.X); }

        public Parser(string filename, int entrypoint) {
            Rom = new Rom(filename);
            Pc = Rom.SnesToPc(entrypoint);
        }

        // TODO: implement explore function
        public void Explore() {
            throw new NotImplementedException();
        }
    }
}
