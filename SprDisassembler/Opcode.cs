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
    
    class Opcode {
        public int Size;
        public byte Code { get; set; }
        public int Operand { get; set; }
        public readonly string Mnemonic;
        public AddressingMode Mode { get; set; }

        public Opcode(int size, byte code, string mnemonic, AddressingMode mode) {
            Size = size;
            Mode = mode;
            Code = code;
            Mnemonic = mnemonic;
        }

        public Opcode(Opcode origin) {
            Size = origin.Size;
            Mode = origin.Mode;
            Code = origin.Code;
            Mnemonic = origin.Mnemonic;
        }

        public void SetOperand (Rom rom, int pc) {
            Operand = (Size - 1) switch {
                0 => 0,
                1 => rom.GetByte(pc + 1),
                2 => rom.GetWord(pc + 1),
                3 => (int)rom.GetLong(pc + 1),
                _ => throw new InvalidSizeException()
            };
        }

        public override string ToString() {
            StringBuilder builder = new StringBuilder(Mnemonic.ToUpper());
            if (Mode != AddressingMode.Immediate && Mode != AddressingMode.Relative && Mode != AddressingMode.RelativeLong) {
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
            while (builder.Length < 6)      // padding
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

        public string ToStringWithLabel(Label label) {
            return ToString().Split()[0] + $" {label}";
        }

        public void SetSize(ProcessorFlags flags) {
            Size = GetSize(flags);
        }

        public int GetSize(ProcessorFlags flags) {
            if (Mode == AddressingMode.Immediate && Size - 1 == 2) {
                return Mnemonic[^1] switch {
                    'A' => flags.IsFlagClear(ProcessorFlags.M) ? 3 : 2,
                    'X' => flags.IsFlagClear(ProcessorFlags.X) ? 3 : 2,
                    'Y' => flags.IsFlagClear(ProcessorFlags.X) ? 3 : 2,
                    _ => flags.IsFlagClear(ProcessorFlags.M) ? 3 : 2
                };
            } else {
                return Size;
            }
        }

        public bool IsInvalid() {
            return Code == 0x00 || Code == 0x02 || Code == 0xDB;
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

        public bool IsActionable() {
            return IsBranchable() || IsJumpable() || IsIndirectJumpable() || IsReturnable() || IsReturning() || CanModifyMode();
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


    }
}
