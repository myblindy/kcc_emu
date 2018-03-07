using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_emu
{
    class BitField
    {
        public byte[] Data { get; private set; }
        private int DataLen;

        public BitField(int len)
        {
            Data = new byte[len / 8];
            DataLen = len;
        }

        public bool this[int idx]
        {
            get => (Data[idx / 8] & (1 << idx & 0x0F)) > 0;
            set
            {
                if (value)
                    Data[idx / 8] |= (byte)(1 << idx & 0x0F);
                else
                    Data[idx / 8] &= (byte)(~(1 << idx & 0x0F));
            }
        }

        public void Clear()
        {
            for (int idx = 0; idx < Data.Length; ++idx)
                Data[idx] = 0;
        }

        public void CopyFrom(BitField src) =>
            Array.Copy(src.Data, Data, Data.Length);
    }

    class BitFieldMarshal256 : ICustomMarshaler
    {
        public void CleanUpManagedData(object ManagedObj)
        {
            throw new NotImplementedException();
        }

        public void CleanUpNativeData(IntPtr pNativeData) =>
            Marshal.FreeHGlobal(pNativeData);

        public int GetNativeDataSize() => 256 / 8;

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            var buffer = Marshal.AllocHGlobal(GetNativeDataSize());
            Marshal.Copy(((BitField)ManagedObj).Data,0, buffer, GetNativeDataSize());
            return buffer;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new NotImplementedException();
        }
    }

    class BitFieldMarshal8 : ICustomMarshaler
    {
        public void CleanUpManagedData(object ManagedObj)
        {
            throw new NotImplementedException();
        }

        public void CleanUpNativeData(IntPtr pNativeData) =>
            Marshal.FreeHGlobal(pNativeData);

        public int GetNativeDataSize() => 1;

        public IntPtr MarshalManagedToNative(object ManagedObj)
        {
            var buffer = Marshal.AllocHGlobal(GetNativeDataSize());
            Marshal.WriteByte(buffer, ((BitField)ManagedObj).Data[0]);
            return buffer;
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            throw new NotImplementedException();
        }
    }
}
