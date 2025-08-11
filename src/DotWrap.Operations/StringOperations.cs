using System.Runtime.InteropServices;

namespace DotWrap.Operations
{
    public static class StringOperations
    {
        public static IntPtr Alloc(string? str)
        {
            if (string.IsNullOrEmpty(str))
            {
                return IntPtr.Zero;
            }

            var ptr = Marshal.StringToHGlobalAnsi(str);
            return ptr;
        }
    }
}
