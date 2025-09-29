using System.Collections.Immutable;
using DotWrap.Configuration;
using Microsoft.CodeAnalysis;
using DotWrap.Generator.Extensions;
using DotWrap.Generator.Builders.Class;

namespace DotWrap.Generator.Builders;

public class GlobalContext(
    HashSet<INamedTypeSymbol> allExplicitTypes,
    HashSet<ITypeSymbol> allInferedTypes,
    Queue<ITypeSymbol> inferedTypesToWrap,
    Queue<INamedTypeSymbol> explicitTypesToWrap,
    ImmutableHashSet<IAssemblySymbol> assembliesToExpose
)
{
    public void AddDiscoveredType(ITypeSymbol typeSymbol)
    {
        AddSingleDiscoveredType(typeSymbol);
        foreach (var nested in typeSymbol.GetTypeArguments() ?? [])
        {
            AddDiscoveredType(nested);
        }
    }

    private readonly HashSet<string> addedWrapperNames = new();
    private void AddSingleDiscoveredType(ITypeSymbol typeSymbol)
    {
        if (
            typeSymbol.IsReferenceType
            && typeSymbol.NullableAnnotation is NullableAnnotation.Annotated
        )
        {
            // remove nullable annotation for reference types
            typeSymbol = typeSymbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        if (typeSymbol.IsRefLikeType)
        {
            Logger.LogWarning(
                $"Skipping wrapper gen for type '{typeSymbol.ToDisplayString()}' because it is a ref-like type."
            );
            return;
        }

        if (typeSymbol.GetTypeArguments()?.Any(t => t.TypeKind == TypeKind.TypeParameter) ?? false)
        {
            Logger.LogWarning(
                $"Skipping wrapper gen for type '{typeSymbol.ToDisplayString()}' because it is an open generic type."
            );
            return;
        }

        // // even though I'm removing the nullable annotation, the comparision with includeNullibility is still
        // // returning false for the same symbols and I'm not sure why
        // var symbolComparer = typeSymbol.IsReferenceType
        //     ? SymbolEqualityComparer.Default
        //     : SymbolEqualityComparer.IncludeNullability;

        // if (
        //     // allExplicitTypes.Contains(typeSymbol, symbolComparer)
        //     // || allInferedTypes.Contains(typeSymbol, symbolComparer))
        //     allExplicitTypes.Contains(typeSymbol, SymbolEqualityComparer.IncludeNullability)
        //     || allInferedTypes.Contains(typeSymbol, SymbolEqualityComparer.IncludeNullability))
        // {
        //     return;
        // }

        if (typeSymbol.TypeKind is not TypeKind.Class and not TypeKind.Struct and not TypeKind.Structure and not TypeKind.Interface and not TypeKind.Array and not TypeKind.Enum)
        {
            Logger.LogWarning(
                $"Skipping inferred type '{typeSymbol.ToDisplayString()}' because it is a {typeSymbol.TypeKind}, but we were expecting a class, struct, interface, array, or enum."
            );
            return;
        }

        ClassBuilderContext classBuilderContext = new(this, typeSymbol, new());
        var wrapperName = classBuilderContext.FullyQualifiedWrapperName;
        if (!addedWrapperNames.Add(wrapperName))
        {
            return;
        }

        var exposeEntireAssembly = assembliesToExpose.Contains(
            typeSymbol.ContainingAssembly,
            SymbolEqualityComparer.Default
        );
        var isPublic = typeSymbol.DeclaredAccessibility == Accessibility.Public;
        var shouldBeExplicit = (isPublic && exposeEntireAssembly) || typeSymbol.GetDotWrapExposeAttribute() is not null;

        if (shouldBeExplicit)
        {
            if (typeSymbol is not INamedTypeSymbol typeSymbolNamed)
            {
                Logger.LogWarning(
                    $"Skipping exposed type '{typeSymbol.ToDisplayString()}' because it is not a named type."
                );
                return;
            }
            allExplicitTypes.Add(typeSymbolNamed);
            explicitTypesToWrap.Enqueue(typeSymbolNamed);
        }
        else
        {
            allInferedTypes.Add(typeSymbol);
            inferedTypesToWrap.Enqueue(typeSymbol);
        }
    }
}

public sealed class CustomTypeSymbolComparer : IEqualityComparer<ITypeSymbol>
{
    public static readonly CustomTypeSymbolComparer Instance = new();

    public bool Equals(ITypeSymbol? x, ITypeSymbol? y)
    {
        if (x is null || y is null)
            return x is null && y is null;

        return AreTypesEqualIgnoringOuterNullability(x, y);
    }

    public int GetHashCode(ITypeSymbol obj)
    {
        var stripped = StripOuterNullability(obj);
        return ComputeHash(stripped);
    }

    private static ITypeSymbol StripOuterNullability(ITypeSymbol symbol)
    {
        if (symbol.NullableAnnotation == NullableAnnotation.Annotated &&
            symbol.IsReferenceType)
        {
            return symbol.WithNullableAnnotation(NullableAnnotation.NotAnnotated);
        }

        return symbol;
    }

    private static int ComputeHash(ITypeSymbol symbol)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + symbol.TypeKind.GetHashCode();
            hash = hash * 31 + symbol.Name.GetHashCode();

            if (symbol is INamedTypeSymbol named && named.IsGenericType)
            {
                foreach (var arg in named.TypeArguments)
                {
                    hash = hash * 31 + SymbolEqualityComparer.Default.GetHashCode(arg);
                }
            }

            return hash;
        }
    }

    private static bool AreTypesEqualIgnoringOuterNullability(ITypeSymbol a, ITypeSymbol b)
    {
        a = StripOuterNullability(a);
        b = StripOuterNullability(b);

        if (a.TypeKind != b.TypeKind || a.Name != b.Name)
            return false;

        if (a is INamedTypeSymbol na && b is INamedTypeSymbol nb)
        {
            if (na.IsGenericType != nb.IsGenericType || na.TypeArguments.Length != nb.TypeArguments.Length)
                return false;

            for (int i = 0; i < na.TypeArguments.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(na.TypeArguments[i], nb.TypeArguments[i]))
                    return false;
            }
        }

        return true;
    }
}