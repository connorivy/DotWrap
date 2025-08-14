using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTUnit;

namespace DotWrap.Tests;

public static class SnapshotVerifier
{
    public static Task Verify(string source, Func<GeneratorDriverRunResult, string> selector)
    {
        var attrSource =
            @"
using System;

namespace DotWrap;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class DotWrapExposeAttribute(string? alias = null) : Attribute
{
    internal string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class DotWrapMetaAttribute(string? alias = null) : Attribute
{
    public string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalExposeAttribute(Type typeToWrap, string? alias = null) : Attribute
{
    public Type typeToWrap { get; } = typeToWrap;
    public string? alias { get; } = alias;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalMethodMeta(
    Type containingType,
    string methodName,
    Type[]? parameters = null,
    string? alias = null,
    bool ignore = false
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;
    public string methodName { get; } = methodName;
    public Type[]? parameters { get; } = parameters;
    public bool ignore { get; } = ignore;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalPropertyMeta(
    Type containingType,
    string propertyName,
    PropertyType propertyType = PropertyType.GetAndSet,
    string? alias = null
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;
    public string propertyName { get; } = propertyName;
    public PropertyType propertyType { get; } = propertyType;
}

[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class DotWrapExternalIndexerMeta(
    Type containingType,
    PropertyType propertyType = PropertyType.GetAndSet,
    string? alias = null
) : DotWrapMetaAttribute(alias)
{
    public Type containingType { get; } = containingType;

    public PropertyType propertyType { get; } = propertyType;
}

[Flags]
public enum PropertyType
{
    None = 0,
    Get = 1 << 0,
    Set = 1 << 1,
    GetAndSet = Get | Set,
}

";
        // Create references for assemblies we require
        // We could add multiple references if required
        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(
                typeof(DotWrap.DotWrapExposeAttribute).Assembly.Location
            ),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enum).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        ];

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create(
            "DotWrap.Generator",
            [CSharpSyntaxTree.ParseText(attrSource), syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new Generator.UnmanagedCallersOnlyGenerator().AsSourceGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);

        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics
        );
        var result = driver.GetRunResult();

        return Verifier.Verify(selector(result));
    }
}
