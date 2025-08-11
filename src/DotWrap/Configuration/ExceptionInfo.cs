using System.Runtime.InteropServices;

namespace DotWrap.Configuration;

[StructLayout(LayoutKind.Sequential)]
public readonly struct ExceptionInfo
{
    public IntPtr Message { get; init; }
    public IntPtr StackTrace { get; init; }
    public IntPtr InnerExceptionMessage { get; init; }
    public IntPtr InnerExceptionStackTrace { get; init; }
}
