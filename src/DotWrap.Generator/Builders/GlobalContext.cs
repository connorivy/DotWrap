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

        // Use consistent equality comparison for duplicate detection
        // Check if this type is already discovered as explicit or inferred type
        var alreadyExplicit = typeSymbol is INamedTypeSymbol namedType && 
                             allExplicitTypes.Contains(namedType, SymbolEqualityComparer.IncludeNullability);
        var alreadyInferred = allInferedTypes.Contains(typeSymbol, SymbolEqualityComparer.IncludeNullability);
        
        if (alreadyExplicit || alreadyInferred)
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
