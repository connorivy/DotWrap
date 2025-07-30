using System.Text;
using DotWrap.Generator.Builders.Method;
using DotWrap.Generator.Extensions;
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
        MethodBuilder instanceMethodBuilder = new(sb, classMetadataBuilder);
        instanceMethodBuilder.GenerateAllMethods(Context);
    }
}

public record ClassBuilderContext(INamedTypeSymbol ClassSymbol, string? Alias = null)
{
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
    public string EntryPrefix =>
        $"{Namespace.Replace(".", "_")}_{ClassName.Replace('<', '_').Replace('>', '_')}_";

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
