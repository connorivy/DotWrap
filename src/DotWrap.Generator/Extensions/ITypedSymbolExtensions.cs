using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class ITypedSymbolExtensions
{
    extension(ITypeSymbol symbol)
    {
        public string GetExposedType(out bool isOriginalType)
        {
            isOriginalType = true;

            switch (symbol)
            {
                case { SpecialType: SpecialType.System_SByte }:
                    return "sbyte";
                case { SpecialType: SpecialType.System_Byte }:
                    return "byte";
                case { SpecialType: SpecialType.System_Int16 }:
                    return "short";
                case { SpecialType: SpecialType.System_UInt16 }:
                    return "ushort";
                case { SpecialType: SpecialType.System_Int32 }:
                    return "int";
                case { SpecialType: SpecialType.System_UInt32 }:
                    return "uint";
                case { SpecialType: SpecialType.System_Int64 }:
                    return "long"; 
                case { SpecialType: SpecialType.System_UInt64 }:
                    return "ulong"; 
                case { SpecialType: SpecialType.System_Single }:
                    return "float";
                case { SpecialType: SpecialType.System_Double }:
                    return "double";
                
                case { SpecialType: SpecialType.System_IntPtr }:
                    return "IntPtr";
                case { SpecialType: SpecialType.System_Void }:
                    return "void";
                case { SpecialType: SpecialType.System_Char }:
                    return "char";

                // Begin types that don't match original type, but are close enough to not need a wrapper
                case ITypeSymbol when symbol.Name == "Half" && symbol.ContainingNamespace?.ToString() == "System":
                    return "float";


                // Begin types that don't match original type
                case IArrayTypeSymbol:
                    isOriginalType = false;
                    return $"IntPtr";
                case { SpecialType: SpecialType.System_Boolean }:
                    isOriginalType = false;
                    return "int";
                case { SpecialType: SpecialType.System_String }:
                    isOriginalType = false;
                    return "IntPtr";
                default:
                    isOriginalType = false;
                    return "int"; // Default to int for unsupported types
            }
        }
    }
}
