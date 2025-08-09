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

                // Get the full type name with proper nested type separators and arity
                var fullName = GetFullTypeNameWithArity(namedType.OriginalDefinition);
                sb.Append(fullName);

                // Collect all type arguments from the entire type hierarchy
                var allTypeArguments = GetAllTypeArguments(namedType);

                if (allTypeArguments.Count > 0)
                {
                    sb.Append("[[");

                    for (int i = 0; i < allTypeArguments.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append("],[");
                        }
                        sb.Append(GetTypeNameWithAssembly(allTypeArguments[i]));
                    }

                    sb.Append("]]");
                }

                return sb.ToString();

            default:
                return GetFullTypeNameWithArity(symbol);
        }
    }

    private static List<ITypeSymbol> GetAllTypeArguments(INamedTypeSymbol namedType)
    {
        var allArguments = new List<ITypeSymbol>();

        // Walk up the containing type hierarchy and collect type arguments
        var current = namedType;
        var typeArgStack = new Stack<ITypeSymbol[]>();

        while (current != null)
        {
            if (current.TypeArguments.Length > 0)
            {
                typeArgStack.Push(current.TypeArguments.ToArray());
            }
            current = current.ContainingType;
        }

        // Add type arguments in the correct order (outermost first)
        while (typeArgStack.Count > 0)
        {
            allArguments.AddRange(typeArgStack.Pop());
        }

        return allArguments;
    }

    private static string GetFullTypeNameWithArity(ITypeSymbol symbol)
    {
        // Build the full type name including namespace, with proper nested type handling
        var containers = new List<ISymbol>();
        var current = symbol;

        // Add the current symbol
        containers.Add(current);

        // Walk up the containing symbols
        var container = current.ContainingSymbol;
        while (container != null)
        {
            if (
                container is INamedTypeSymbol
                || (container is INamespaceSymbol ns && !ns.IsGlobalNamespace)
            )
            {
                containers.Add(container);
            }
            container = container.ContainingSymbol;
        }

        // Reverse to get correct order (namespace.type)
        containers.Reverse();

        // Build the type name
        var sb = new StringBuilder();
        bool firstPart = true;
        bool inNestedTypes = false;

        foreach (var containerSymbol in containers)
        {
            if (!firstPart)
            {
                if (containerSymbol is INamedTypeSymbol && inNestedTypes)
                {
                    // Use + for nested types after we've started encountering types
                    sb.Append('+');
                }
                else
                {
                    // Use . for namespaces and the first type
                    sb.Append('.');
                }
            }
            firstPart = false;

            if (containerSymbol is INamedTypeSymbol namedType)
            {
                // Mark that we're now in types (not namespaces)
                if (!inNestedTypes)
                {
                    inNestedTypes = true;
                }

                sb.Append(namedType.Name);

                // Add arity for generic types
                if (namedType.Arity > 0)
                {
                    sb.Append('`');
                    sb.Append(namedType.Arity);
                }
            }
            else if (containerSymbol is INamespaceSymbol namespaceSymbol)
            {
                sb.Append(namespaceSymbol.Name);
            }
        }

        return sb.ToString();
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
