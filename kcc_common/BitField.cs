using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_common
{
    public class BitField
    {
        public byte[] Data { get; private set; }
        public int DataLength { get; private set; }

        public BitField(int len)
        {
            Data = new byte[len / 8];
            DataLength = len;
        }

        public BitField(byte[] data)
        {
            Data = data;
            DataLength = data.Length * 8;
        }

        public bool this[int idx]
        {
            get => (Data[idx / 8] & (1 << idx % 8)) > 0;
            set
            {
                if (value)
                    Data[idx / 8] |= (byte)(1 << idx % 8);
                else
                    Data[idx / 8] &= (byte)(~(1 << idx % 8));
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
}
