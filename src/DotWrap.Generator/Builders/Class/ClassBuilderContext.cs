using DotWrap.Generator.Extensions;
using DotWrap.Internal;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ClassBuilderContext(
    GlobalContext globalContext,
    INamedTypeSymbol classSymbol,
    string? alias = null
)
{
    public GlobalContext GlobalContext => globalContext;
    public INamedTypeSymbol ClassSymbol => classSymbol;
    public string? Alias => alias;
    public string ClassNameWithoutGenerics => Alias ?? ClassSymbol.Name;
    public string ClassName =>
        ClassNameWithoutGenerics
        + string.Join(
                ", ",
                ClassSymbol
                    .TypeArguments.Concat(ContainingTypes.SelectMany(c => c.TypeArguments))
                    .Select(t => t.ToDisplayString())
            )
            .AddOnIfNotNullOrEmpty("<", ">");

    public string Namespace => ClassSymbol.ContainingNamespace.ToDisplayString();

    public List<INamedTypeSymbol> ContainingTypes
    {
        get
        {
            if (field is null)
            {
                var currentSymbol = ClassSymbol.ContainingSymbol;
                List<INamedTypeSymbol> containingSymbols = [];
                while (currentSymbol is INamedTypeSymbol containingSymbol)
                {
                    containingSymbols.Add(containingSymbol);
                    currentSymbol = containingSymbol.ContainingSymbol;
                }
                field = containingSymbols;
            }
            return field;
        }
    }

    public IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> TypeParametersToArguments =>
        field ??= (
            ClassSymbol
                .TypeParameters.Concat(ContainingTypes.SelectMany(c => c.TypeParameters))
                .Zip(
                    ClassSymbol.TypeArguments.Concat(
                        ContainingTypes.SelectMany(c => c.TypeArguments)
                    ),
                    (param, arg) => (param, arg)
                )
                .ToDictionary(pair => pair.param, pair => pair.arg)
        );
    public string WrapperName =>
        ClassNameWithoutGenerics
        + "DotWrapWrapper"
        + string.Join(
                "_",
                ClassSymbol
                    .TypeArguments.Concat(ContainingTypes.SelectMany(c => c.TypeArguments))
                    .Select(t => t.ToDisplayString())
            )
            .AddOnIfNotNullOrEmpty("_");
    public bool IsStatic => ClassSymbol.IsStatic;
    public string FullyQualifiedWrapperName => $"{Namespace}.{WrapperName}";
    public string FullyQualifiedClassName => ClassSymbol.ToDisplayString();
    public string EntryPrefix => field ??= $"c{DotWrapUtils.GetStamp(FullyQualifiedClassName)}_";

    public ClassSpecialCaseFlags SpecialCaseFlags
    {
        get
        {
            ClassSpecialCaseFlags flags = ClassSpecialCaseFlags.None;

            if (ClassSymbol.AllInterfaces.Any(i => i.Name == "IEnumerable"))
            {
                flags |= ClassSpecialCaseFlags.IEnumerable;
            }
            if (ClassSymbol.AllInterfaces.Any(i => i.Name.StartsWith("IReadOnlyCollection")))
            {
                flags |= ClassSpecialCaseFlags.ICollection;
                if (ClassSymbol.AllInterfaces.Any(i => i.Name == "ICollection"))
                {
                    flags |= ClassSpecialCaseFlags.IsReadOnly;
                }
            }
            if (ClassSymbol.AllInterfaces.Any(i => i.Name.StartsWith("IReadOnlyList")))
            {
                flags |= ClassSpecialCaseFlags.IList;
                if (!ClassSymbol.AllInterfaces.Any(i => i.Name == "IList"))
                {
                    flags |= ClassSpecialCaseFlags.IsReadOnly;
                }
            }
            return flags;
        }
    }
}
