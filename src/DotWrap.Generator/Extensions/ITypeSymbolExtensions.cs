using DotWrap.Configuration;
using DotWrap.Generator.Builders.Method;
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

        public bool SkipWrapperMethodGeneration()
        {
            if (symbol.SpecialType.IsBlittable())
            {
                return true;
            }
            if (MethodBuilder.GetBlittableExternalTypeAssignment(symbol) is not null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// returns the assembly qualified name of the type symbol.
        /// </summary>
        /// <returns></returns>
        public string GetAssemblyQualifiedName()
        {
            if (symbol is IArrayTypeSymbol arrayType)
            {
                // Handle arrays: e.g. System.Int32[], System.Runtime, ...
                string elementTypeName = GetTypeName(arrayType.ElementType);
                var assembly = symbol.ContainingAssembly;
                if (assembly == null || assembly.Identity == null)
                    return $"{elementTypeName}[]";
                var identity = assembly.Identity;
                var publicKeyToken = identity.PublicKeyToken != null && identity.PublicKeyToken.Length > 0
                    ? string.Concat(identity.PublicKeyToken.Select(b => b.ToString("x2")))
                    : "null";
                var assemblyDetails = $"{identity.Name}, Version={identity.Version}, Culture={(string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName)}, PublicKeyToken={publicKeyToken}";
                return $"{elementTypeName}[], {assemblyDetails}";
            }
            else if (symbol is INamedTypeSymbol namedType)
            {
                string typeName = GetFullTypeName(namedType);
                var assembly = symbol.ContainingAssembly;
                if (assembly == null || assembly.Identity == null)
                    return typeName;
                var identity = assembly.Identity;
                var publicKeyToken = identity.PublicKeyToken != null && identity.PublicKeyToken.Length > 0
                    ? string.Concat(identity.PublicKeyToken.Select(b => b.ToString("x2")))
                    : "null";
                var assemblyDetails = $"{identity.Name}, Version={identity.Version}, Culture={(string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName)}, PublicKeyToken={publicKeyToken}";
                if (namedType.TypeArguments.Length > 0)
                {
                    var genericArgs = string.Join(",", namedType.TypeArguments.Select(GetGenericArg));
                    return $"{typeName}[{genericArgs}], {assemblyDetails}";
                }
                else
                {
                    return $"{typeName}, {assemblyDetails}";
                }
            }
            else
            {
                string typeName = GetTypeName(symbol);
                var assembly = symbol.ContainingAssembly;
                if (assembly == null || assembly.Identity == null)
                    return typeName;
                var identity = assembly.Identity;
                var publicKeyToken = identity.PublicKeyToken != null && identity.PublicKeyToken.Length > 0
                    ? string.Concat(identity.PublicKeyToken.Select(b => b.ToString("x2")))
                    : "null";
                var assemblyDetails = $"{identity.Name}, Version={identity.Version}, Culture={(string.IsNullOrEmpty(identity.CultureName) ? "neutral" : identity.CultureName)}, PublicKeyToken={publicKeyToken}";
                return $"{typeName}, {assemblyDetails}";
            }

            // Helper: Get full type name for nested types and generics
            static string GetFullTypeName(INamedTypeSymbol symbol)
            {
                var parts = new List<string>();
                var current = symbol;
                while (current != null)
                {
                    var name = current.Name;
                    if (current.TypeArguments.Length > 0)
                        name += $"`{current.TypeArguments.Length}";
                    parts.Insert(0, name);
                    current = current.ContainingType;
                }
                var ns = symbol.ContainingNamespace?.ToDisplayString() ?? "";
                return string.IsNullOrEmpty(ns) ? string.Join("+", parts) : $"{ns}.{string.Join("+", parts)}";
            }

            // Helper: Get generic argument string for assembly qualified name
            static string GetGenericArg(ITypeSymbol arg)
            {
                if (arg is INamedTypeSymbol nestedNamedType && nestedNamedType.TypeArguments.Length > 0)
                {
                    return $"[{nestedNamedType.GetAssemblyQualifiedName()}]";
                }
                if (arg is IArrayTypeSymbol arrayType)
                {
                    return $"[{arrayType.GetAssemblyQualifiedName()}]";
                }
                string argTypeName = GetTypeName(arg);
                var argAssembly = arg.ContainingAssembly?.Identity;
                if (argAssembly == null)
                    return $"[{argTypeName}]";
                var argPublicKeyToken = argAssembly.PublicKeyToken != null && argAssembly.PublicKeyToken.Length > 0
                    ? string.Concat(argAssembly.PublicKeyToken.Select(b => b.ToString("x2")))
                    : "null";
                var argAssemblyDetails = $"{argAssembly.Name}, Version={argAssembly.Version}, Culture={(string.IsNullOrEmpty(argAssembly.CultureName) ? "neutral" : argAssembly.CultureName)}, PublicKeyToken={argPublicKeyToken}";
                return $"[{argTypeName}, {argAssemblyDetails}]";
            }

            // Helper: Get type name for primitives and other types
            static string GetTypeName(ITypeSymbol symbol)
            {
                return symbol.SpecialType switch
                {
                    SpecialType.System_Int32 => "System.Int32",
                    SpecialType.System_Int64 => "System.Int64",
                    SpecialType.System_Int16 => "System.Int16",
                    SpecialType.System_UInt32 => "System.UInt32",
                    SpecialType.System_UInt64 => "System.UInt64",
                    SpecialType.System_UInt16 => "System.UInt16",
                    SpecialType.System_Byte => "System.Byte",
                    SpecialType.System_SByte => "System.SByte",
                    SpecialType.System_Single => "System.Single",
                    SpecialType.System_Double => "System.Double",
                    SpecialType.System_Char => "System.Char",
                    SpecialType.System_Boolean => "System.Boolean",
                    SpecialType.System_String => "System.String",
                    SpecialType.System_Object => "System.Object",
                    _ => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", "")
                };
            }
        }

    }
}
