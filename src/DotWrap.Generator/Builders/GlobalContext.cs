using DotWrap.Generator.Builders.Method;
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
        if (classSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsBlittable())
        {
            return true;
        }
        if (MethodBuilder.GetBlittableExternalTypeAssignment(classSymbol) is not null)
        {
            return true;
        }
        return false;
    }
}
