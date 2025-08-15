using System.Runtime.InteropServices;

namespace DotWrap.Internal;

[StructLayout(LayoutKind.Sequential)]
internal readonly struct ExceptionInfo
{
    public IntPtr Message { get; init; }
    public IntPtr StackTrace { get; init; }
    public IntPtr InnerExceptionMessage { get; init; }
    public IntPtr InnerExceptionStackTrace { get; init; }
}

// [StructLayout(LayoutKind.Sequential)]
// internal readonly struct NullableDto<T>
//     where T : struct
// {
//     public byte HasValue { get; init; }
//     public T Value { get; init; }
// }

[StructLayout(LayoutKind.Sequential)]
internal readonly struct NullableDto
{
    public byte HasValue { get; init; }
    public IntPtr Value { get; init; }
}
