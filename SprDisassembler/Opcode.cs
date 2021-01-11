using System;
using System.Collections.Generic;
using System.Text;

namespace SprDisassembler {

    public enum AddressingMode {
        Implied,
        Immediate,
        ImmediateLong,
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
        int Size;
        public int Code { get; set; }
        int Operand;
        string Mnemonic;
        AddressingMode Mode;

        public Opcode(int size, AddressingMode mode, int code, string mnemonic) {
            Size = size;
            Mode = mode;
            Code = code;
            Mnemonic = mnemonic;
        }

        public override string ToString() {
            StringBuilder builder = new StringBuilder(Mnemonic.ToUpper());
            builder.Append((Size - 1) switch {
                0 => "",
                1 => ".b ",
                2 => ".w ",
                3 => ".l ",
                _ => throw new NotImplementedException()
            });
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
                AddressingMode.ImmediateLong => "#" + codeStr,
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
                AddressingMode.StackRelativeIndirectIndexed => codeStr + ",s),y",
                AddressingMode.AbsoluteIndirect => codeStr + ")",
                AddressingMode.AbsoluteIndirectLong => codeStr + "]",
                AddressingMode.AbsoluteIndexedIndirect => codeStr + ",x)",
                AddressingMode.ImpliedAccumulator => "",
                AddressingMode.BlockMove => codeStr,
                _ => throw new NotImplementedException()
            }) ;
            return builder.ToString();
        }
    }
}
