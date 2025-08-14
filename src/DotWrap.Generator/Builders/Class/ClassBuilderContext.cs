using System.Text.Json.Serialization;
using DotWrap.Generator.Extensions;
using DotWrap.Utils;
using Microsoft.CodeAnalysis;

namespace DotWrap.Generator.Builders.Class;

public class ClassBuilderContext(
    GlobalContext globalContext,
    ITypeSymbol classSymbol,
    DotWrapExposeAttribute typeMetadata
)
{
    public GlobalContext GlobalContext => globalContext;
    public ITypeSymbol ClassSymbol => classSymbol;
    public string? Alias => typeMetadata.alias;
    public string ClassNameWithoutGenerics
    {
        get
        {
            if (Alias is not null)
            {
                return Alias;
            }
            if (ClassSymbol is IArrayTypeSymbol arraySymbol)
            {
                return DotWrapUtils.ReplaceArraySymbols(arraySymbol.ToDisplayString());
            }
            return ClassSymbol.Name;
        }
    }
    public string ClassName =>
        ClassNameWithoutGenerics
        + string.Join(", ", TypeArguments.Select(t => t.ToDisplayString()))
            .AddOnIfNotNullOrEmpty("<", ">");

    public string Namespace => typeMetadata.namespaceAlias ?? OriginalNamespace;

    private string OriginalNamespace =>
        ClassSymbol.ContainingNamespace?.ToDisplayString() ?? "global";

    [JsonIgnore]
    public string WrapperNamespace => OriginalNamespace;

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

    public IReadOnlyList<ITypeParameterSymbol> TypeParameters =>
        field ??= (
            (this.ClassSymbol as INamedTypeSymbol)
                ?.TypeParameters.Concat(ContainingTypes.SelectMany(c => c.TypeParameters))
                .ToList() ?? []
        );

    public IReadOnlyList<ITypeSymbol> TypeArguments =>
        field ??= (
            (this.ClassSymbol as INamedTypeSymbol)
                ?.TypeArguments.Concat(ContainingTypes.SelectMany(c => c.TypeArguments))
                .ToList() ?? []
        );

    public IReadOnlyDictionary<ITypeParameterSymbol, ITypeSymbol> TypeParametersToArguments =>
        field ??= (
            TypeParameters
                .Zip(TypeArguments, (param, arg) => (param, arg))
                .ToDictionary(pair => pair.param, pair => pair.arg)
        );
    public string WrapperName =>
        ClassNameWithoutGenerics
        + "DotWrapWrapper"
        + string.Join("_", TypeArguments.Select(t => t.ToDisplayString().Replace("?", "")))
            .AddOnIfNotNullOrEmpty("_");
    public bool IsStatic => ClassSymbol.IsStatic;
    public string FullyQualifiedWrapperName => $"{WrapperNamespace}.{WrapperName}";
    public string FullyQualifiedClassName => ClassSymbol.ToDisplayString();
    public string EntryPrefix => field ??= $"c{DotWrapUtils.GetStamp(FullyQualifiedClassName)}_";
}
