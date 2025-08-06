using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyTUnit;

namespace DotWrap.Tests;

public class EnumTests
{
    [Test]
    public async Task TestClassThatUsesEnums()
    {
        var source =
            @"
using DotWrap;

namespace DotWrap.Tests;


public enum TestEnum : long
{
    ValueZero = 0,
    ValueOne = 1,
    ValueTwoNoNumber,
    ValueFive = 5,
}

[DotWrapExposeAttribute(""DotWrap.Tests.ClassWithEnums"")]
public class ClassWithEnums
{
    public TestEnum EnumProperty { get; set; }

    public TestEnum GetEnum(TestEnum input)
    {
        return input;
    }

    public TestEnum GetValueFive()
    {
        return TestEnum.ValueFive;
    }
}
";

        await SnapshotVerifier.Verify(source);
    }
}

public static class SnapshotVerifier
{
    public static Task Verify(string source)
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

        return Verifier.Verify(result.Results[0].GeneratedSources[1].SourceText.ToString());
    }
}
