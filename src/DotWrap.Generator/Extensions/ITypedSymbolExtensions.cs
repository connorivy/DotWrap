using System;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class ITypedSymbolExtensions
{
    // extension(ITypeSymbol symbol)
    // {
    //     public string GetExposedCType(out bool isOriginalType)
    //     {
    //         isOriginalType = true;

    //         switch (symbol)
    //         {
    //             case { SpecialType: SpecialType.System_String }:
    //                 return "IntPtr"; // CString
    //             case { SpecialType: SpecialType.System_Boolean }:
    //                 return "bool";
    //             case { SpecialType: SpecialType.System_Double }:
    //                 return "double";
    //             case { SpecialType: SpecialType.System_Single }:
    //                 return "float";
    //             case { SpecialType: SpecialType.System_Byte or
    //                       SpecialType.System_SByte or
    //                       SpecialType.System_UInt16 or
    //                       SpecialType.System_UInt32 or
    //                       SpecialType.System_UInt64 or
    //                       SpecialType.System_Int16 or
    //                       SpecialType.System_Int32 or
    //                       SpecialType.System_Int64 }:
    //                 return "int";
    //             case { SpecialType: SpecialType.System_Void }:
    //                 return "void";
    //             default:
    //                 isOriginalType = false;
    //                 return symbol.ToDisplayString();
    //         }
    //     }
    // }
    public static string GetExposedCType(this ITypeSymbol symbol, out bool isOriginalType)
    {
        isOriginalType = true;

        switch (symbol)
        {
            case { SpecialType: SpecialType.System_String }:
                return "IntPtr"; // CString
            case { SpecialType: SpecialType.System_Boolean }:
                return "bool";
            case { SpecialType: SpecialType.System_Double }:
                return "double";
            case { SpecialType: SpecialType.System_Single }:
                return "float";
            case {
                SpecialType: SpecialType.System_Byte
                    or SpecialType.System_SByte
                    or SpecialType.System_UInt16
                    or SpecialType.System_UInt32
                    or SpecialType.System_UInt64
                    or SpecialType.System_Int16
                    or SpecialType.System_Int32
                    or SpecialType.System_Int64
            }:
                return "int";
            case { SpecialType: SpecialType.System_Void }:
                return "void";
            default:
                isOriginalType = false;
                return "int"; // Default to int for unsupported types
        }
    }
}
