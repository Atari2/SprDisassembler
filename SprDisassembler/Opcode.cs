using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SprDisassembler {

    public enum AddressingMode {
        Implied,
        Immediate,
        Relative,
        RelativeLong,
        Direct,
        DirectIndexedX,
        DirectIndexedY,
        DirectIndirect,
        DirectIndexedIndirect,
        DirectIndirectIndexed,
        DirectIndirectLong,
        DirectIndirectIndexedLong,
        Absolute,
        AbsoluteIndexedX,
        AbsoluteIndexedY,
        AbsoluteLong,
        AbsoluteIndexedLong,
        StackRelative,
        StackRelativeIndirectIndexed,
        AbsoluteIndirect,
        AbsoluteIndirectLong,
        AbsoluteIndexedIndirect,
        ImpliedAccumulator,
        BlockMove
    };

    interface IJumpable {
        static IEnumerable<string> Jumpables { get; }
        int Destination { get; set; }
        public abstract void UpdateDestination(Rom rom, ref int curpc);
        public abstract int GetDestination();
    }

    interface IReturnable {
        static IEnumerable<string> Returnables { get; }
        int ReturnAddress { get; set; }
        public abstract void PushReturn(Stack<byte> romStack);
    }

    interface ISwitchableMode {
        static IEnumerable<string> Modifiers { get; }
        Opcode op { get; set; }
        public abstract void UpdateProcessorFlags(ref ProcessorFlags flags, Stack<byte> romStack);
    }

    class Jumpable : IJumpable {
        private int _destination;
        private string _mnemonic;
        private Opcode op;
        private static readonly string[] _jumpables = { "JMP", "JSL", "JML", "JSR", "BRA" };
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
            if (_mnemonic == _jumpables[^1]) {
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

        public void PushReturn(Stack<byte> romStack) {
            int realReturn = _returnaddress + 1;
            if (_mnemonic == _returnables[0]) {
                romStack.Push((byte)((realReturn >> 16) & 0xFF));
            }
            romStack.Push((byte)((realReturn >> 8) & 0xFF));
            romStack.Push((byte)(realReturn & 0xFF));
        }
    }

    class ModifierMode : ISwitchableMode {
        private readonly string _mnemonic;
        private Opcode _op;
        private static readonly string[] _modifiers = { "REP", "SEP", "PHP" };
        public Opcode op { get => _op; set => _op = value; }
        public static IEnumerable<string> Modifiers { get => _modifiers; }

        public ModifierMode (Opcode op) {
            if (Modifiers.Contains(op.Mnemonic)) {
                _op = op;
                _mnemonic = op.Mnemonic;
            } else {
                throw new InvalidOperationException();
            }
        }

        public void UpdateProcessorFlags(ref ProcessorFlags flags, Stack<byte> romStack) {
            if (_mnemonic == _modifiers[^1]) {
                flags = ProcessorFlags.M | ProcessorFlags.X;//(ProcessorFlags)romStack.Pop();
            } else {
                for (int i = 0; i < 8; i++) {
                    byte val = (byte)((op.Operand & 0xFF) & (1 << i));
                    if (_mnemonic == "REP") {
                        flags = flags.UnSetFlag((ProcessorFlags)val);
                    } else if (_mnemonic == "SEP") {
                        flags = flags.SetFlag((ProcessorFlags)val);
                    }
                }
            }
        }
    }


    class Returning {
        private readonly string _mnemonic;
        private static readonly string[] _returning = { "RTL", "RTS", "RTI" };
        private Stack<byte> RomStack { get; set; }
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
    
    record Opcode {
        public int Size;
        public int Code { get; set; }
        public int Operand { get; set; }
        public readonly string Mnemonic;
        public AddressingMode Mode { get; set; }

        public Opcode(int size, AddressingMode mode, int code, string mnemonic) {
            Size = size;
            Mode = mode;
            Code = code;
            Mnemonic = mnemonic;
        }

        public void SetOperand (int operand) {
            Operand = operand;
        }

        public override string ToString() {
            StringBuilder builder = new StringBuilder(Mnemonic.ToUpper());
            if (Mode != AddressingMode.Immediate && Mode != AddressingMode.Relative) {
                builder.Append((Size - 1) switch {
                    0 => "",
                    1 => ".b ",
                    2 => ".w ",
                    3 => ".l ",
                    _ => throw new NotImplementedException()
                });
            } else {
                builder.Append(' ');
            }
            while (builder.Length < 6)
                builder.Append(' ');
            string codeStr;
            if (Mode != AddressingMode.BlockMove) {
                string format = string.Format("${{0:X0{0}}}", (Size - 1) * 2);
                codeStr = string.Format(format, Operand);
            } else {
                codeStr = $"${Operand & 0xFF:0X2},{(Operand >> 8) & 0xFF:0X2}";
            }
            builder.Append(Mode switch {
                AddressingMode.Implied => "",
                AddressingMode.Immediate => "#" + codeStr,
                AddressingMode.Relative => codeStr,
                AddressingMode.RelativeLong => codeStr,
                AddressingMode.Direct => codeStr,
                AddressingMode.DirectIndexedX => codeStr + ",x",
                AddressingMode.DirectIndexedY => codeStr + ",y",
                AddressingMode.DirectIndirect => "(" + codeStr + ")",
                AddressingMode.DirectIndexedIndirect => "(" + codeStr + ",x)",
                AddressingMode.DirectIndirectIndexed => "(" + codeStr + "),y",
                AddressingMode.DirectIndirectLong => "[" + codeStr + "]",
                AddressingMode.DirectIndirectIndexedLong => "[" + codeStr + "],y",
                AddressingMode.Absolute => codeStr,
                AddressingMode.AbsoluteIndexedX => codeStr + ",x",
                AddressingMode.AbsoluteIndexedY => codeStr + ",y",
                AddressingMode.AbsoluteLong => codeStr,
                AddressingMode.AbsoluteIndexedLong => codeStr + ",x",
                AddressingMode.StackRelative => codeStr + ",s",
                AddressingMode.StackRelativeIndirectIndexed => "(" + codeStr + ",s),y",
                AddressingMode.AbsoluteIndirect => "(" + codeStr + ")",
                AddressingMode.AbsoluteIndirectLong => "[" + codeStr + "]",
                AddressingMode.AbsoluteIndexedIndirect => "(" +codeStr + ",x)",
                AddressingMode.ImpliedAccumulator => "",
                AddressingMode.BlockMove => codeStr,
                _ => throw new NotImplementedException()
            }) ;
            return builder.ToString();
        }

        public string ToStringWithSize(int size) {
            int oldsize = Size;
            Size = size;
            string ret = ToString();
            Size = oldsize;
            return ret;
        }

        public bool IsBranchable() {
            return Mode == AddressingMode.Relative || Mode == AddressingMode.RelativeLong;
        }

        public bool IsJumpable() {
            return Jumpable.Jumpables.Contains(Mnemonic) && Jumpable.Modes.Contains(Mode);
        }

        public bool IsIndirectJumpable() {
            string modeString = Mode.ToString();
            return Jumpable.Jumpables.Contains(Mnemonic) && (modeString.Contains("Indirect") || modeString.Contains("Indexed"));
        }

        public bool IsReturnable() {
            return IsJumpable() && Returnable.Returnables.Contains(Mnemonic);
        }

        public bool IsReturning() {
            return Returning.ReturningOps.Contains(Mnemonic);
        }

        public bool CanModifyMode() {
            return ModifierMode.Modifiers.Contains(Mnemonic);
        }

        public Jumpable GetJumpableFromOp() {
            if (IsJumpable())
                return new Jumpable(this);
            else
                throw new InvalidOperationException();
        }

        public Returnable GetReturnableFromOp(int curpc) {
            if (IsReturnable())
                return new Returnable(this, curpc);
            else
                throw new InvalidOperationException();
        }

        public Returning GetReturningFromOp(Stack<byte> romStack) {
            if (IsReturning())
                return new Returning(this, romStack);
            else
                throw new InvalidOperationException();
        }

        public ModifierMode GetModifierFromOp() {
            if (CanModifyMode())
                return new ModifierMode(this);
            else
                throw new InvalidOperationException();
        }

        public string ToStringWithLabel(Label label) {
            return ToString().Split()[0] + $" {label}";
        }

    }
}
