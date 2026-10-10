using ProjectOdyssey.Common;
using ProjectOdyssey.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace ProjectOdyssey.Engine
{
    public static class VirtualKeyMapper
    {
        private const ushort VK_SPACE = 0x20; // 32
        private const ushort VK_OEM_1 = 0xBA; // 186, the ; key on a UK / US layout

        private static readonly ushort[] defaultKeyBinds4k = [68, 70, 74, 75];                                  // D F J K
        private static readonly ushort[] defaultKeyBinds5k = [68, 70, VK_SPACE, 74, 75];                        // D F _ J K
        private static readonly ushort[] defaultKeyBinds6k = [83, 68, 70, 74, 75, 76];                          // S D F J K L
        private static readonly ushort[] defaultKeyBinds7k = [83, 68, 70, VK_SPACE, 74, 75, 76];                // S D F _ J K L
        private static readonly ushort[] defaultKeyBinds8k = [65, 83, 68, 70, 74, 75, 76, VK_OEM_1];            // A S D F J K L ;
        private static readonly ushort[] defaultKeyBinds9k = [65, 83, 68, 70, VK_SPACE, 74, 75, 76, VK_OEM_1];  // A S D F _ J K L ;
        private static readonly ushort[] defaultKeyBinds10k = [81, 87, 69, 82, 86, 78, 85, 73, 79, 80];         // Q W E R V N U I O P 

        public static Result<ushort[]> GetManiaBindings(ushort keyCount)
        {
            if (keyCount < 4 || keyCount > 10)
            {
                return Result<ushort[]>.Err($"[WARN] Unsupported key count: {keyCount}. Must be between 4 and 10.");
            }

            switch (keyCount)
            {
                case 4:
                    return Result<ushort[]>.Ok(defaultKeyBinds4k);
                case 5:
                    return Result<ushort[]>.Ok(defaultKeyBinds5k);
                case 6:
                    return Result<ushort[]>.Ok(defaultKeyBinds6k);
                case 7: 
                    return Result<ushort[]>.Ok(defaultKeyBinds7k);
                case 8:
                    return Result<ushort[]>.Ok(defaultKeyBinds8k);
                case 9:
                    return Result<ushort[]>.Ok(defaultKeyBinds9k);
                case 10:
                    return Result<ushort[]>.Ok(defaultKeyBinds10k);
            }

            // Should be unreachable, but just in case
            return Result<ushort[]>.Err($"[WARN] Unsupported key count: {keyCount}. Must be between 4 and 10.");
        }

        // old
        private static int VkeyToColumn7k(ushort key)
        {
            return key switch
            {
                83 => 0, // S
                68 => 1, // D
                70 => 2, // F
                32 => 3, // Space
                74 => 4, // J
                75 => 5, // K
                76 => 6, // L
                _ => throw new ArgumentException($"Invalid key code: {key}")
            };
        }
    }
}
