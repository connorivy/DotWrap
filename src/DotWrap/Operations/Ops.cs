using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace DotWrap.Operations;

public static class Ops
{
    public static unsafe void CopyBlittableArrayInfoToNumpyArray<T>(
        T[] arr,
        IntPtr numpyArr,
        int length
    )
    {
        Span<T> span = arr;
        Span<T> numpySpan = MemoryMarshal.CreateSpan(
            ref Unsafe.AsRef<T>(numpyArr.ToPointer()),
            length
        );
        span.CopyTo(numpySpan);
    }

    public static unsafe void CopyBlittableEnumerableInfoToNumpyArray<T>(
        IEnumerable<T> enumerable,
        IntPtr numpyArr,
        int length
    )
    {
        var arr = enumerable.ToArray();
        CopyBlittableArrayInfoToNumpyArray(arr, numpyArr, length);
    }

    public static unsafe void CopyNonBlittableEnumerableInfoToNumpyArray<T>(
        IEnumerable<T> enumerable,
        IntPtr numpyArr,
        int length
    )
    {
        var arr = enumerable
            .Select(x =>
            {
                var handle = GCHandle.Alloc(x, GCHandleType.Normal);
                return GCHandle.ToIntPtr(handle);
            })
            .ToArray();
        Span<nint> span = arr;
        Span<nint> numpySpan = MemoryMarshal.CreateSpan(
            ref Unsafe.AsRef<nint>(numpyArr.ToPointer()),
            length
        );
        span.CopyTo(numpySpan);
    }

    public static unsafe Guid PointerToGuid(IntPtr ptr)
    {
        return Unsafe.ReadUnaligned<Guid>(ptr.ToPointer());
    }
}
