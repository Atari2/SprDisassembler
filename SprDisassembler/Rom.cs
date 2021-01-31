using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SprDisassembler {
    class Rom {
        byte[] Data { get; set; }
        bool Header { get; set; }

        int HeaderSize { get; set; } = 0;
        bool Sa1 { get; set; }

        public Rom(string filename) {
            if (!File.Exists(filename)) {
                Console.WriteLine("The specified rom file couldn't be found");
                throw new FileNotFoundException();
            }
            Data = File.ReadAllBytes(filename);
            HeaderSize = Data.Length & 0x7FFF;
            if (HeaderSize != 0)
                Header = true;
            Sa1 = Data[0x7FD5 + HeaderSize] == 0x23;
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
            if (Sa1) {
                if (snes_addr >= 0xC00000) {
                    return (snes_addr & 0x7FFFFF) + (Header ? HeaderSize : 0);
                }
                if (snes_addr >= 0x800000) {
                    snes_addr -= 0x400000;
                }
                return ((snes_addr & 0x7F0000) >> 1 | (snes_addr & 0x7FFF)) + (Header ? HeaderSize : 0);
            } else {
                return ((snes_addr & 0x7F0000) >> 1 | (snes_addr & 0x7FFF)) + (Header ? HeaderSize : 0);
            }
        }

        public int PcToSnes(int pc_addr) {
            if (Header)
                pc_addr -= HeaderSize;
            if (Sa1) {
                if (pc_addr >= 0x400000) {
                    return (pc_addr & 0x3FFFFF) | 0xC00000;
                } else if (pc_addr >= 0x200000) {
                    return ((pc_addr << 1) & 0x3F0000) | (pc_addr & 0x7FFF) | 0x808000;
                } else {
                    return ((pc_addr << 1) & 0x3F0000) | (pc_addr & 0x7FFF) | 0x8000;
                }
            } else {
                return ((pc_addr << 1) & 0x7F0000) | (pc_addr & 0x7FFF) | 0x8000;
            }
        }
    }
}
