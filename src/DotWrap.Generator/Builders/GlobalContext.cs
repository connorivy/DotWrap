using DotWrap.Configuration;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders;

public class GlobalContext(
    HashSet<INamedTypeSymbol> allExplicitTypes,
    HashSet<ITypeSymbol> allInferedTypes,
    List<ITypeSymbol> inferedTypesToWrap
)
{
    public void AddInferedType(ITypeSymbol typeSymbol)
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
                $"Skipping inferred type '{typeSymbol.ToDisplayString()}' because it is a ref-like type."
            );
            return;
        }

        // even though I'm removing the nullable annotation, the comparision with includeNullibility is still
        // returning false for the same symbols and I'm not sure why
        var symbolComparer = typeSymbol.IsReferenceType
            ? SymbolEqualityComparer.Default
            : SymbolEqualityComparer.IncludeNullability;

        if (
            !allInferedTypes.Add(typeSymbol)
            || allExplicitTypes.Contains(typeSymbol, symbolComparer)
        )
        {
            return;
        }

        if (
            typeSymbol.TypeKind
            is TypeKind.Class
                or TypeKind.Struct
                or TypeKind.Structure
                or TypeKind.Interface
                or TypeKind.Array
                or TypeKind.Enum
        )
        {
            inferedTypesToWrap.Add(typeSymbol);
        }
        else
        {
            Logger.LogWarning(
                $"Skipping inferred type '{typeSymbol.ToDisplayString()}' because it is not a class, struct, interface, array, or enum."
            );
        }
    }
}
