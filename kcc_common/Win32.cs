using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_common
{
    public static class Win32
    {
        [DllImport("user32.dll", EntryPoint = "MapVirtualKeyEx")]
        static private extern uint _MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
        public const uint MAPVK_VSC_TO_VK = 0x01;

        static public uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl)
        {
            // mappings this function fails at
            if(uMapType== MAPVK_VSC_TO_VK)
            {
                if (uCode == 205) return 0x27;                                      // RIGHT ARROW
                else if (uCode == 203) return 0x25;                                 // LEFT ARROW
                else if (uCode == 200) return 0x26;                                 // UP ARROW
                else if (uCode == 208) return 0x28;                                 // DOWN ARROW
                else if (uCode == 199) return 0x24;                                 // HOME
                else if (uCode == 207) return 0x23;                                 // END
                else if (uCode == 201) return 0x21;                                 // PAGE UP
                else if (uCode == 209) return 0x22;                                 // PAGE DOWN
            }

            return _MapVirtualKeyEx(uCode, uMapType, dwhkl);
        }
    }
}
