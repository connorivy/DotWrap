using DotWrap.Configuration;
using DotWrap.Generator.Builders.Method;
using DotWrap.Utils;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

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

            var originalReturnTypeString = symbol switch
            {
                { SpecialType: SpecialType.System_SByte } => "sbyte",
                { SpecialType: SpecialType.System_Byte } => "byte",
                { SpecialType: SpecialType.System_Int16 } => "short",
                { SpecialType: SpecialType.System_UInt16 } => "ushort",
                { SpecialType: SpecialType.System_Int32 } => "int",
                { SpecialType: SpecialType.System_UInt32 } => "uint",
                { SpecialType: SpecialType.System_Int64 } => "long",
                { SpecialType: SpecialType.System_UInt64 } => "ulong",
                { SpecialType: SpecialType.System_Single } => "float",
                { SpecialType: SpecialType.System_Double } => "double",
                { SpecialType: SpecialType.System_IntPtr } => "IntPtr",
                { SpecialType: SpecialType.System_Void } => "void",
                _ => null
            };

            if (originalReturnTypeString is not null)
            {
                return originalReturnTypeString;
            }
            isOriginalType = false;

            return symbol switch
            {
                ITypeSymbol when symbol.Name == "Half" && symbol.ContainingNamespace?.ToString() == "System" => "float",
                { SpecialType: SpecialType.System_Char } => "int",
                { SpecialType: SpecialType.System_Boolean } => "int",
                { SpecialType: SpecialType.System_String } => "IntPtr",
                _ => "IntPtr",// Default to IntPtr for unsupported types
            };
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
                    return ExportedType.SByte;
                case { SpecialType: SpecialType.System_Byte }:
                    return ExportedType.Byte;
                case { SpecialType: SpecialType.System_Int16 }:
                    return ExportedType.Int16;
                case { SpecialType: SpecialType.System_UInt16 }:
                    return ExportedType.UInt16;
                case { SpecialType: SpecialType.System_Int32 }:
                    return ExportedType.Int32;
                case { SpecialType: SpecialType.System_UInt32 }:
                    return ExportedType.UInt32;
                case { SpecialType: SpecialType.System_Int64 }:
                    return ExportedType.Int64;
                case { SpecialType: SpecialType.System_UInt64 }:
                    return ExportedType.UInt64;
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
                    return ExportedType.Int32;
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
            if (symbol.SpecialType.IsBlittable())
            {
                flags |= TypeSpecialCaseFlags.DirectlyBlittable;
            }
            if (symbol.GetBlittableExternalTypeAssignment() is not null)
            {
                flags |= TypeSpecialCaseFlags.IndirectlyBlittable;
            }

            return flags;
        }

        public string? GetBlittableExternalTypeAssignment()
        {
            if (symbol is null)
            {
                throw new ArgumentNullException(nameof(symbol));
            }

            if (symbol.Name == "Half" && symbol.ContainingNamespace?.ToString() == "System")
            {
                return @$"
            var {ExportedResult} = (float){InternalResult};";
            }
            else if (symbol.SpecialType == SpecialType.System_String)
            {
                return @$"
            var {ExportedResult} = global::DotWrap.BuiltIn.CString.Create({InternalResult});";
            }
            else if (symbol.SpecialType == SpecialType.System_Boolean)
            {
                return @$"
            var {ExportedResult} = {InternalResult} ? 1 : 0;";
            }
            else if (symbol.SpecialType == SpecialType.System_Char)
            {
                return @$"
            var {ExportedResult} = (int){InternalResult};";
            }
            else if (symbol.TypeKind == TypeKind.Enum)
            {
                var namedType =
                    symbol as INamedTypeSymbol
                    ?? throw new ArgumentException(
                        "Expected typeSymbol to be a named type symbol for enum handling.",
                        nameof(symbol)
                    );
                var underlyingType =
                    namedType.EnumUnderlyingType
                    ?? throw new InvalidOperationException("Enum underlying type is null.");
                return @$"
            var {ExportedResult} = ({underlyingType.ToDisplayString()}){InternalResult};";
            }
            return null;
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
#pragma warning disable CS0618 // Type or member is obsolete
            return new ExportedTypeId(
                AssemblyNameUtils.GetSimplifiedAssemblyName(Utils.AssemblyNameUtils.GetAssemblyQualifiedName(symbol))
                // symbol.ContainingNamespace?.ToDisplayString() ?? "global",
                // symbol.Name,
                // symbol.GetTypeArguments()?.Select(arg => DotWrapUtils.NormalizeCsTypeName(arg.ToDisplayString())) ?? []
            );
#pragma warning restore CS0618 // Type or member is obsolete
        }

        public ExportedTypeInstanceInfo GetExportedTypeInstance(string? genericName)
        {
            return new ExportedTypeInstanceInfo()
            {
                DefinitionId = symbol.GetExportedTypeId(),
                DefinitionGenericArgs = symbol.GetTypeArguments()?.Select(arg => arg.ToDisplayString())?.ToArray() ?? [],
                GenericName = genericName,
                IsNullable = symbol.NullableAnnotation == NullableAnnotation.Annotated && symbol.IsReferenceType
            };
        }

        public bool SkipWrapperMethodGeneration()
        {
            if (symbol.SpecialType.IsBlittable())
            {
                return true;
            }
            if (symbol.GetBlittableExternalTypeAssignment() is not null)
            {
                return true;
            }
            return false;
        }
    }
}
