using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;

namespace SprDisassembler {
    /// <summary>
    /// Enum that represents the state of the processor flags during exploration of the entrypoint
    /// </summary>
    class Parser {
        // TODO: Parse stack -> make sure jumps go where they're supposed to go (aka, ignore conditional and indirect jumps)
        private int _pc;
        private ulong _loop = 0;
        private ProcessorFlags _flags = ProcessorFlags.M | ProcessorFlags.X;
        public static List<Opcode> Opcodes = new () {
            new Opcode(2, 0x00, "BRK", AddressingMode.Immediate), // BRK #$xx
            new Opcode(2, 0x01, "ORA", AddressingMode.DirectIndexedIndirect), // ORA ($xx,x)
            new Opcode(2, 0x02, "COP", AddressingMode.Immediate), // COP #$xx
            new Opcode(2, 0x03, "ORA", AddressingMode.StackRelative), // ORA $xx,s
            new Opcode(2, 0x04, "TRB", AddressingMode.Direct), // TRB $xx
            new Opcode(2, 0x05, "ORA", AddressingMode.Direct), // ORA $xx
            new Opcode(2, 0x06, "ASL", AddressingMode.Direct), // ASL $xx
            new Opcode(2, 0x07, "ORA", AddressingMode.DirectIndirectLong), // ORA [$xx]
            new Opcode(1, 0x08, "PHP", AddressingMode.Implied), // PHP
            new Opcode(3, 0x09, "ORA", AddressingMode.Immediate), // ORA #$xxxx | ORA #$xx
            new Opcode(1, 0x0A, "ASL", AddressingMode.ImpliedAccumulator), // ASL A
            new Opcode(1, 0x0B, "PHD", AddressingMode.Implied), // PHD
            new Opcode(3, 0x0C, "TSB", AddressingMode.Absolute), // TSB $xxxx
            new Opcode(3, 0x0D, "ORA", AddressingMode.Absolute), // ORA $xxxx
            new Opcode(3, 0x0E, "ASL", AddressingMode.Absolute), // ASL $xxxx
            new Opcode(4, 0x0F, "ORA", AddressingMode.AbsoluteLong), // ORA $xxxxxx
            new Opcode(2, 0x10, "BPL", AddressingMode.Relative), // BPL $xx
            new Opcode(2, 0x11, "ORA", AddressingMode.DirectIndirectIndexed), // ORA ($xx),y
            new Opcode(2, 0x12, "ORA", AddressingMode.DirectIndirect), // ORA ($xx)
            new Opcode(2, 0x13, "ORA", AddressingMode.StackRelativeIndirectIndexed), // ORA ($xx,s),y
            new Opcode(2, 0x14, "TRB", AddressingMode.Direct), // TRB $xx
            new Opcode(2, 0x15, "ORA", AddressingMode.DirectIndexedX), // ORA $xx,x
            new Opcode(2, 0x16, "ASL", AddressingMode.DirectIndexedX), // ASL $xx,x
            new Opcode(2, 0x17, "ORA", AddressingMode.DirectIndirectIndexedLong), // ORA [$xx],y
            new Opcode(1, 0x18, "CLC", AddressingMode.Implied), // CLC
            new Opcode(3, 0x19, "ORA", AddressingMode.AbsoluteIndexedY), // ORA $xxxx,y
            new Opcode(1, 0x1A, "INC", AddressingMode.ImpliedAccumulator), // INC A
            new Opcode(1, 0x1B, "TCS", AddressingMode.Implied), // TCS
            new Opcode(3, 0x1C, "TRB", AddressingMode.Absolute), // TRB $xxxx
            new Opcode(3, 0x1D, "ORA", AddressingMode.AbsoluteIndexedX), // ORA $xxxx,x
            new Opcode(3, 0x1E, "ASL", AddressingMode.AbsoluteIndexedX), // ASL $xxxx,x
            new Opcode(4, 0x1F, "ORA", AddressingMode.AbsoluteIndexedLong), // ORA $xxxxxx,x
            new Opcode(3, 0x20, "JSR", AddressingMode.Absolute), // JSR $xxxx
            new Opcode(2, 0x21, "AND", AddressingMode.DirectIndexedIndirect), // AND ($xx,x)
            new Opcode(4, 0x22, "JSL", AddressingMode.AbsoluteLong), // JSL $xxxxxx
            new Opcode(2, 0x23, "AND", AddressingMode.StackRelative), // AND $xx,s
            new Opcode(2, 0x24, "BIT", AddressingMode.Direct), // BIT $xx
            new Opcode(2, 0x25, "AND", AddressingMode.Direct), // AND $xx
            new Opcode(2, 0x26, "ROL", AddressingMode.Direct), // ROL $xx
            new Opcode(2, 0x27, "AND", AddressingMode.DirectIndirectLong), // AND [$xx]
            new Opcode(1, 0x28, "PLP", AddressingMode.Implied), // PLP
            new Opcode(3, 0x29, "AND", AddressingMode.Immediate), // AND #$xxxx | AND #$xx
            new Opcode(1, 0x2A, "ROL", AddressingMode.ImpliedAccumulator), // ROL A
            new Opcode(1, 0x2B, "PLD", AddressingMode.Implied), // PLD
            new Opcode(3, 0x2C, "BIT", AddressingMode.Absolute), // BIT
            new Opcode(3, 0x2D, "AND", AddressingMode.Absolute), // AND $xxxx
            new Opcode(3, 0x2E, "ROL", AddressingMode.Absolute), // ROL $xxxx
            new Opcode(4, 0x2F, "AND", AddressingMode.AbsoluteLong), // AND $xxxxxx
            new Opcode(2, 0x30, "BMI", AddressingMode.Relative), // BMI $xx
            new Opcode(2, 0x31, "AND", AddressingMode.DirectIndirectIndexed), // AND ($xx),y
            new Opcode(2, 0x32, "AND", AddressingMode.DirectIndirect), // AND ($xx)
            new Opcode(2, 0x33, "AND", AddressingMode.StackRelativeIndirectIndexed), // AND ($xx,s),y
            new Opcode(2, 0x34, "BIT", AddressingMode.DirectIndexedX), // BIT $xx,x
            new Opcode(2, 0x35, "AND", AddressingMode.DirectIndexedX), // AND $xx,x
            new Opcode(2, 0x36, "ROL", AddressingMode.DirectIndexedX), // ROL $xx,x
            new Opcode(2, 0x37, "AND", AddressingMode.DirectIndirectIndexedLong), // AND [$xx],y
            new Opcode(1, 0x38, "SEC", AddressingMode.Implied), // SEC
            new Opcode(3, 0x39, "AND", AddressingMode.AbsoluteIndexedX), // AND $xxxx,y
            new Opcode(1, 0x3A, "DEC", AddressingMode.ImpliedAccumulator), // DEC A 
            new Opcode(1, 0x3B, "TSC", AddressingMode.Implied), // TSC
            new Opcode(3, 0x3C, "BIT", AddressingMode.AbsoluteIndexedX), // BIT $xxxx,x
            new Opcode(3, 0x3D, "AND", AddressingMode.AbsoluteIndexedX), // AND $xxxx,x
            new Opcode(3, 0x3E, "ROL", AddressingMode.AbsoluteIndexedX), // ROL $xxxx,x
            new Opcode(4, 0x3F, "AND", AddressingMode.AbsoluteIndexedLong), // AND $xxxxxx,x
            new Opcode(1, 0x40, "RTI", AddressingMode.Implied), // RTI
            new Opcode(2, 0x41, "EOR", AddressingMode.DirectIndexedIndirect), // EOR ($xx,x)
            new Opcode(2, 0x42, "WDM", AddressingMode.Immediate), // WDM #$xx
            new Opcode(2, 0x43, "EOR", AddressingMode.StackRelative), // EOR $xx,s
            new Opcode(3, 0x44, "MVP", AddressingMode.BlockMove), // MVP $xx,$xx
            new Opcode(2, 0x45, "EOR", AddressingMode.Direct), // EOR $xx
            new Opcode(2, 0x46, "LSR", AddressingMode.Direct), // LSR $xx
            new Opcode(2, 0x47, "EOR", AddressingMode.DirectIndirectLong), // EOR [$xx]
            new Opcode(1, 0x48, "PHA", AddressingMode.Implied), // PHA
            new Opcode(3, 0x49, "EOR", AddressingMode.Immediate), // EOR #$xxxx | EOR #$xx
            new Opcode(1, 0x4A, "LSR", AddressingMode.ImpliedAccumulator), // LSR A
            new Opcode(1, 0x4B, "PHK", AddressingMode.Implied), // PHK
            new Opcode(3, 0x4C, "JMP", AddressingMode.Absolute), // JMP $xxxx
            new Opcode(3, 0x4D, "EOR", AddressingMode.Absolute), // EOR $xxxx
            new Opcode(3, 0x4E, "LSR", AddressingMode.Absolute), // LSR $xxxx
            new Opcode(4, 0x4F, "EOR", AddressingMode.AbsoluteLong), // EOR $xxxxxx
            new Opcode(2, 0x50, "BVC", AddressingMode.Relative), // EOR $xx
            new Opcode(2, 0x51, "EOR", AddressingMode.DirectIndirectIndexed), // EOR ($xx),y
            new Opcode(2, 0x52, "EOR", AddressingMode.DirectIndirect), // EOR ($xx)
            new Opcode(2, 0x53, "EOR", AddressingMode.StackRelativeIndirectIndexed), // EOR ($xx,s),y
            new Opcode(3, 0x54, "MVN", AddressingMode.BlockMove), // MVN $xx,$xx
            new Opcode(2, 0x55, "EOR", AddressingMode.DirectIndexedX), // EOR $xx,x
            new Opcode(2, 0x56, "LSR", AddressingMode.DirectIndexedX), // LSR $xx,x
            new Opcode(2, 0x57, "EOR", AddressingMode.DirectIndirectIndexedLong), // EOR [$xx],y
            new Opcode(1, 0x58, "CLI", AddressingMode.Implied), // CLI
            new Opcode(3, 0x59, "EOR", AddressingMode.AbsoluteIndexedY), // EOR $xxxx,y
            new Opcode(1, 0x5A, "PHY", AddressingMode.Implied), // PHY
            new Opcode(1, 0x5B, "TCD", AddressingMode.Implied), // TCD
            new Opcode(4, 0x5C, "JML", AddressingMode.AbsoluteLong), // JML $xxxxxx
            new Opcode(3, 0x5D, "EOR", AddressingMode.AbsoluteIndexedX), // EOR $xxxx,x
            new Opcode(3, 0x5E, "LSR", AddressingMode.AbsoluteIndexedX), // LSR $xxxx,x
            new Opcode(4, 0x5F, "EOR", AddressingMode.AbsoluteIndexedLong), // EOR $xxxxxx,x
            new Opcode(1, 0x60, "RTS", AddressingMode.Implied),
            new Opcode(2, 0x61, "ADC", AddressingMode.DirectIndexedIndirect), // ADC ($xx,x)
            new Opcode(3, 0x62, "PER", AddressingMode.Absolute), // PER $xxxx
            new Opcode(2, 0x63, "ADC", AddressingMode.StackRelative), // ADC $xx,s
            new Opcode(2, 0x64, "STZ", AddressingMode.Direct), // STZ $xx
            new Opcode(2, 0x65, "ADC", AddressingMode.Direct), // ADC $xx
            new Opcode(2, 0x66, "ROR", AddressingMode.Direct), // ROR $xx
            new Opcode(2, 0x67, "ADC", AddressingMode.DirectIndirectLong), // ADC [$xx]
            new Opcode(1, 0x68, "PLA", AddressingMode.Implied), // PLA
            new Opcode(3, 0x69, "ADC", AddressingMode.Immediate), // ADC #$xxxx
            new Opcode(1, 0x6A, "ROR", AddressingMode.ImpliedAccumulator), // ROR A
            new Opcode(1, 0x6B, "RTL", AddressingMode.Implied), // RTL
            new Opcode(3, 0x6C, "JMP", AddressingMode.AbsoluteIndirect), // JMP ($xxxx)
            new Opcode(3, 0x6D, "ADC", AddressingMode.Absolute), // ADC $xxxx
            new Opcode(3, 0x6E, "ROR", AddressingMode.Absolute), // ROR
            new Opcode(4, 0x6F, "ADC", AddressingMode.AbsoluteLong), // ADC $xxxxxx
            new Opcode(2, 0x70, "BVS", AddressingMode.Relative), // BVS $xx
            new Opcode(2, 0x71, "ADC", AddressingMode.DirectIndirectIndexed), // ADC ($xx),y
            new Opcode(2, 0x72, "ADC", AddressingMode.DirectIndirect), // ADC ($xx)
            new Opcode(2, 0x73, "ADC", AddressingMode.StackRelativeIndirectIndexed), // ADC ($xx,s),y
            new Opcode(2, 0x74, "STZ", AddressingMode.DirectIndexedX), // STZ $xx,x
            new Opcode(2, 0x75, "ADC", AddressingMode.DirectIndexedX), // ADC $xx,x
            new Opcode(2, 0x76, "ROR", AddressingMode.DirectIndexedX), // ROR $xx,x
            new Opcode(2, 0x77, "ADC", AddressingMode.DirectIndirectIndexedLong), // ADC [$xx],y
            new Opcode(1, 0x78, "SEI", AddressingMode.Implied), // SEI
            new Opcode(3, 0x79, "ADC", AddressingMode.AbsoluteIndexedY), // ADC $xxxx,y
            new Opcode(1, 0x7A, "PLY", AddressingMode.Implied), // PLY
            new Opcode(1, 0x7B, "TDC", AddressingMode.Implied), // TDC
            new Opcode(3, 0x7C, "JMP", AddressingMode.AbsoluteIndexedIndirect), // JMP ($xxxx,x)
            new Opcode(3, 0x7D, "ADC", AddressingMode.AbsoluteIndexedX), // ADC
            new Opcode(3, 0x7E, "ROR", AddressingMode.AbsoluteIndexedX), // ROR $xxxx,x
            new Opcode(4, 0x7F, "ADC", AddressingMode.AbsoluteIndexedLong), // ADC $xxxxxx,x
            new Opcode(2, 0x80, "BRA", AddressingMode.Relative), // BRA $xx
            new Opcode(2, 0x81, "STA", AddressingMode.DirectIndexedIndirect), // STA ($xx,x)
            new Opcode(3, 0x82, "BRL", AddressingMode.RelativeLong), // BRL $xxxx
            new Opcode(2, 0x83, "STA", AddressingMode.StackRelative), // STA $xx,s
            new Opcode(2, 0x84, "STY", AddressingMode.Direct), // STY $xx
            new Opcode(2, 0x85, "STA", AddressingMode.Direct), // STA $xx
            new Opcode(2, 0x86, "STX", AddressingMode.Direct), // STX $xx
            new Opcode(2, 0x87, "STA", AddressingMode.DirectIndirectLong), // STA [$xx]
            new Opcode(1, 0x88, "DEY", AddressingMode.Implied), // DEY
            new Opcode(3, 0x89, "BIT", AddressingMode.Immediate), // BIT #$xxxx | BIT #$xx
            new Opcode(1, 0x8A, "TXA", AddressingMode.Implied), // TXA
            new Opcode(1, 0x8B, "PHB", AddressingMode.Implied), // PHB
            new Opcode(3, 0x8C, "STY", AddressingMode.Absolute), // STY $xxxx
            new Opcode(3, 0x8D, "STA", AddressingMode.Absolute), // STA $xxxx   
            new Opcode(3, 0x8E, "STX", AddressingMode.Absolute), // STX $xxxx
            new Opcode(4, 0x8F, "STA", AddressingMode.AbsoluteLong), // STA $xxxxxx
            new Opcode(2, 0x90, "BCC", AddressingMode.Relative),
            new Opcode(2, 0x91, "STA", AddressingMode.DirectIndirectIndexed),
            new Opcode(2, 0x92, "STA", AddressingMode.DirectIndirect),
            new Opcode(2, 0x93, "STA", AddressingMode.StackRelativeIndirectIndexed),
            new Opcode(2, 0x94, "STY", AddressingMode.DirectIndexedX),
            new Opcode(2, 0x95, "STA", AddressingMode.DirectIndexedX),
            new Opcode(2, 0x96, "STX", AddressingMode.DirectIndexedY),
            new Opcode(2, 0x97, "STA", AddressingMode.DirectIndirectIndexedLong),
            new Opcode(1, 0x98, "TYA", AddressingMode.Implied),
            new Opcode(3, 0x99, "STA", AddressingMode.AbsoluteIndexedY),
            new Opcode(1, 0x9A, "TXS", AddressingMode.Implied),
            new Opcode(1, 0x9B, "TXY", AddressingMode.Implied),
            new Opcode(3, 0x9C, "STZ", AddressingMode.Absolute),
            new Opcode(3, 0x9D, "STA", AddressingMode.AbsoluteIndexedX),
            new Opcode(3, 0x9E, "STZ", AddressingMode.AbsoluteIndexedX),
            new Opcode(4, 0x9F, "STA", AddressingMode.AbsoluteIndexedLong),
            new Opcode(3, 0xA0, "LDY", AddressingMode.Immediate),
            new Opcode(2, 0xA1, "LDA", AddressingMode.DirectIndexedIndirect),
            new Opcode(3, 0xA2, "LDX", AddressingMode.Immediate),
            new Opcode(2, 0xA3, "LDA", AddressingMode.StackRelative),
            new Opcode(2, 0xA4, "LDY", AddressingMode.Direct),
            new Opcode(2, 0xA5, "LDA", AddressingMode.Direct),
            new Opcode(2, 0xA6, "LDX", AddressingMode.Direct),
            new Opcode(2, 0xA7, "LDA", AddressingMode.DirectIndirectLong),
            new Opcode(1, 0xA8, "TAY", AddressingMode.Implied),
            new Opcode(3, 0xA9, "LDA", AddressingMode.Immediate),
            new Opcode(1, 0xAA, "TAX", AddressingMode.Implied),
            new Opcode(1, 0xAB, "PLB", AddressingMode.Implied),
            new Opcode(3, 0xAC, "LDY", AddressingMode.Absolute),
            new Opcode(3, 0xAD, "LDA", AddressingMode.Absolute),
            new Opcode(3, 0xAE, "LDX", AddressingMode.Absolute),
            new Opcode(4, 0xAF, "LDA", AddressingMode.AbsoluteLong),
            new Opcode(2, 0xB0, "BCS", AddressingMode.Relative),
            new Opcode(2, 0xB1, "LDA", AddressingMode.DirectIndirectIndexed),
            new Opcode(2, 0xB2, "LDA", AddressingMode.DirectIndirect),
            new Opcode(2, 0xB3, "LDA", AddressingMode.StackRelativeIndirectIndexed),
            new Opcode(2, 0xB4, "LDY", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xB5, "LDA", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xB6, "LDX", AddressingMode.DirectIndexedY),
            new Opcode(2, 0xB7, "LDA", AddressingMode.DirectIndirectIndexedLong),
            new Opcode(1, 0xB8, "CLV", AddressingMode.Implied),
            new Opcode(3, 0xB9, "LDA", AddressingMode.AbsoluteIndexedY),
            new Opcode(1, 0xBA, "TSX", AddressingMode.Implied),
            new Opcode(1, 0xBB, "TYX", AddressingMode.Implied),
            new Opcode(3, 0xBC, "LDY", AddressingMode.AbsoluteIndexedX),
            new Opcode(3, 0xBD, "LDA", AddressingMode.AbsoluteIndexedX),
            new Opcode(3, 0xBE, "LDX", AddressingMode.AbsoluteIndexedY),
            new Opcode(4, 0xBF, "LDA", AddressingMode.AbsoluteIndexedLong),
            new Opcode(3, 0xC0, "CPY", AddressingMode.Immediate),
            new Opcode(2, 0xC1, "CMP", AddressingMode.DirectIndexedIndirect),
            new Opcode(2, 0xC2, "REP", AddressingMode.Immediate),
            new Opcode(2, 0xC3, "CMP", AddressingMode.StackRelative),
            new Opcode(2, 0xC4, "CPY", AddressingMode.Direct),
            new Opcode(2, 0xC5, "CMP", AddressingMode.Direct),
            new Opcode(2, 0xC6, "DEC", AddressingMode.Direct),
            new Opcode(2, 0xC7, "CMP", AddressingMode.DirectIndirectLong),
            new Opcode(1, 0xC8, "INY", AddressingMode.Implied),
            new Opcode(3, 0xC9, "CMP", AddressingMode.Immediate),
            new Opcode(1, 0xCA, "DEX", AddressingMode.Implied),
            new Opcode(1, 0xCB, "WAI", AddressingMode.Implied),
            new Opcode(3, 0xCC, "CPY", AddressingMode.Absolute),
            new Opcode(3, 0xCD, "CMP", AddressingMode.Absolute),
            new Opcode(3, 0xCE, "DEC", AddressingMode.Absolute),
            new Opcode(4, 0xCF, "CMP", AddressingMode.AbsoluteLong),
            new Opcode(2, 0xD0, "BNE", AddressingMode.Relative),
            new Opcode(2, 0xD1, "CMP", AddressingMode.DirectIndirectIndexed),
            new Opcode(2, 0xD2, "CMP", AddressingMode.DirectIndirect),
            new Opcode(2, 0xD3, "CMP", AddressingMode.StackRelativeIndirectIndexed),
            new Opcode(2, 0xD4, "PEI", AddressingMode.DirectIndirect),
            new Opcode(2, 0xD5, "CMP", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xD6, "DEC", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xD7, "CMP", AddressingMode.DirectIndirectIndexedLong),
            new Opcode(1, 0xD8, "CLD", AddressingMode.Implied),
            new Opcode(3, 0xD9, "CMP", AddressingMode.AbsoluteIndexedY),
            new Opcode(1, 0xDA, "PHX", AddressingMode.Implied),
            new Opcode(1, 0xDB, "STP", AddressingMode.Implied),
            new Opcode(3, 0xDC, "JML", AddressingMode.AbsoluteIndirectLong),
            new Opcode(3, 0xDD, "CMP", AddressingMode.AbsoluteIndexedX),
            new Opcode(3, 0xDE, "DEC", AddressingMode.AbsoluteIndexedX),
            new Opcode(4, 0xDF, "CMP", AddressingMode.AbsoluteIndexedLong),
            new Opcode(3, 0xE0, "CPX", AddressingMode.Immediate),
            new Opcode(2, 0xE1, "SBC", AddressingMode.DirectIndexedIndirect),
            new Opcode(2, 0xE2, "SEP", AddressingMode.Immediate),
            new Opcode(2, 0xE3, "SBC", AddressingMode.StackRelative),
            new Opcode(2, 0xE4, "CPX", AddressingMode.Direct),
            new Opcode(2, 0xE5, "SBC", AddressingMode.Direct),
            new Opcode(2, 0xE6, "INC", AddressingMode.Direct),
            new Opcode(2, 0xE7, "SBC", AddressingMode.DirectIndirectLong),
            new Opcode(1, 0xE8, "INX", AddressingMode.Implied),
            new Opcode(3, 0xE9, "SBC", AddressingMode.Immediate),
            new Opcode(1, 0xEA, "NOP", AddressingMode.Implied),
            new Opcode(1, 0xEB, "XBA", AddressingMode.Implied),
            new Opcode(3, 0xEC, "CPX", AddressingMode.Absolute),
            new Opcode(3, 0xED, "SBC", AddressingMode.Absolute),
            new Opcode(3, 0xEE, "INC", AddressingMode.Absolute),
            new Opcode(4, 0xEF, "SBC", AddressingMode.AbsoluteLong),
            new Opcode(2, 0xF0, "BEQ", AddressingMode.Relative),
            new Opcode(2, 0xF1, "SBC", AddressingMode.DirectIndirectIndexed),
            new Opcode(2, 0xF2, "SBC", AddressingMode.DirectIndirect),
            new Opcode(2, 0xF3, "SBC", AddressingMode.StackRelativeIndirectIndexed),
            new Opcode(3, 0xF4, "PEA", AddressingMode.Absolute),
            new Opcode(2, 0xF5, "SBC", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xF6, "INC", AddressingMode.DirectIndexedX),
            new Opcode(2, 0xF7, "SBC", AddressingMode.DirectIndirectIndexedLong),
            new Opcode(1, 0xF8, "SED", AddressingMode.Implied),
            new Opcode(3, 0xF9, "SBC", AddressingMode.AbsoluteIndexedY),
            new Opcode(1, 0xFA, "PLX", AddressingMode.Implied),
            new Opcode(1, 0xFB, "XCE", AddressingMode.Implied),
            new Opcode(3, 0xFC, "JSR", AddressingMode.AbsoluteIndexedIndirect),
            new Opcode(3, 0xFD, "SBC", AddressingMode.AbsoluteIndexedX),
            new Opcode(3, 0xFE, "INC", AddressingMode.AbsoluteIndexedX),
            new Opcode(4, 0xFF, "SBC", AddressingMode.AbsoluteIndexedLong) // SBC $xxxxxx,x
        };
        public Rom Rom { get; set; }
        private readonly int Origin;
        public int Pc { get => _pc; set => _pc = value; }
        Stack<byte> RomStack { get; set; } = new Stack<byte>();
        Stack<int> PcStack { get; set; } = new Stack<int>();
        Dictionary<int, string> Routines { get; } = new Dictionary<int, string>();
        Dictionary<int, Label> Labels { get; } = new Dictionary<int, Label>();
        bool Invalid { get; set; }
        bool EmulationMode { get; set; }
        OutputData Output;

        // processor flags
        public ProcessorFlags Flags { get => _flags; set => _flags = value; }
        bool A16Bit { get => Flags.IsFlagClear(ProcessorFlags.M); set => Flags.ClearFlag(ProcessorFlags.M); }
        bool A8Bit { get => Flags.IsFlagSet(ProcessorFlags.M); set => Flags.SetFlag(ProcessorFlags.M); }
        bool XY16Bit { get => Flags.IsFlagClear(ProcessorFlags.X); set => Flags.ClearFlag(ProcessorFlags.X); }
        bool XY8Bit { get => Flags.IsFlagSet(ProcessorFlags.X); set => Flags.SetFlag(ProcessorFlags.X); }

        // unused for now
        Dictionary<RegisterType, Register> Registers = new Dictionary<RegisterType, Register>() {
            { RegisterType.A,  new Register(RegisterType.A) },
            { RegisterType.X,  new Register(RegisterType.X) },
            { RegisterType.Y,  new Register(RegisterType.Y) },
            { RegisterType.S,  new Register(RegisterType.S) },
            { RegisterType.DB, new Register(RegisterType.DB) },
            { RegisterType.DP, new Register(RegisterType.DP) },
            { RegisterType.PB, new Register(RegisterType.PB) },
            { RegisterType.P,  new Register(RegisterType.P) },
            { RegisterType.PC, new Register(RegisterType.PC) }
        };

        public Parser(string filename, int entrypoint, string outputName) {
            Rom = new Rom(filename);
            Pc = Rom.SnesToPc(entrypoint);
            Origin = Pc;
            if (outputName != null) {
                Output = new OutputData(outputName);
            } else {
                Output = new OutputData();
            }
            string entrypointIntro = $"\n;;\n;; Entrypoint at ${Rom.PcToSnes(Pc):X06}\n;;";
            Label entryLabel = new(Origin, Rom);
            Labels.Add(Origin, entryLabel);
            Routines.Add(Pc, entrypointIntro);
            PrintLine(entrypointIntro);
            PrintLine($"{entryLabel}:");
        }

        public void CheckLoop() {
            if (_loop >= 0x100) {       // extremely dumb (to say the least) infinite loop detection
                throw new LoopEncounteredException();
            }
            _loop++;
        }

        internal void Close() {
            Output.Close();
        }

        // TODO: refactor to take account of conditional branches
        public void Explore() {
            CheckLoop();
            if (Pc != Origin) {
                string routineIntro = $"\n;;\n;; Subroutine at ${Pc:X06}\n;;";
                Routines.Add(Pc, routineIntro);
                PrintLine(routineIntro);
            }
            bool IsReturned = false;
            while (!Invalid && !IsReturned) {
                int currPc = _pc;
                bool alreadyPrinted = false;
                Opcode op = new(Opcodes[Rom.GetByte(Pc)]);
                op.SetSize(Flags);
                op.SetOperand(Rom, Pc);
                Invalid = op.IsInvalid();
                if (op.CanModifyMode())
                    op.GetModifierFromOp().UpdateProcessorFlags(ref _flags, RomStack);
                if (op.IsReturnable()) {
                    var returnable = op.GetReturnableFromOp(Pc);
                    alreadyPrinted = true;
                    if (Routines.ContainsKey(Rom.SnesToPc(returnable.GetDestination()))) {
                        int oldPc = Pc + op.Size;
                        returnable.UpdateDestination(Rom, ref _pc);     // temporarily update the Pc for the Print
                        PrintLine(op, currPc, op.Size);
                        Pc = oldPc;
                    } else {
                        returnable.PushReturn(RomStack).UpdateDestination(Rom, ref _pc);
                        PrintLine(op, currPc, op.Size);
                        Explore();
                    }
                } else if (op.IsJumpable()) {
                    op.GetJumpableFromOp().UpdateDestination(Rom, ref _pc);
                } else {
                    IsReturned = op.IsIndirectJumpable() || op.IsReturning();
                    if (op.IsReturning()) {
                        op.GetReturningFromOp(RomStack).Return(ref _pc);
                    } else {
                        Pc += op.Size;
                    }
                }
                if (!alreadyPrinted)
                    PrintLine(op, currPc, op.Size);
            }
        }
        // the printing functions are both bad and the logic in the whole thing is a bit fucked
        private void PrintLine(string text) {
            Output.WriteLine(text, Pc);
        }
        private void PrintLine(Opcode op, int currPc, int size) {
            if (Labels.ContainsKey(currPc))
                Output.WriteLine($"{Labels[currPc]}:", currPc);
            if (op.IsBranchable()) {
                int branchPc = op.IsJumpable() ? Pc : Pc + (sbyte)op.Operand;       // if it's BRA/BRL (aka jumpable) the Pc already got updated, if it isn't we need to update it now
                if (Labels.ContainsKey(branchPc)) {     // if not found, use the new one
                    Output.WriteLine($"\t{op.ToStringWithLabel(Labels[branchPc]),-20};${Rom.PcToSnes(currPc):X06}{"",-10}{Flags.FlagsToString()}", currPc);
                } else {
                    Label newLabel = new Label(branchPc, Rom);
                    Labels.Add(branchPc, newLabel);
                    Output.WriteLine($"\t{op.ToStringWithLabel(newLabel),-20};${Rom.PcToSnes(currPc):X06}{"",-10}{Flags.FlagsToString()}", currPc);    // branch doesn't exist and points to a previous pc address, let's see if we can add it
                    if (branchPc < currPc)
                        Output.WriteLineAtPc($"{newLabel}:", branchPc);
                }
            } else if (op.IsJumpable() || op.IsReturnable()) {
                Label label = new(Pc, Rom);
                if (!Labels.ContainsKey(Pc)) {     // if not found, add the new one and use it
                    Labels.Add(Pc, label);
                }
                Output.WriteLine($"\t{op.ToStringWithLabel(label),-20};${Rom.PcToSnes(currPc):X06}{"",-10}{Flags.FlagsToString()}", currPc);
            } else {
                Output.WriteLine($"\t{op.ToStringWithSize(size),-20};${Rom.PcToSnes(currPc):X06}{"",-10}{Flags.FlagsToString()}", currPc);
            }
        }
    }
}
