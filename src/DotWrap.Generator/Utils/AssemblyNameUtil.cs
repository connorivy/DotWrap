using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Utils;

public static class AssemblyNameUtils
{
    /// <summary>
    /// takes a type symbol and returns its assembly qualified name.
    /// </summary>
    /// <param name="symbol"></param>
    /// <returns></returns>
    public static string GetAssemblyQualifiedName(this ITypeSymbol symbol)
    {
        return GetTypeNameWithAssembly(symbol);
    }

    private static string GetTypeNameWithAssembly(ITypeSymbol symbol)
    {
        var typeName = GetTypeName(symbol);
        var assemblyName = GetAssemblyName(symbol);
        return $"{typeName}, {assemblyName}";
    }

    private static string GetTypeName(ITypeSymbol symbol)
    {
        switch (symbol)
        {
            case IArrayTypeSymbol arrayType:
                return $"{GetTypeName(arrayType.ElementType)}[]";

            case INamedTypeSymbol namedType when namedType.IsGenericType:
                var sb = new StringBuilder();

                // Get the full namespace and name without generic parameters from the original definition
                var originalDefinition = namedType.OriginalDefinition;
                var fullName = GetFullTypeName(originalDefinition);
                sb.Append(fullName);
                sb.Append('`');

                // Use the original definition's arity (number of type parameters it declares)
                sb.Append(originalDefinition.Arity);

                if (namedType.TypeArguments.Length > 0)
                {
                    sb.Append("[[");

                    for (int i = 0; i < namedType.TypeArguments.Length; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append("],[");
                        }
                        sb.Append(GetTypeNameWithAssembly(namedType.TypeArguments[i]));
                    }

                    sb.Append("]]");
                }

                return sb.ToString();

            default:
                return GetFullTypeName(symbol);
        }
    }

    private static string GetFullTypeName(ITypeSymbol symbol)
    {
        // Build the full type name including namespace
        var parts = new List<string>();

        // Add the type name
        parts.Add(symbol.Name);

        // Walk up the containing types and namespaces
        var container = symbol.ContainingSymbol;
        while (container != null)
        {
            if (container is INamedTypeSymbol containingType)
            {
                parts.Add(containingType.Name);
            }
            else if (
                container is INamespaceSymbol namespaceSymbol
                && !namespaceSymbol.IsGlobalNamespace
            )
            {
                parts.Add(namespaceSymbol.Name);
            }
            container = container.ContainingSymbol;
        }

        // Reverse to get correct order (namespace.type)
        parts.Reverse();
        return string.Join(".", parts);
    }

    private static string GetAssemblyName(ITypeSymbol symbol)
    {
        // Get the actual underlying type to retrieve assembly info
        ITypeSymbol targetSymbol = symbol;

        // For arrays, get the element type's assembly
        while (targetSymbol is IArrayTypeSymbol arrayType)
        {
            targetSymbol = arrayType.ElementType;
        }

        // For generic types, we want the assembly of the generic definition, not the type arguments
        if (targetSymbol is INamedTypeSymbol namedType && namedType.IsGenericType)
        {
            targetSymbol = namedType.OriginalDefinition;
        }

        var assembly = targetSymbol.ContainingAssembly;
        if (assembly?.Identity != null)
        {
            var identity = assembly.Identity;
            var publicKeyToken = identity.PublicKeyToken.IsEmpty
                ? "null"
                : string.Join("", identity.PublicKeyToken.Select(b => b.ToString("x2")));

            return $"{identity.Name}, Version={identity.Version}, Culture=neutral, PublicKeyToken={publicKeyToken}";
        }

        throw new InvalidOperationException(
            $"Assembly identity for type '{symbol.ToDisplayString()}' is null or empty."
        );
    }
}
