using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_common
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct KeyboardStatePacketType
    {
        [MarshalAs(UnmanagedType.I1)]
        public sbyte Type;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public byte[] KeyStateData;
    }

    public class KeyboardStatePacketWrapperType
    {
        public KeyboardStatePacketType Packet;
        public BitField KeyState;

        public KeyboardStatePacketWrapperType()
        {
            KeyState = new BitField(256);
            Packet = new KeyboardStatePacketType
            {
                Type = 2,
                KeyStateData = KeyState.Data
            };
        }

        public KeyboardStatePacketWrapperType(KeyboardStatePacketType packet)
        {
            Packet = packet;
            KeyState = new BitField(packet.KeyStateData);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct MouseStatePacketType
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

    public class MouseStatePacketWrapperType
    {
        public MouseStatePacketType Packet;

        public BitField Buttons;

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
            Buttons = new BitField(8);
            Packet = new MouseStatePacketType
            {
                Type = 1,
                ButtonsData = Buttons.Data
            };
        }

        public MouseStatePacketWrapperType(MouseStatePacketType rawpacket)
        {
            Packet = rawpacket;
            Buttons = new BitField(rawpacket.ButtonsData);
        }
    }
}
