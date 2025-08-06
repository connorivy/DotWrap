using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class ITypedSymbolExtensions
{
    extension(ITypeSymbol symbol)
    {
        public string GetExposedType(out bool isOriginalType)
        {
            isOriginalType = true;

            if (symbol.GetUnderlyingEnumType() is INamedTypeSymbol underlyingType)
            {
                isOriginalType = false;
                symbol = underlyingType;
            }

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
                case { SpecialType: SpecialType.System_Boolean }:
                    isOriginalType = false;
                    return "int";
                case { SpecialType: SpecialType.System_String }:
                    isOriginalType = false;
                    return "IntPtr";
                default:
                    isOriginalType = false;
                    return "IntPtr"; // Default to IntPtr for unsupported types
            }
        }

        public ExportedType GetExportedType(out bool isOriginalType)
        {
            isOriginalType = true;

            if (symbol.GetUnderlyingEnumType() is INamedTypeSymbol underlyingType)
            {
                isOriginalType = false;
                symbol = underlyingType;
            }

            switch (symbol)
            {
                case { SpecialType: SpecialType.System_SByte }:
                    return ExportedType.Sbyte;
                case { SpecialType: SpecialType.System_Byte }:
                    return ExportedType.Byte;
                case { SpecialType: SpecialType.System_Int16 }:
                    return ExportedType.Short;
                case { SpecialType: SpecialType.System_UInt16 }:
                    return ExportedType.Ushort;
                case { SpecialType: SpecialType.System_Int32 }:
                    return ExportedType.Int;
                case { SpecialType: SpecialType.System_UInt32 }:
                    return ExportedType.Uint;
                case { SpecialType: SpecialType.System_Int64 }:
                    return ExportedType.Long;
                case { SpecialType: SpecialType.System_UInt64 }:
                    return ExportedType.Ulong;
                case { SpecialType: SpecialType.System_Single }:
                    return ExportedType.Float;
                case { SpecialType: SpecialType.System_Double }:
                    return ExportedType.Double;

                case { SpecialType: SpecialType.System_IntPtr }:
                    return ExportedType.IntPtr;
                case { SpecialType: SpecialType.System_Void }:
                    return ExportedType.Void;
                case { SpecialType: SpecialType.System_Char }:
                    return ExportedType.Char;

                // Begin types that don't match original type, but are close enough to not need a wrapper
                case ITypeSymbol when symbol.Name == "Half" && symbol.ContainingNamespace?.ToString() == "System":
                    return ExportedType.Float;


                // Begin types that don't match original type
                case { SpecialType: SpecialType.System_Boolean }:
                    isOriginalType = false;
                    return ExportedType.Int;
                case { SpecialType: SpecialType.System_String }:
                    isOriginalType = false;
                    return ExportedType.IntPtr;
                default:
                    isOriginalType = false;
                    return ExportedType.IntPtr; // Default to IntPtr for unsupported types
            }
        }

        public INamedTypeSymbol? GetUnderlyingEnumType()
        {
            if (symbol.TypeKind != TypeKind.Enum)
            {
                return null;
            }

            var namedType = symbol as INamedTypeSymbol
                ?? throw new ArgumentException(
                    "Expected symbol to be a named type symbol for enum handling.",
                    nameof(symbol)
                );

            return namedType.EnumUnderlyingType;
        }

        public TypeSpecialCaseFlags GetSpecialCaseFlags()
        {
            TypeSpecialCaseFlags flags = TypeSpecialCaseFlags.None;

            if (symbol.TypeKind == TypeKind.Enum)
            {
                flags |= TypeSpecialCaseFlags.Enum;
            }
            if (symbol.TypeKind == TypeKind.Class)
            {
                flags |= TypeSpecialCaseFlags.Class;
            }
            if (symbol.TypeKind == TypeKind.Interface)
            {
                flags |= TypeSpecialCaseFlags.Interface;
            }
            if (symbol.TypeKind is TypeKind.Struct or TypeKind.Structure)
            {
                flags |= TypeSpecialCaseFlags.Struct;
            }
            if (symbol.IsStatic)
            {
                flags |= TypeSpecialCaseFlags.Static;
            }
            return flags;
        }
        public ITypeParameterSymbol[]? GetTypeParameters()
        {
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return null;
            }

            return [.. namedTypeSymbol.TypeParameters, .. symbol.GetContainingTypes().SelectMany(c => c.TypeParameters)];
        }
        public ITypeSymbol[]? GetTypeArguments()
        {
            if (symbol is not INamedTypeSymbol namedTypeSymbol)
            {
                return null;
            }

            return [.. namedTypeSymbol.TypeArguments, .. symbol.GetContainingTypes().SelectMany(c => c.TypeArguments)];
        }

        public IEnumerable<INamedTypeSymbol> GetContainingTypes()
        {
            var currentSymbol = symbol.ContainingSymbol;
            while (currentSymbol is INamedTypeSymbol containingSymbol)
            {
                yield return containingSymbol;
                currentSymbol = containingSymbol.ContainingSymbol;
            }
        }

        public ExportedTypeId GetExportedTypeId()
        {
            return new ExportedTypeId(
                symbol.ContainingNamespace?.ToDisplayString() ?? "global",
                symbol.Name,
                symbol.GetTypeArguments()?.Select(arg => arg.ToDisplayString()) ?? []
            );
        }

        public ExportedTypeInstanceInfo GetExportedTypeInstance(string? genericName)
        {
            return new ExportedTypeInstanceInfo()
            {
                DefinitionId = symbol.GetExportedTypeId(),
                DefinitionGenericArgs = symbol.GetTypeArguments()?.Select(arg => arg.ToDisplayString())?.ToArray() ?? [],
                GenericName = genericName,
                IsNullable = symbol.NullableAnnotation == NullableAnnotation.Annotated
            };
        }
    }
}
