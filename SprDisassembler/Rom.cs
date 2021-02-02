using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SprDisassembler {

    public enum Mapper {
        Lorom,
        SA1Rom,
        FullSA1Rom
    }

    public class Rom {
        readonly int[] SA1Banks = { 0 << 20, 1 << 20, -1, -1, 2 << 20, 3 << 20, -1, -1 };
        byte[] Data { get; set; }
        bool Header { get; set; }

        int HeaderSize { get; set; } = 0;
        Mapper Mapper { get; set; } = Mapper.Lorom;

        public Rom(string filename) {
            if (!File.Exists(filename)) {
                Console.WriteLine("The specified rom file couldn't be found");
                throw new FileNotFoundException();
            }
            Data = File.ReadAllBytes(filename);
            HeaderSize = Data.Length & 0x7FFF;
            if (HeaderSize != 0)
                Header = true;
            if (Data[0x7FD5 + HeaderSize] == 0x23) {
                if (Data[0x7FD7 + HeaderSize] == 0x0D) {
                    Mapper = Mapper.FullSA1Rom;
                } else {
                    Mapper = Mapper.SA1Rom;
                }
            } else {
                Mapper = Mapper.Lorom;
            }
        }

        public byte GetByte(int addr) {
            if (addr > Data.Length - 1) throw new IndexOutOfRangeException();
            return Data[addr];
        }

        public ushort GetWord(int addr) {
            if (addr > Data.Length - 2) throw new IndexOutOfRangeException();
            return (ushort)(Data[addr] | (Data[addr + 1] << 8));
        }

        public uint GetLong(int addr) {
            if (addr > Data.Length - 3) throw new IndexOutOfRangeException();
            return (uint)(Data[addr] | (Data[addr + 1] << 8) | (Data[addr + 2] << 16));
        }

        public uint GetLongPtr(int addr) {
            return (uint)((Data[addr] << 16) | (Data[addr + 1] << 8) | Data[addr + 2]);
        }

        public ushort GetWordPtr(int addr) {
            return (ushort)((Data[addr] << 8) | Data[addr + 1]);
        }
 
        public byte[] GetBuffer(int addr, int size) {
            if (addr + size > Data.Length)
                return Array.Empty<byte>();
            byte[] buf = new byte[size];
            Array.Copy(Data, addr, buf, 0, size);
            return buf;
        }

        public ReadOnlySpan<byte> GetReadOnlyBuffer(int addr, int size) {
            if (addr + size > Data.Length)
                return Array.Empty<byte>();
            return Data.AsSpan(addr, size);
        }

        public int SnesToPc(int snes_addr) {
            if (Mapper == Mapper.Lorom) {
                if ((snes_addr & 0xFE0000) == 0x7E0000 || (snes_addr & 0x408000) == 0x000000 || (snes_addr & 0x708000) == 0x700000)
                    return -1;
                snes_addr = (snes_addr & 0x7F0000) >> 1 | (snes_addr & 0x7FFF);
            } else if (Mapper == Mapper.SA1Rom) {
                if ((snes_addr & 0x408000) == 0x008000) {
                    snes_addr = SA1Banks[(snes_addr & 0xE00000) >> 21] | ((snes_addr & 0x1F0000) >> 1) | (snes_addr & 0x007FFF);
                } else if ((snes_addr & 0xC00000) == 0xC00000) {
                    snes_addr = SA1Banks[((snes_addr & 0x100000) >> 20) | ((snes_addr & 0x200000) >> 19)] | (snes_addr & 0x0FFFFF);
                } else {
                    snes_addr = -1;
                }
            } else if (Mapper == Mapper.FullSA1Rom) {
                if ((snes_addr & 0xC00000) == 0xC00000) {
                    snes_addr = (snes_addr & 0x3FFFFF) | 0x400000;
                } else if ((snes_addr & 0xC00000) == 0x000000 || (snes_addr & 0xC00000) == 0x800000) {
                    if ((snes_addr & 0x008000) == 0x000000)
                        return -1;
                    snes_addr = (snes_addr & 0x800000) >> 2 | (snes_addr & 0x3F0000) >> 1 | (snes_addr & 0x7FFF);
                } else {
                    return -1;
                }
            } else {
                return -1;
            }
            return snes_addr + (Header ? HeaderSize : 0);
        }

        public int PcToSnes(int pc_addr) {
            if (Header)
                pc_addr -= HeaderSize;

            if (Mapper == Mapper.Lorom) {
                return ((pc_addr << 1) & 0x7F0000) | (pc_addr & 0x7FFF) | 0x8000;
            } else if (Mapper == Mapper.SA1Rom) {
                for (int i = 0; i < 8; i++) {
                    if (SA1Banks[i] == (pc_addr & 0x700000)) {
                        return 0x008000 | (i << 21) | ((pc_addr & 0x0F8000) << 1) | (pc_addr & 0x7FFF);
                    }
                }
            } else if (Mapper == Mapper.FullSA1Rom) {
                if ((pc_addr & 0x400000) == 0x400000) {
                    return pc_addr | 0xC00000;
                }
                if ((pc_addr & 0x600000) == 0x000000) {
                    return ((pc_addr << 1) & 0x3F0000) | 0x8000 | (pc_addr & 0x7FFF);
                }
                if ((pc_addr & 0x600000) == 0x200000) {
                    return 0x800000 | ((pc_addr << 1) & 0x3F0000) | 0x8000 | (pc_addr & 0x7FFF);
                }
            }
            return -1;
        }
    }
}
