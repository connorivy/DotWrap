using System.Runtime.InteropServices;
using DotWrap.Internal;

namespace DotWrap.Operations;

public static class ExceptionOps
{
    public static void HandleException(Exception e, IntPtr exceptionInfoPtr)
    {
        var exceptionInfo = Create(e);
        Marshal.StructureToPtr<ExceptionInfo>(exceptionInfo, exceptionInfoPtr, false);
    }

    internal static ExceptionInfo Create(Exception e)
    {
        return new()
        {
            Message = StringOperations.Alloc(e.Message),
            StackTrace = StringOperations.Alloc(e.StackTrace),
            InnerExceptionMessage = StringOperations.Alloc(e.InnerException?.Message),
            InnerExceptionStackTrace = StringOperations.Alloc(e.InnerException?.StackTrace),
        };
    }
}
