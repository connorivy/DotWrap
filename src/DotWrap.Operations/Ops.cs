using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotWrap;
using static DotWrap.Internal.Constants;

namespace DotWrap.Operations;

public static class Ops
{
    public static unsafe void CopyArrayInfoToNumpyArray<T>(T[] arr, IntPtr numpyArr, int length)
    {
        Span<T> span = arr;
        Span<T> numpySpan = MemoryMarshal.CreateSpan(
            ref Unsafe.AsRef<T>(numpyArr.ToPointer()),
            length
        );
        span.CopyTo(numpySpan);
    }
}
