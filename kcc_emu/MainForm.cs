using kcc_common;
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
    public partial class MainForm : Form
    {
        static Keyboard Keyboard;
        static AutoResetEvent KeyboardEvent = new AutoResetEvent(false);
        static Thread KeyboardThread = new Thread(KeyboardThreadHandler) { IsBackground = true };
        static System.Threading.Timer KeyboardNetworkTimer = new System.Threading.Timer(KeyboardNetworkTimerHandler);
        static Socket KeyboardNetworkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        static KeyboardStatePacketWrapperType KeyboardStatePacket = new KeyboardStatePacketWrapperType();
        static BitField LastKeyboardKeyState = new BitField(256);

        static Mouse Mouse;
        static AutoResetEvent MouseEvent = new AutoResetEvent(false);
        static Thread MouseThread = new Thread(MouseThreadHandler) { IsBackground = true };
        static System.Threading.Timer MouseNetworkTimer = new System.Threading.Timer(MouseNetworkTimerHandler);
        static Socket MouseNetworkSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        static MouseStatePacketWrapperType MouseStatePacket = new MouseStatePacketWrapperType();
        static BitField LastMouseButtonState = new BitField(8);

        static IPEndPoint IPEndPoint;

        private static void KeyboardThreadHandler()
        {
            var setvks = new HashSet<uint>();
            while (true)
            {
                KeyboardEvent.WaitOne();
                var state = Keyboard.GetCurrentState();

                lock (KeyboardThread)
                {
                    setvks.Clear();
                    setvks.AddRange(state.PressedKeys.Select(w => Win32.MapVirtualKeyEx((uint)w, Win32.MAPVK_VSC_TO_VK, IntPtr.Zero)));

                    for (uint vk = 0; vk < 255; ++vk)
                        if (setvks.Contains(vk))                                            // pressed
                        {
                            if (!LastKeyboardKeyState[(int)vk])                             // and previously released
                                if (vk == 0x7B)
                                    Application.OpenForms[0].BeginInvoke(new MethodInvoker(() => Application.OpenForms[0].Close()));            // queue a close message
                                else
                                    KeyboardStatePacket.KeyState[(int)vk] = true;
                        }
                        else if (LastKeyboardKeyState[(int)vk])                             // released and previously pressed
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
                MouseNetworkSocket.SendTo(MouseStatePacket.Packet.ToBytesArray(), IPEndPoint);
                LastMouseButtonState.CopyFrom(MouseStatePacket.Buttons);
                MouseStatePacket.XDelta = MouseStatePacket.YDelta = 0;
                MouseStatePacket.Packet.WheelDelta = 0;
            }
        }

        private static void KeyboardNetworkTimerHandler(object state)
        {
            lock (KeyboardThread)
            {
                KeyboardNetworkSocket.SendTo(KeyboardStatePacket.Packet.ToBytesArray(), IPEndPoint);
                LastKeyboardKeyState.CopyFrom(KeyboardStatePacket.KeyState);
            }
        }

        public MainForm()
        {
            InitializeComponent();

            var tmp = Settings.Default.DestinationIP.Split(':');
            IPEndPoint = new IPEndPoint(IPAddress.Parse(tmp[0]), int.Parse(tmp[1]));
            Text += " [" + Settings.Default.DestinationIP + "]";
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
    }
}
