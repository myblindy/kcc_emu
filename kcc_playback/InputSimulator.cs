using kcc_common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_playback
{
    static class InputSimulator
    {
        [DllImport("user32.dll", SetLastError = true)]
        static extern uint SendInput(uint nInputs,
            [MarshalAs(UnmanagedType.LPArray), In] Input[] pInputs,
            int cbSize);

        [StructLayout(LayoutKind.Sequential)]
        struct Input
        {
            public SendInputEventType type;
            public MouseKeybdhardwareInputUnion mkhi;
        }

        [StructLayout(LayoutKind.Explicit)]
        struct MouseKeybdhardwareInputUnion
        {
            [FieldOffset(0)]
            public MouseInputData mi;

            [FieldOffset(0)]
            public KeyboardInput ki;

            [FieldOffset(0)]
            public HardwareInput hi;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct KeyboardInput
        {
            public ushort wVk;
            public ushort wScan;
            public KeyboardEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        internal enum KeyboardEventFlags : uint
        {
            EXTENDEDKEY = 0x0001,
            KEYUP = 0x0002,
            SCANCODE = 0x0008,
            UNICODE = 0x0004
        }

        [StructLayout(LayoutKind.Sequential)]
        struct HardwareInput
        {
            public int uMsg;
            public short wParamL;
            public short wParamH;
        }

        struct MouseInputData
        {
            public int dx;
            public int dy;
            public int mouseData;
            public MouseEventFlags dwFlags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        [Flags]
        enum MouseEventFlags : uint
        {
            MOUSEEVENTF_MOVE = 0x0001,
            MOUSEEVENTF_LEFTDOWN = 0x0002,
            MOUSEEVENTF_LEFTUP = 0x0004,
            MOUSEEVENTF_RIGHTDOWN = 0x0008,
            MOUSEEVENTF_RIGHTUP = 0x0010,
            MOUSEEVENTF_MIDDLEDOWN = 0x0020,
            MOUSEEVENTF_MIDDLEUP = 0x0040,
            MOUSEEVENTF_XDOWN = 0x0080,
            MOUSEEVENTF_XUP = 0x0100,
            MOUSEEVENTF_WHEEL = 0x0800,
            MOUSEEVENTF_VIRTUALDESK = 0x4000,
            MOUSEEVENTF_ABSOLUTE = 0x8000,
            MOUSEEVENTF_MOVE_NOCOALESCE = 0x2000,
        }

        enum SendInputEventType : int
        {
            InputMouse,
            InputKeyboard,
            InputHardware
        }

        public static void ApplyPacket(MouseStatePacketWrapperType packetwrapper, BitField oldbuttons)
        {
            var input = new Input { type = SendInputEventType.InputMouse };
            input.mkhi.mi.dwFlags = MouseEventFlags.MOUSEEVENTF_MOVE | MouseEventFlags.MOUSEEVENTF_MOVE_NOCOALESCE | MouseEventFlags.MOUSEEVENTF_WHEEL;
            input.mkhi.mi.dx = packetwrapper.XDelta;
            input.mkhi.mi.dy = packetwrapper.YDelta;
            input.mkhi.mi.mouseData = packetwrapper.Packet.WheelDelta;

            if (packetwrapper.Buttons[0] != oldbuttons[0])
                input.mkhi.mi.dwFlags |= packetwrapper.Buttons[0] ? MouseEventFlags.MOUSEEVENTF_LEFTDOWN : MouseEventFlags.MOUSEEVENTF_LEFTUP;
            if (packetwrapper.Buttons[1] != oldbuttons[1])
                input.mkhi.mi.dwFlags |= packetwrapper.Buttons[1] ? MouseEventFlags.MOUSEEVENTF_RIGHTDOWN : MouseEventFlags.MOUSEEVENTF_RIGHTUP;
            if (packetwrapper.Buttons[2] != oldbuttons[2])
                input.mkhi.mi.dwFlags |= packetwrapper.Buttons[2] ? MouseEventFlags.MOUSEEVENTF_MIDDLEDOWN : MouseEventFlags.MOUSEEVENTF_MIDDLEUP;

            SendInput(1, new[] { input }, Marshal.SizeOf<Input>());
        }

        public static void ApplyPacket(KeyboardStatePacketWrapperType packetwrapper, BitField oldkeystate)
        {
            var inputs = new List<Input>();

            for (var vk = 0; vk < packetwrapper.KeyState.DataLength; ++vk)
            {
                var oldstate = oldkeystate[vk];
                var newstate = packetwrapper.KeyState[vk];

                if (oldstate != newstate)
                {
                    var input = new Input { type = SendInputEventType.InputKeyboard };
                    input.mkhi.ki.wVk = (ushort)vk;
                    input.mkhi.ki.dwFlags = newstate && !oldstate ? 0 : KeyboardEventFlags.KEYUP;
                    inputs.Add(input);
                }
            }

            SendInput((uint)inputs.Count, inputs.ToArray(), Marshal.SizeOf<Input>());
        }
    }
}
