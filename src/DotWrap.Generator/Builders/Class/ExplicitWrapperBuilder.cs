using System.Text;
using DotWrap.Generator.Builders.Method;
using DotWrap.Generator.Extensions;
using DotWrap.Internal;
using DotWrap.MSBuild;
using Microsoft.CodeAnalysis;
using static DotWrap.Internal.Constants;

namespace DotWrap.Generator.Builders.Class;

public class ExplicitWrapperBuilder(ClassBuilderContext context)
    : EntryPointStaticClassBuilderBase(context)
{
    public override void CreateClassBody(
        StringBuilder methodsSource,
        ClassMetadataBuilder classMetadataBuilder
    )
    {
        this.AddInstanceMethods(methodsSource, classMetadataBuilder);
    }

    protected void AddInstanceMethods(StringBuilder sb, ClassMetadataBuilder classMetadataBuilder)
    {
        MethodBuilder instanceMethodBuilder = new(sb, classMetadataBuilder, Context);
        instanceMethodBuilder.GenerateAllMethods(Context);
    }
}

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
        + string.Join(", ", ClassSymbol.TypeArguments.Select(t => t.ToDisplayString()))
            .AddOnIfNotNullOrEmpty("<", ">");
    public string Namespace => ClassSymbol.ContainingNamespace.ToDisplayString();
    public string WrapperName =>
        ClassNameWithoutGenerics
        + "DotWrapWrapper"
        + string.Join("_", ClassSymbol.TypeArguments.Select(t => t.ToDisplayString()))
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
