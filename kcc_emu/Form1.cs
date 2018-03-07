using kcc_emu.Properties;
using MoreLinq;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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
        static Thread KeyboardThread = new Thread(KeyboardThreadHandler) { IsBackground = true };
        static System.Threading.Timer KeyboardNetworkTimer = new System.Threading.Timer(KeyboardNetworkTimerHandler);
        static Socket KeyboardNetworkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct KeyboardStatePacketType
        {
            [MarshalAs(UnmanagedType.I1)]
            public sbyte Type;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] KeyStateData;
        }
        class KeyboardStatePacketWrapperType
        {
            public KeyboardStatePacketType Packet = new KeyboardStatePacketType();
            public BitField KeyState = new BitField(256);

            public KeyboardStatePacketWrapperType()
            {
                Packet.Type = 2;
                Packet.KeyStateData = KeyState.Data;
            }
        }
        static KeyboardStatePacketWrapperType KeyboardStatePacket = new KeyboardStatePacketWrapperType();
        static BitField LastKeyboardKeyState = new BitField(256);

        static Mouse Mouse;
        static AutoResetEvent MouseEvent = new AutoResetEvent(false);
        static Thread MouseThread = new Thread(MouseThreadHandler) { IsBackground = true };
        static System.Threading.Timer MouseNetworkTimer = new System.Threading.Timer(MouseNetworkTimerHandler);
        static Socket MouseNetworkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        struct MouseStatePacketType
        {
            [MarshalAs(UnmanagedType.I1)]
            public sbyte Type;

            [MarshalAs(UnmanagedType.I2)]
            public short XDelta, YDelta;

            [MarshalAs(UnmanagedType.I1)]
            public sbyte WheelDelta;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
            public byte[] ButtonsData;

            [MarshalAs(UnmanagedType.I1)]
            public sbyte ExtraData;
        }
        class MouseStatePacketWrapperType
        {
            public MouseStatePacketType Packet = new MouseStatePacketType();

            public BitField Buttons = new BitField(8);

            public short XDelta
            {
                get => IPAddress.NetworkToHostOrder(Packet.XDelta);
                set => Packet.XDelta = IPAddress.HostToNetworkOrder(value);
            }

            public short YDelta
            {
                get => IPAddress.NetworkToHostOrder(Packet.YDelta);
                set => Packet.YDelta = IPAddress.HostToNetworkOrder(value);
            }

            public MouseStatePacketWrapperType()
            {
                Packet.Type = 1;
                Packet.ButtonsData = Buttons.Data;
            }
        }
        static MouseStatePacketWrapperType MouseStatePacket = new MouseStatePacketWrapperType();
        static BitField LastMouseButtonState = new BitField(8);

        [DllImport("user32.dll")]
        static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);
        const uint MAPVK_VSC_TO_VK = 0x01;

        static IPEndPoint IPEndPoint;

        private static void KeyboardThreadHandler()
        {
            while (true)
            {
                KeyboardEvent.WaitOne();
                var state = Keyboard.GetCurrentState();

                lock (KeyboardThread)
                {
                    var setvks = state.PressedKeys.Select(w => MapVirtualKeyEx((uint)w, MAPVK_VSC_TO_VK, IntPtr.Zero)).ToHashSet();
                    for (uint vk = 0; vk < 255; ++vk)
                        if (setvks.Contains(vk))
                            if (vk == 0x7B)
                                Application.OpenForms[0].BeginInvoke(new MethodInvoker(() => Application.OpenForms[0].Close()));            // queue a close message
                            else
                                KeyboardStatePacket.KeyState[(int)vk] = true;
                        else
                            KeyboardStatePacket.KeyState[(int)vk] = false;
                }
            }
        }

        private static void MouseThreadHandler()
        {
            while (true)
            {
                MouseEvent.WaitOne();
                var state = Mouse.GetCurrentState();

                lock (MouseThread)
                {
                    MouseStatePacket.XDelta += (short)state.X;
                    MouseStatePacket.YDelta += (short)state.Y;
                    for (int idx = 0; idx < state.Buttons.Length; ++idx)
                        if (MouseStatePacket.Buttons[idx] == LastMouseButtonState[idx])
                            MouseStatePacket.Buttons[idx] = state.Buttons[idx];
                    MouseStatePacket.Packet.WheelDelta += (sbyte)(state.Z / 120);
                }
            }
        }

        private static void MouseNetworkTimerHandler(object state)
        {
            lock (MouseThread)
            {
                MouseNetworkSocket.SendTo(ToBytesArray(MouseStatePacket.Packet), IPEndPoint);
                LastMouseButtonState.CopyFrom(MouseStatePacket.Buttons);
            }
        }

        private static void KeyboardNetworkTimerHandler(object state)
        {
            lock (KeyboardThread)
            {
                KeyboardNetworkSocket.SendTo(ToBytesArray(KeyboardStatePacket.Packet), IPEndPoint);
                LastKeyboardKeyState.CopyFrom(KeyboardStatePacket.KeyState);
            }
        }

        public Form1()
        {
            InitializeComponent();

            var tmp = Settings.Default.DestinationIP.Split(':');
            IPEndPoint = new IPEndPoint(IPAddress.Parse(tmp[0]), int.Parse(tmp[1]));
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            var di = new DirectInput();

            Keyboard = new Keyboard(di);
            //Keyboard.SetCooperativeLevel(Handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            Keyboard.SetNotification(KeyboardEvent);
            Keyboard.Acquire();
            KeyboardThread.Start();
            KeyboardNetworkTimer.Change(100, 100);

            Mouse = new Mouse(di);
            //Mouse.SetCooperativeLevel(Handle, CooperativeLevel.Exclusive | CooperativeLevel.Background);
            Mouse.SetNotification(MouseEvent);
            Mouse.Acquire();
            MouseThread.Start();
            MouseNetworkTimer.Change(100, 20);
        }

        private void tmrMouseMove_Tick(object sender, EventArgs e) =>
            Cursor.Position = new Point(Left + Width / 2, Top + Height / 2);

        public static unsafe byte[] ToBytesArray(object o)
        {
            int size = Marshal.SizeOf(o);

            byte[] arr = new byte[size];
            var mem = stackalloc byte[size];
            var memptr = new IntPtr(mem);
            Marshal.StructureToPtr(o, memptr, true);
            Marshal.Copy(memptr, arr, 0, size);

            return arr;
        }
    }
}
