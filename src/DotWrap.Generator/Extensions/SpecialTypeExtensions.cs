using System;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class SpecialTypeExtensions
{
    /// <summary>
    /// https://learn.microsoft.com/en-us/dotnet/framework/interop/blittable-and-non-blittable-types
    /// </summary>
    /// <param name="classSymbol"></param>
    /// <returns></returns>
    public static bool IsBlittable(this SpecialType classSymbol) =>
        classSymbol switch
        {
            SpecialType.System_Byte => true,
            SpecialType.System_SByte => true,
            SpecialType.System_Int16 => true,
            SpecialType.System_UInt16 => true,
            SpecialType.System_Int32 => true,
            SpecialType.System_UInt32 => true,
            SpecialType.System_Int64 => true,
            SpecialType.System_UInt64 => true,
            SpecialType.System_IntPtr => true,
            SpecialType.System_UIntPtr => true,
            SpecialType.System_Single => true,
            SpecialType.System_Double => true,

            // added by me, not included in the web docs
            SpecialType.System_Void => true,
            _ => false,
        };
}
