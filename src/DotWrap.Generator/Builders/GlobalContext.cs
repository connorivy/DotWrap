using DotWrap.Generator.Extensions;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders;

public class GlobalContext(
    HashSet<ITypeSymbol> allExplicitTypes,
    HashSet<ITypeSymbol> allInferedTypes,
    List<ITypeSymbol> inferedTypesToWrap
)
{
    public void AddInferedType(ITypeSymbol typeSymbol)
    {
        if (
            !SkipWrapperGeneration(typeSymbol)
            && !allExplicitTypes.Contains(typeSymbol)
            && allInferedTypes.Add(typeSymbol)
        )
        {
            inferedTypesToWrap.Add(typeSymbol);
        }
    }

    private static bool SkipWrapperGeneration(ITypeSymbol classSymbol)
    {
        return classSymbol switch
        {
            INamedTypeSymbol namedTypeSymbol when namedTypeSymbol.IsBlittable() => true,
            { SpecialType: SpecialType.System_String } => true,
            _ => false,
        };
    }
}
