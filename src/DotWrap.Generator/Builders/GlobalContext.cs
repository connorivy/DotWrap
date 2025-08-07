using DotWrap.Configuration;
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
    public List<ExportedEnumInfo> ExportedEnums { get; } = [];

    public void AddInferedType(ITypeSymbol typeSymbol)
    {
        if (allInferedTypes.Contains(typeSymbol) || !allInferedTypes.Add(typeSymbol))
        {
            return;
        }

        if (typeSymbol.TypeKind is TypeKind.Enum)
        {
            var namedTypeSymbol =
                typeSymbol as INamedTypeSymbol
                ?? throw new ArgumentException("Expected INamedTypeSymbol for enum type.");
            var underlyingType =
                namedTypeSymbol.EnumUnderlyingType
                ?? throw new ArgumentException("Enum type must have an underlying type.");
            ExportedEnums.Add(
                new()
                {
                    Name = namedTypeSymbol.Name,
                    OriginalTypeName = "Enum",
                    ExposedTypeIfDifferent = underlyingType.ToDisplayString(),
                    Namespace = namedTypeSymbol.ContainingNamespace.ToDisplayString(),
                    Options = namedTypeSymbol
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .ToDictionary(
                            f => f.Name,
                            f =>
                                long.TryParse(f.ConstantValue?.ToString(), out var result)
                                    ? result
                                    : throw new ArgumentException(
                                        $"Could not parse constant value for enum field '{f.Name}' in enum '{namedTypeSymbol.Name}'."
                                    )
                        ),
                }
            );
        }
        else if (
            typeSymbol.TypeKind
            is TypeKind.Class
                or TypeKind.Struct
                or TypeKind.Interface
                or TypeKind.Array
        )
        {
            if (!SkipWrapperGeneration(typeSymbol) && !allExplicitTypes.Contains(typeSymbol))
            {
                inferedTypesToWrap.Add(typeSymbol);
            }
        }
        else
        {
            // log skipping type
        }
    }

    private bool SkipWrapperGeneration(ITypeSymbol classSymbol)
    {
        if (classSymbol is INamedTypeSymbol namedTypeSymbol && namedTypeSymbol.IsBlittable())
        {
            return true;
        }
        if (MethodBuilder.GetBlittableExternalTypeAssignment(classSymbol, this) is not null)
        {
            return true;
        }
        return false;
    }
}
