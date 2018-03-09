using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace kcc_common
{
    public static class Extensions
    {
        public static unsafe byte[] ToBytesArray(this object o)
        {
            int size = Marshal.SizeOf(o);

            byte[] arr = new byte[size];
            var mem = stackalloc byte[size];
            var memptr = new IntPtr(mem);
            Marshal.StructureToPtr(o, memptr, true);
            Marshal.Copy(memptr, arr, 0, size);

            return arr;
        }

        public static unsafe T ToObject<T>(this byte[] bytearray) where T : new()
        {
            int len = Marshal.SizeOf<T>();
            var mem = stackalloc byte[len];
            var memptr = new IntPtr(mem);
            Marshal.Copy(bytearray, 0, memptr, len);
            var obj = (T)Marshal.PtrToStructure(memptr, typeof(T));

            return obj;
        }

        public static void AddRange<T>(this ICollection<T> source, IEnumerable<T> items)
        {
            foreach (var item in items)
                source.Add(item);
        }
    }
}
