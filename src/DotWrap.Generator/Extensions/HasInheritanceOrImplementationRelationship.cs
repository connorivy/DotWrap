using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Extensions;

public static class HasInheritanceOrImplementationRelationshipClass
{
    extension(ITypeSymbol typeSymbol)
    {
        public bool HasInheritanceOrImplementationRelationship(ITypeSymbol comparedSymbol)
        {
            // 1. Exact type match for typeSymbol or base of typeSymbol (e.g., List<int> == List<int>)
            if (typeSymbol.CurrentSymOrBaseOfCurrentMatchesCompareSymbol(comparedSymbol))
            {
                return true;
            }

            // 2. Type implements exact interface match (e.g., List<int> implements IList<int>)
            if (
                typeSymbol.AllInterfaces.Any(i =>
                    SymbolEqualityComparer.Default.Equals(i, comparedSymbol)
                )
            )
            {
                return true;
            }

            var namedExposeTypeSymbol = comparedSymbol as INamedTypeSymbol;
            if (namedExposeTypeSymbol is null)
            {
                return false; // not a named type, skip
            }

            // 3. Type is generic type definition (e.g., List<int> matches List<>)
            if (namedExposeTypeSymbol.IsGenericType)
            {
                if (
                    typeSymbol is INamedTypeSymbol namedClassSymbol
                    && namedClassSymbol.IsGenericType
                )
                {
                    if (
                        SymbolEqualityComparer.Default.Equals(
                            namedClassSymbol.ConstructedFrom,
                            namedExposeTypeSymbol.ConstructedFrom
                        )
                    )
                    {
                        return true;
                    }
                }
            }

            // 4. Type is generic type definition and implements interface (e.g., List<int> implements IList<>)
            if (namedExposeTypeSymbol.IsGenericType)
            {
                foreach (var iface in typeSymbol.AllInterfaces)
                {
                    if (iface is INamedTypeSymbol namedIface && namedIface.IsGenericType)
                    {
                        if (
                            SymbolEqualityComparer.Default.Equals(
                                namedIface.ConstructedFrom,
                                namedExposeTypeSymbol.ConstructedFrom
                            )
                        )
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }
    /// <summary>
    /// given a list of external method exposes, find all that apply to the current class symbol.
    /// an external method expose applies if the externalMethodExposes.containingType is assignable to the type of the class symbol.
    /// Conditions to check (example assumes class context symbol is `List<int>`):
    /// - exact type match (List<int>)
    /// - type implements exact interface match (IList<int>)
    /// - type is generic type definition (List<>)
    /// - type is generic type definition and implements interface (IList<>)
    /// </summary>
    /// <param name="classContext"></param>
    /// <param name="externalMethodMetas"></param>
    /// <returns></returns>
    private bool CurrentSymOrBaseOfCurrentMatchesCompareSymbol(
        ITypeSymbol compareSymbol
    )
    {
        var currentSymbol = typeSymbol;
        while (currentSymbol is not null)
        {
            if (SymbolEqualityComparer.Default.Equals(currentSymbol, compareSymbol))
            {
                return true;
            }
            currentSymbol = currentSymbol.BaseType;
        }
        return false;
    }
    }
}