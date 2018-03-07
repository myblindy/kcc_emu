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
}
