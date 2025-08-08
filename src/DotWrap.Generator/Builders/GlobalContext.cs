using DotWrap.Configuration;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders;

public class GlobalContext(
    HashSet<ITypeSymbol> allExplicitTypes,
    HashSet<ITypeSymbol> allInferedTypes,
    List<ITypeSymbol> inferedTypesToWrap
)
{
    // public List<ExportedEnumInfo> ExportedEnums { get; } = [];

    public void AddInferedType(ITypeSymbol typeSymbol)
    {
        if (!allInferedTypes.Add(typeSymbol) || allExplicitTypes.Contains(typeSymbol))
        {
            return;
        }

        if (
            typeSymbol.TypeKind
            is TypeKind.Class
                or TypeKind.Struct
                or TypeKind.Interface
                or TypeKind.Array
                or TypeKind.Enum
        )
        {
            inferedTypesToWrap.Add(typeSymbol);
        }
        else
        {
            // log skipping type
        }
    }
}
