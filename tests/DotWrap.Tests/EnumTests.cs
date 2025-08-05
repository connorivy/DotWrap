using System;
using System.Collections.Generic;
using System.Diagnostics;
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

public enum TestEnum : long
{
    ValueZero = 0,
    ValueOne = 1,
    ValueTwoNoNumber,
    ValueFive = 5,
}

[DotWrap.DotWrapExpose]
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
        var syntaxTree = CSharpSyntaxTree.ParseText(source);

        // Create references for assemblies we require
        // We could add multiple references if required
        PortableExecutableReference[] references =
        [
            MetadataReference.CreateFromFile(
                typeof(DotWrap.DotWrapExposeAttribute).Assembly.Location
            ),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        ];

        var compilation = CSharpCompilation.Create(
            "DotWrap.Generator",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        var generator = new Generator.UnmanagedCallersOnlyGenerator().AsSourceGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);

        driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var diagnostics
        );

        return Verifier.Verify(outputCompilation);
    }
}
