using System.Collections.Immutable;
using DotWrap.Configuration;
using Microsoft.CodeAnalysis;
using DotWrap.Generator.Extensions;

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

        // if (typeSymbol.SpecialType.IsBlittable())
        // {
        //     // no need to wrap blittable types
        //     return;
        // }
        // if (typeSymbol.GetBlittableExternalTypeAssignment() is not null)
        // {
        //     // no need to wrap types that have been explicitly assigned a blittable external type
        //     return;
        // }

        if (typeSymbol.IsRefLikeType)
        {
            Logger.LogWarning(
                $"Skipping inferred type '{typeSymbol.ToDisplayString()}' because it is a ref-like type."
            );
            return;
        }

        // even though I'm removing the nullable annotation, the comparision with includeNullibility is still
        // returning false for the same symbols and I'm not sure why
        var symbolComparer = typeSymbol.IsReferenceType
            ? SymbolEqualityComparer.Default
            : SymbolEqualityComparer.IncludeNullability;

        if (allExplicitTypes.Contains(typeSymbol, symbolComparer) || allInferedTypes.Contains(typeSymbol))
        {
            return;
        }
        if (typeSymbol.TypeKind is not TypeKind.Class and not TypeKind.Struct and not TypeKind.Structure and not TypeKind.Interface and not TypeKind.Array and not TypeKind.Enum)
        {
            Logger.LogWarning(
                $"Skipping inferred type '{typeSymbol.ToDisplayString()}' because it is a {typeSymbol.TypeKind}, but we were expecting a class, struct, interface, array, or enum."
            );
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
            Logger.LogWarning($"Exposing type as explicit: '{typeSymbolNamed.ToDisplayString()}'");
            allExplicitTypes.Add(typeSymbolNamed);
            explicitTypesToWrap.Enqueue(typeSymbolNamed);
        }
        else
        {
            Logger.LogWarning($"Exposing type as inferred: '{typeSymbol.ToDisplayString()}'");
            allInferedTypes.Add(typeSymbol);
            inferedTypesToWrap.Enqueue(typeSymbol);
        }
    }
}
