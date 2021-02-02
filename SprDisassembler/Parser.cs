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
            new Opcode(3, AddressingMode.Immediate, 0x09, "ORA"), // ORA #$xxxx | ORA #$xx
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x0A, "ASL"), // ASL A
            new Opcode(1, AddressingMode.Implied, 0x0B, "PHD"), // PHD
            new Opcode(3, AddressingMode.Absolute, 0x0C, "TSB"), // TSB $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x0D, "ORA"), // ORA $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x0E, "ASL"), // ASL $xxxx
            new Opcode(4, AddressingMode.AbsoluteLong, 0x0F, "ORA"), // ORA $xxxxxx
            new Opcode(2, AddressingMode.Relative, 0x10, "BPL"), // BPL $xx
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0x11, "ORA"), // ORA ($xx),y
            new Opcode(2, AddressingMode.DirectIndirect, 0x12, "ORA"), // ORA ($xx)
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0x13, "ORA"), // ORA ($xx,s),y
            new Opcode(2, AddressingMode.Direct, 0x14, "TRB"), // TRB $xx
            new Opcode(2, AddressingMode.DirectIndexedX, 0x15, "ORA"), // ORA $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX, 0x16, "ASL"), // ASL $xx,x
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0x17, "ORA"), // ORA [$xx],y
            new Opcode(1, AddressingMode.Implied, 0x18, "CLC"), // CLC
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0x19, "ORA"), // ORA $xxxx,y
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x1A, "INC"), // INC A
            new Opcode(1, AddressingMode.Implied, 0x1B, "TCS"), // TCS
            new Opcode(3, AddressingMode.Absolute, 0x1C, "TRB" ), // TRB $xxxx
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x1D, "ORA"), // ORA $xxxx,x
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x1E, "ASL"), // ASL $xxxx,x
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0x1F, "ORA"), // ORA $xxxxxx,x
            new Opcode(3, AddressingMode.Absolute, 0x20, "JSR"), // JSR $xxxx
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0x21, "AND"), // AND ($xx,x)
            new Opcode(4, AddressingMode.AbsoluteLong, 0x22, "JSL"), // JSL $xxxxxx
            new Opcode(2, AddressingMode.StackRelative, 0x23, "AND"), // AND $xx,s
            new Opcode(2, AddressingMode.Direct, 0x24, "BIT"), // BIT $xx
            new Opcode(2, AddressingMode.Direct, 0x25, "AND"), // AND $xx
            new Opcode(2, AddressingMode.Direct, 0x26, "ROL"), // ROL $xx
            new Opcode(2, AddressingMode.DirectIndirectLong, 0x27, "AND"), // AND [$xx]
            new Opcode(1, AddressingMode.Implied, 0x28, "PLP"), // PLP
            new Opcode(3, AddressingMode.Immediate, 0x29, "AND"), // AND #$xxxx | AND #$xx
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x2A, "ROL"), // ROL A
            new Opcode(1, AddressingMode.Implied, 0x2B, "PLD"), // PLD
            new Opcode(3, AddressingMode.Absolute, 0x2C, "BIT"), // BIT
            new Opcode(3, AddressingMode.Absolute, 0x2D, "AND"), // AND $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x2E, "ROL"), // ROL $xxxx
            new Opcode(4, AddressingMode.AbsoluteLong, 0x2F, "AND"), // AND $xxxxxx
            new Opcode(2, AddressingMode.Relative, 0x30, "BMI"), // BMI $xx
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0x31, "AND"), // AND ($xx),y
            new Opcode(2, AddressingMode.DirectIndirect, 0x32, "AND"), // AND ($xx)
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0x33, "AND"), // AND ($xx,s),y
            new Opcode(2, AddressingMode.DirectIndexedX, 0x34, "BIT"), // BIT $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX ,0x35, "AND"), // AND $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX ,0x36, "ROL"), // ROL $xx,x
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0x37, "AND"), // AND [$xx],y
            new Opcode(1, AddressingMode.Implied, 0x38, "SEC"), // SEC
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x39, "AND"), // AND $xxxx,y
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x3A, "DEC" ), // DEC A 
            new Opcode(1, AddressingMode.Implied, 0x3B, "TSC" ), // TSC
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x3C, "BIT"), // BIT $xxxx,x
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x3D, "AND"), // AND $xxxx,x
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x3E, "ROL"), // ROL $xxxx,x
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0x3F, "AND"), // AND $xxxxxx,x
            new Opcode(1, AddressingMode.Implied, 0x40, "RTI" ), // RTI
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0x41, "EOR"), // EOR ($xx,x)
            new Opcode(2, AddressingMode.Immediate, 0x42, "WDM"), // WDM #$xx
            new Opcode(2, AddressingMode.StackRelative, 0x43, "EOR"), // EOR $xx,s
            new Opcode(3, AddressingMode.BlockMove, 0x44, "MVP"), // MVP $xx,$xx
            new Opcode(2, AddressingMode.Direct, 0x45, "EOR"), // EOR $xx
            new Opcode(2, AddressingMode.Direct, 0x46, "LSR"), // LSR $xx
            new Opcode(2, AddressingMode.DirectIndirectLong, 0x47, "EOR"), // EOR [$xx]
            new Opcode(1, AddressingMode.Implied, 0x48, "PHA"), // PHA
            new Opcode(3, AddressingMode.Immediate, 0x49, "EOR"), // EOR #$xxxx | EOR #$xx
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x4A, "LSR"), // LSR A
            new Opcode(1, AddressingMode.Implied, 0x4B, "PHK"), // PHK
            new Opcode(3, AddressingMode.Absolute, 0x4C, "JMP"), // JMP $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x4D, "EOR"), // EOR $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x4E, "LSR"), // LSR $xxxx
            new Opcode(4, AddressingMode.AbsoluteLong, 0x4F, "EOR"), // EOR $xxxxxx
            new Opcode(2, AddressingMode.Relative, 0x50, "BVC"), // EOR $xx
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0x51, "EOR"), // EOR ($xx),y
            new Opcode(2, AddressingMode.DirectIndirect, 0x52, "EOR"), // EOR ($xx)
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0x53, "EOR"), // EOR ($xx,s),y
            new Opcode(3, AddressingMode.BlockMove, 0x54, "MVN"), // MVN $xx,$xx
            new Opcode(2, AddressingMode.DirectIndexedX, 0x55, "EOR"), // EOR $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX, 0x56, "LSR"), // LSR $xx,x
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0x57, "EOR"), // EOR [$xx],y
            new Opcode(1, AddressingMode.Implied, 0x58, "CLI"), // CLI
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0x59, "EOR"), // EOR $xxxx,y
            new Opcode(1, AddressingMode.Implied, 0x5A, "PHY"), // PHY
            new Opcode(1, AddressingMode.Implied, 0x5B, "TCD"), // TCD
            new Opcode(4, AddressingMode.AbsoluteLong, 0x5C, "JML" ), // JML $xxxxxx
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x5D, "EOR"), // EOR $xxxx,x
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x5E, "LSR"), // LSR $xxxx,x
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0x5F, "EOR"), // EOR $xxxxxx,x
            new Opcode(1, AddressingMode.Implied, 0x60, "RTS"),
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0x61, "ADC"), // ADC ($xx,x)
            new Opcode(3, AddressingMode.Absolute, 0x62, "PER"), // PER $xxxx
            new Opcode(2, AddressingMode.StackRelative, 0x63, "ADC"), // ADC $xx,s
            new Opcode(2, AddressingMode.Direct, 0x64, "STZ"), // STZ $xx
            new Opcode(2, AddressingMode.Direct, 0x65, "ADC"), // ADC $xx
            new Opcode(2, AddressingMode.Direct, 0x66, "ROR"), // ROR $xx
            new Opcode(2, AddressingMode.DirectIndirectLong, 0x67, "ADC"), // ADC [$xx]
            new Opcode(1, AddressingMode.Implied, 0x68, "PLA"), // PLA
            new Opcode(3, AddressingMode.Immediate, 0x69, "ADC"), // ADC #$xxxx
            new Opcode(1, AddressingMode.ImpliedAccumulator, 0x6A, "ROR"), // ROR A
            new Opcode(1, AddressingMode.Implied, 0x6B, "RTL"), // RTL
            new Opcode(3, AddressingMode.AbsoluteIndirect, 0x6C, "JMP"), // JMP ($xxxx)
            new Opcode(3, AddressingMode.Absolute, 0x6D, "ADC"), // ADC $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x6E, "ROR"), // ROR
            new Opcode(4, AddressingMode.AbsoluteLong, 0x6F, "ADC"), // ADC $xxxxxx
            new Opcode(2, AddressingMode.Relative, 0x70, "BVS"), // BVS $xx
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0x71, "ADC"), // ADC ($xx),y
            new Opcode(2, AddressingMode.DirectIndirect, 0x72, "ADC"), // ADC ($xx)
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0x73, "ADC"), // ADC ($xx,s),y
            new Opcode(2, AddressingMode.DirectIndexedX, 0x74, "STZ"), // STZ $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX, 0x75, "ADC"), // ADC $xx,x
            new Opcode(2, AddressingMode.DirectIndexedX, 0x76, "ROR"), // ROR $xx,x
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0x77, "ADC"), // ADC [$xx],y
            new Opcode(1, AddressingMode.Implied, 0x78, "SEI"), // SEI
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0x79, "ADC"), // ADC $xxxx,y
            new Opcode(1, AddressingMode.Implied, 0x7A, "PLY"), // PLY
            new Opcode(1, AddressingMode.Implied, 0x7B, "TDC"), // TDC
            new Opcode(3, AddressingMode.AbsoluteIndexedIndirect, 0x7C, "JMP"), // JMP ($xxxx,x)
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x7D, "ADC"), // ADC
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x7E, "ROR"), // ROR $xxxx,x
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0x7F, "ADC"), // ADC $xxxxxx,x
            new Opcode(2, AddressingMode.Relative, 0x80, "BRA"), // BRA $xx
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0x81, "STA"), // STA ($xx,x)
            new Opcode(3, AddressingMode.RelativeLong, 0x82, "BRL"), // BRL $xxxx
            new Opcode(2, AddressingMode.StackRelative, 0x83, "STA"), // STA $xx,s
            new Opcode(2, AddressingMode.Direct, 0x84, "STY"), // STY $xx
            new Opcode(2, AddressingMode.Direct, 0x85, "STA"), // STA $xx
            new Opcode(2, AddressingMode.Direct, 0x86, "STX"), // STX $xx
            new Opcode(2, AddressingMode.DirectIndirectLong, 0x87, "STA"), // STA [$xx]
            new Opcode(1, AddressingMode.Implied, 0x88, "DEY"), // DEY
            new Opcode(3, AddressingMode.Immediate, 0x89, "BIT"), // BIT #$xxxx | BIT #$xx
            new Opcode(1, AddressingMode.Implied, 0x8A, "TXA"), // TXA
            new Opcode(1, AddressingMode.Implied, 0x8B, "PHB"), // PHB
            new Opcode(3, AddressingMode.Absolute, 0x8C, "STY"), // STY $xxxx
            new Opcode(3, AddressingMode.Absolute, 0x8D, "STA"), // STA $xxxx   
            new Opcode(3, AddressingMode.Absolute, 0x8E, "STX"), // STX $xxxx
            new Opcode(4, AddressingMode.AbsoluteLong, 0x8F, "STA" ), // STA $xxxxxx
            new Opcode(2, AddressingMode.Relative, 0x90, "BCC"),
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0x91, "STA"),
            new Opcode(2, AddressingMode.DirectIndirect, 0x92, "STA"),
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0x93, "STA"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0x94, "STY"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0x95, "STA"),
            new Opcode(2, AddressingMode.DirectIndexedY, 0x96, "STX"),
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0x97, "STA"),
            new Opcode(1, AddressingMode.Implied, 0x98, "TYA"),
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0x99, "STA"),
            new Opcode(1, AddressingMode.Implied, 0x9A, "TXS"),
            new Opcode(1, AddressingMode.Implied, 0x9B, "TXY"),
            new Opcode(3, AddressingMode.Absolute, 0x9C, "STZ"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x9D, "STA"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0x9E, "STZ"),
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0x9F, "STA"),
            new Opcode(3, AddressingMode.Immediate, 0xA0, "LDY"),
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0xA1, "LDA"),
            new Opcode(3, AddressingMode.Immediate, 0xA2, "LDX"),
            new Opcode(2, AddressingMode.StackRelative, 0xA3, "LDA"),
            new Opcode(2, AddressingMode.Direct, 0xA4, "LDY"),
            new Opcode(2, AddressingMode.Direct, 0xA5, "LDA"),
            new Opcode(2, AddressingMode.Direct, 0xA6, "LDX"),
            new Opcode(2, AddressingMode.DirectIndirectLong, 0xA7, "LDA"),
            new Opcode(1, AddressingMode.Implied, 0xA8, "TAY"),
            new Opcode(3, AddressingMode.Immediate, 0xA9, "LDA"),
            new Opcode(1, AddressingMode.Implied, 0xAA, "TAX"),
            new Opcode(1, AddressingMode.Implied, 0xAB, "PLB"),
            new Opcode(3, AddressingMode.Absolute, 0xAC, "LDY"),
            new Opcode(3, AddressingMode.Absolute, 0xAD, "LDA"),
            new Opcode(3, AddressingMode.Absolute, 0xAE, "LDX"),
            new Opcode(4, AddressingMode.AbsoluteLong, 0xAF, "LDA"),
            new Opcode(2, AddressingMode.Relative, 0xB0, "BCS"),
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0xB1, "LDA"),
            new Opcode(2, AddressingMode.DirectIndirect, 0xB2, "LDA"),
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0xB3, "LDA"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xB4, "LDY"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xB5, "LDA"),
            new Opcode(2, AddressingMode.DirectIndexedY, 0xB6, "LDX"),
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0xB7, "LDA"),
            new Opcode(1, AddressingMode.Implied, 0xB8, "CLV"),
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0xB9, "LDA"),
            new Opcode(1, AddressingMode.Implied, 0xBA, "TSX"),
            new Opcode(1, AddressingMode.Implied, 0xBB, "TYX"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xBC, "LDY"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xBD, "LDA"),
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0xBE, "LDX"),
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0xBF, "LDA"),
            new Opcode(3, AddressingMode.Immediate, 0xC0, "CPY"),
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0xC1, "CMP"),
            new Opcode(2, AddressingMode.Immediate, 0xC2, "REP"),
            new Opcode(2, AddressingMode.StackRelative, 0xC3, "CMP"),
            new Opcode(2, AddressingMode.Direct, 0xC4, "CPY"),
            new Opcode(2, AddressingMode.Direct, 0xC5, "CMP"),
            new Opcode(2, AddressingMode.Direct, 0xC6, "DEC"),
            new Opcode(2, AddressingMode.DirectIndirectLong, 0xC7, "CMP"),
            new Opcode(1, AddressingMode.Implied, 0xC8, "INY"),
            new Opcode(3, AddressingMode.Immediate, 0xC9, "CMP"),
            new Opcode(1, AddressingMode.Implied, 0xCA, "DEX"),
            new Opcode(1, AddressingMode.Implied, 0xCB, "WAI"),
            new Opcode(3, AddressingMode.Absolute, 0xCC, "CPY"),
            new Opcode(3, AddressingMode.Absolute, 0xCD, "CMP"),
            new Opcode(3, AddressingMode.Absolute, 0xCE, "DEC"),
            new Opcode(4, AddressingMode.AbsoluteLong, 0xCF, "CMP"),
            new Opcode(2, AddressingMode.Relative, 0xD0, "BNE"),
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0xD1, "CMP"),
            new Opcode(2, AddressingMode.DirectIndirect, 0xD2, "CMP"),
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0xD3, "CMP"),
            new Opcode(2, AddressingMode.DirectIndirect, 0xD4, "PEI"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xD5, "CMP"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xD6, "DEC"),
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0xD7, "CMP"),
            new Opcode(1, AddressingMode.Implied, 0xD8, "CLD"),
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0xD9, "CMP"),
            new Opcode(1, AddressingMode.Implied, 0xDA, "PHX"),
            new Opcode(1, AddressingMode.Implied, 0xDB, "STP"),
            new Opcode(3, AddressingMode.AbsoluteIndirectLong, 0xDC, "JML"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xDD, "CMP"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xDE, "DEC"),
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0xDF, "CMP"),
            new Opcode(3, AddressingMode.Immediate, 0xE0, "CPX"),
            new Opcode(2, AddressingMode.DirectIndexedIndirect, 0xE1, "SBC"),
            new Opcode(2, AddressingMode.Immediate, 0xE2, "SEP"),
            new Opcode(2, AddressingMode.StackRelative, 0xE3, "SBC"),
            new Opcode(2, AddressingMode.Direct, 0xE4, "CPX"),
            new Opcode(2, AddressingMode.Direct, 0xE5, "SBC"),
            new Opcode(2, AddressingMode.Direct, 0xE6, "INC"),
            new Opcode(2, AddressingMode.DirectIndirectLong, 0xE7, "SBC"),
            new Opcode(1, AddressingMode.Implied, 0xE8, "INX"),
            new Opcode(3, AddressingMode.Immediate, 0xE9, "SBC"),
            new Opcode(1, AddressingMode.Implied, 0xEA, "NOP"),
            new Opcode(1, AddressingMode.Implied, 0xEB, "XBA"),
            new Opcode(3, AddressingMode.Absolute, 0xEC, "CPX"),
            new Opcode(3, AddressingMode.Absolute, 0xED, "SBC"),
            new Opcode(3, AddressingMode.Absolute, 0xEE, "INC"),
            new Opcode(4, AddressingMode.AbsoluteLong, 0xEF, "SBC"),
            new Opcode(2, AddressingMode.Relative, 0xF0, "BEQ"),
            new Opcode(2, AddressingMode.DirectIndirectIndexed, 0xF1, "SBC"),
            new Opcode(2, AddressingMode.DirectIndirect, 0xF2, "SBC"),
            new Opcode(2, AddressingMode.StackRelativeIndirectIndexed, 0xF3, "SBC"),
            new Opcode(3, AddressingMode.Absolute, 0xF4, "PEA"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xF5, "SBC"),
            new Opcode(2, AddressingMode.DirectIndexedX, 0xF6, "INC"),
            new Opcode(2, AddressingMode.DirectIndirectIndexedLong, 0xF7, "SBC"),
            new Opcode(1, AddressingMode.Implied, 0xF8, "SED"),
            new Opcode(3, AddressingMode.AbsoluteIndexedY, 0xF9, "SBC"),
            new Opcode(1, AddressingMode.Implied, 0xFA, "PLX"),
            new Opcode(1, AddressingMode.Implied, 0xFB, "XCE"),
            new Opcode(3, AddressingMode.AbsoluteIndexedIndirect, 0xFC, "JSR"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xFD, "SBC"),
            new Opcode(3, AddressingMode.AbsoluteIndexedX, 0xFE, "INC"),
            new Opcode(4, AddressingMode.AbsoluteIndexedLong, 0xFF, "SBC") // SBC $xxxxxx,x
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
            Label entryLabel = new Label(Origin, Rom);
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
                Opcode op = new Opcode(Opcodes[Rom.GetByte(Pc)]);
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
                Label label = new Label(Pc, Rom);
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
