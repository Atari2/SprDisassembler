using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SprDisassembler {
    class Jumpable : IJumpable {
        private int _destination;
        private string _mnemonic;
        private Opcode op;
        private static readonly string[] _jumpables = { "JMP", "JSL", "JML", "JSR", "BRA", "BRL" };
        private static readonly AddressingMode[] _modes = { AddressingMode.Relative, AddressingMode.RelativeLong, AddressingMode.Absolute, AddressingMode.AbsoluteLong };
        public int Destination { get => _destination; set => _destination = value; }
        public static IEnumerable<string> Jumpables { get => _jumpables; }
        public static IEnumerable<AddressingMode> Modes { get => _modes; }

        public Jumpable(Opcode op) {
            this.op = op;
            _mnemonic = op.Mnemonic;
            if (Jumpables.Contains(op.Mnemonic))
                Destination = op.Operand;
            else
                throw new InvalidOperationException();
        }
        public void UpdateDestination(Rom rom, ref int curpc) {
            if (_mnemonic == _jumpables[^1] || _mnemonic == _jumpables[^2]) {       // brl + bra
                curpc += (sbyte)_destination + 2;
            } else if (op.Size == 3) {
                curpc = (curpc & 0xFF0000) | (rom.SnesToPc(Destination) & 0x00FFFF);
            } else {
                curpc = rom.SnesToPc(Destination);
            }
        }

        public int GetDestination() {
            return Destination;
        }
    }

    class Returnable : Jumpable, IReturnable {
        private int _returnaddress;
        private readonly string _mnemonic;
        private static readonly string[] _returnables = { "JSL", "JSR" };
        public int ReturnAddress { get => _returnaddress; set => _returnaddress = value; }
        public static IEnumerable<string> Returnables { get => _returnables; }
        public Returnable(Opcode op, int curpc) : base(op) {
            if (Returnables.Contains(op.Mnemonic)) {
                ReturnAddress = curpc + op.Size - 1;
                _mnemonic = op.Mnemonic;
            } else
                throw new InvalidOperationException();
        }

        public Returnable PushReturn(Stack<byte> romStack) {
            int realReturn = _returnaddress + 1;
            if (_mnemonic == _returnables[0]) {
                romStack.Push((byte)((realReturn >> 16) & 0xFF));
            }
            romStack.Push((byte)((realReturn >> 8) & 0xFF));
            romStack.Push((byte)(realReturn & 0xFF));
            return this;
        }
    }

    class ModifierMode : ISwitchableMode {
        private readonly string _mnemonic;
        private Opcode _op;
        private static readonly string[] _modifiers = { "REP", "SEP", "PHP" };
        public Opcode Op { get => _op; set => _op = value; }
        public static IEnumerable<string> Modifiers { get => _modifiers; }

        public ModifierMode(Opcode op) {
            if (Modifiers.Contains(op.Mnemonic)) {
                _op = op;
                _mnemonic = op.Mnemonic;
            } else {
                throw new InvalidOperationException();
            }
        }

        public void UpdateProcessorFlags(ref ProcessorFlags flags, Stack<byte> romStack) {
            if (_mnemonic == _modifiers[^1]) {
                flags = ProcessorFlags.M | ProcessorFlags.X;        //(ProcessorFlags)romStack.Pop();, this just resets both A and X/Y to 8 bit, for no real reason
            } else {
                for (int i = 0; i < 8; i++) {
                    byte val = (byte)((Op.Operand & 0xFF) & (1 << i));
                    if (_mnemonic == "REP") {
                        flags = flags.ClearFlag((ProcessorFlags)val);
                    } else if (_mnemonic == "SEP") {
                        flags = flags.SetFlag((ProcessorFlags)val);
                    }
                }
            }
        }
    }

    class Returning : IReturning {
        private readonly string _mnemonic;
        private static readonly string[] _returning = { "RTL", "RTS", "RTI" };
        public Stack<byte> RomStack { get; set; }
        public static IEnumerable<string> ReturningOps { get => _returning; }

        public Returning(Opcode op, Stack<byte> romStack) {
            if (ReturningOps.Contains(op.Mnemonic)) {
                RomStack = romStack;
                _mnemonic = op.Mnemonic;
            } else
                throw new InvalidOperationException();
        }

        public void Return(ref int curpc) {
            try {
                byte low = RomStack.Pop();
                byte high = RomStack.Pop();
                if (_mnemonic != _returning[1]) {
                    byte bank = RomStack.Pop();
                    curpc = (bank << 16) | (high << 8) | (low);
                } else {
                    curpc = (curpc & 0xFF0000) | (high << 8) | (low);
                }
            } catch (InvalidOperationException) {
                return;
            }
        }
    }
}
