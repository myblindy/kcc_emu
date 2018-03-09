using kcc_common;
using kcc_playback.Properties;
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

namespace kcc_playback
{
    public partial class MainForm : Form
    {
        Thread ListenerThread;

        public MainForm()
        {
            InitializeComponent();

            ListenerThread = new Thread(() =>
              {
                  var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                  socket.Bind(new IPEndPoint(IPAddress.Any, Settings.Default.ListeningPort));

                  var oldbuttons = new BitField(8);
                  var oldkeystate = new BitField(256);

                  var recvbuffer = new byte[40];
                  var rawmousepacket = new byte[Marshal.SizeOf<MouseStatePacketType>()];
                  var rawkeyboardpacket = new byte[Marshal.SizeOf<KeyboardStatePacketType>()];

                  while (true)
                  {
                      socket.Receive(recvbuffer);

                      switch (recvbuffer[0])
                      {
                          case 1:
                              {
                                  // mouse
                                  Array.Copy(recvbuffer, rawmousepacket, rawmousepacket.Length);
                                  var packetwrapper = new MouseStatePacketWrapperType(rawmousepacket.ToObject<MouseStatePacketType>());
                                  InputSimulator.ApplyPacket(packetwrapper, oldbuttons);
                                  oldbuttons.CopyFrom(packetwrapper.Buttons);
                              }

                              break;
                          case 2:
                              {
                                  // keyboard
                                  Array.Copy(recvbuffer, rawkeyboardpacket, rawkeyboardpacket.Length);
                                  var packetwrapper = new KeyboardStatePacketWrapperType(rawkeyboardpacket.ToObject<KeyboardStatePacketType>());
                                  InputSimulator.ApplyPacket(packetwrapper, oldkeystate);
                                  oldkeystate.CopyFrom(packetwrapper.KeyState);
                              }

                              break;
                          default:
                              continue;
                      }
                  }
              })
            { Name = "Listener Thread", IsBackground = true };
            ListenerThread.Start();
        }
    }
}
