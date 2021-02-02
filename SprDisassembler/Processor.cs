using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

    public enum RegisterType {
        A,
        X,
        Y,
        S,
        DB,
        DP,
        PB,
        P,
        PC
    }

    public class Register {
        string Name { get => RegType.ToString(); }
        RegisterType RegType { get; set; }
        ushort Value;
        public Register(RegisterType type) {
            RegType = type;
            Value = 0;
        }

        public void UpdateValue(ushort newvalue, bool length) {
            if (length)
                Value = newvalue;
            else {
                Value &= 0xFF00;
                Value |= (ushort)(newvalue & 0xFF);
            }
        }

        public ushort GetValue(bool length) {
            if (length)
                return Value;
            else
                return (ushort)(Value & 0xFF);
        }
    }

    /// <summary>
    /// Extension methods for the ProcessorFlags enum
    /// </summary>
    public static class FlagsExtensions {

        /// <summary>
        /// Checks if specified Flags(s) is/are set.
        /// </summary>
        /// <param name="flags"> Instance to check </param>
        /// <param name="other"> Flags to check if they are set</param>
        /// <returns>true if flag(s) is/are set, false otherwise</returns>
        public static bool IsFlagSet(this ProcessorFlags flags, ProcessorFlags other) {
            return (flags & other) != 0;
        }

        /// <summary>
        /// Checks if specified Flags(s) is/are clear.
        /// </summary>
        /// <param name="flags"> Instance to check</param>
        /// <param name="other"> Flags to check if they are clear</param>
        /// <returns></returns>
        public static bool IsFlagClear(this ProcessorFlags flags, ProcessorFlags other) {
            return (flags & other) == 0;
        }
        /// <summary>
        /// Sets specified flag(s)
        /// </summary>
        /// <param name="flags"> Instance to set </param>
        /// <param name="other"> Flag(s) to set </param>
        /// <returns> new instance with specified flag set </returns>
        public static ProcessorFlags SetFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags |= other;
            return flags;
        }
        /// <summary>
        /// Unsets specified flag(s)
        /// </summary>
        /// <param name="flags"> Instance to unset </param>
        /// <param name="other"> Flag(s) to set </param>
        /// <returns> new instance with specified flags unset </returns>
        public static ProcessorFlags ClearFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags &= ((ProcessorFlags)0xFF ^ other);
            return flags;
        }
        /// <summary>
        /// Inverts specified flag(s)
        /// </summary>
        /// <param name="flags"> Instance to invert </param>
        /// <param name="other"> Flag(s) to invert </param>
        /// <returns> new instnace with specified flags inverted </returns>
        public static ProcessorFlags InvertFlag(this ProcessorFlags flags, ProcessorFlags other) {
            flags ^= other;
            return flags;
        }

        /// <summary>
        /// Converts flags to a string like "nvmxdizc", where uppercase means that a flag is active and lowercase means not active.
        /// </summary>
        /// <param name="flags">Flags to evaluate</param>
        /// <returns></returns>
        public static string FlagsToString(this ProcessorFlags flags) {
            char[] arr = new char[8];
            int i = 0;
            foreach (var type in (ProcessorFlags[])Enum.GetValues(typeof(ProcessorFlags))) {
                arr[i] = flags.IsFlagSet(type) ? type.ToString().ToUpper()[0] : type.ToString().ToLower()[0];
                i++;
            }
            Array.Reverse(arr);
            return new string(arr);
        }
    }
}
