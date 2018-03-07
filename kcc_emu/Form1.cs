using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace kcc_emu
{
    public partial class Form1 : Form
    {
        static Keyboard Keyboard;
        static AutoResetEvent KeyboardEvent = new AutoResetEvent(false);
        static Thread KeyboardThread = new Thread(KeyboardThreadHandler);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct KeyboardStatePacketType
        {
            [MarshalAs(UnmanagedType.U1)]
            public byte Type;

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(BitFieldMarshal256))]
            public BitField KeyboardState;

            public KeyboardStatePacketType(int _)
            {
                Type = 2;
                KeyboardState = new BitField(256);
            }
        }
        static KeyboardStatePacketType KeyboardStatePacket = new KeyboardStatePacketType(0);

        static Mouse Mouse;
        static AutoResetEvent MouseEvent = new AutoResetEvent(false);
        static Thread MouseThread = new Thread(MouseThreadHandler);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MouseStatePacketType
        {
            [MarshalAs(UnmanagedType.U1)]
            public byte Type;

            [MarshalAs(UnmanagedType.I2)]
            public short XDelta, YDelta;

            [MarshalAs(UnmanagedType.I1)]
            public sbyte WheelDelta;

            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(BitFieldMarshal8))]
            public BitField Buttons;

            [MarshalAs(UnmanagedType.U1)]
            public byte ExtraData;

            public MouseStatePacketType(int _)
            {
                Type = 1;
                XDelta = YDelta = WheelDelta = 0;
                Buttons = new BitField(8);
                ExtraData = 0;
            }
        }
        static MouseStatePacketType MouseStatePacket = new MouseStatePacketType(0);

        static volatile bool AppClosing = false;

        [DllImport("user32.dll")]
        static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
        const uint MAPVK_VSC_TO_VK = 0x01;

        private static void KeyboardThreadHandler()
        {
            while (!AppClosing)
            {
                KeyboardEvent.WaitOne();
                if (!AppClosing)
                {
                    var state = Keyboard.GetCurrentState();

                    lock (KeyboardThread)
                    {
                        KeyboardStatePacket.KeyboardState.Clear();
                        foreach (var vk in state.PressedKeys.Select(w => MapVirtualKeyEx((uint)w, MAPVK_VSC_TO_VK, IntPtr.Zero)))
                            if (vk == 0x7B)
                                Application.OpenForms[0].Invoke(new MethodInvoker(() => Application.OpenForms[0].Close()));
                            else
                                KeyboardStatePacket.KeyboardState[(int)vk] = true;
                    }
                }
            }
        }

        private static void MouseThreadHandler()
        {
            while (!AppClosing)
            {
                MouseEvent.WaitOne();
                if (!AppClosing)
                {
                    var state = Mouse.GetCurrentState();

                    lock (MouseThread)
                    {
                        MouseStatePacket.XDelta = (short)state.X;
                        MouseStatePacket.YDelta = (short)state.Y;
                        for (int idx = 0; idx < state.Buttons.Length; ++idx)
                            MouseStatePacket.Buttons[idx] = state.Buttons[idx];
                        MouseStatePacket.WheelDelta = (sbyte)(state.Z / 120);
                    }
                }
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            AppClosing = true;

            KeyboardEvent.Set();
            Keyboard.Unacquire();

            MouseEvent.Set();
            Mouse.Unacquire();

            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var di = new DirectInput();

            Keyboard = new Keyboard(di);
            //Keyboard.SetCooperativeLevel(Handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            Keyboard.SetNotification(KeyboardEvent);
            Keyboard.Acquire();
            KeyboardThread.Start();

            Mouse = new Mouse(di);
            //Mouse.SetCooperativeLevel(Handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            Mouse.SetNotification(MouseEvent);
            Mouse.Acquire();
            MouseThread.Start();
        }

        private void tmrMouseMove_Tick(object sender, EventArgs e)
        {
            Cursor.Position = new Point(Left + Width / 2, Top + Height / 2);
        }
    }
}
