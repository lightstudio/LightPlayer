using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Light.Text
{
    internal static class Chardet
    {
        static bool Initialized = false;
        [DllImport("Light")]
        static extern void InitializeChardetAPI();
        [DllImport("Light")]
        static unsafe extern int ChardetDetectBytes(byte* text, int length, byte* codePage);

        static public unsafe string DetectCodepage(byte[] bytes)
        {
            if (!Initialized)
            {
                InitializeChardetAPI();
                Initialized = true;
            }
            fixed (byte* b = bytes)
            {
                var buffer = stackalloc byte[64];
                IntPtr ptr = new IntPtr(buffer);
                var ret = ChardetDetectBytes(b, bytes.Length, buffer);
                if (ret != 0)
                    return null;
                var str = Marshal.PtrToStringAnsi(ptr);
                return str;
            }
        }
    }
}
