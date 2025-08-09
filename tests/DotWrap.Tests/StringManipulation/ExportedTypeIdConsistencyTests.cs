using DotWrap.Extensions;
using DotWrap.Generator.Extensions;
using DotWrap.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotWrap.Tests;

public class ExportedTypeIdConsistencyTests
{
    [Test]
    [Arguments(typeof(int), "int")]
    [Arguments(typeof(string), "string")]
    [Arguments(typeof(List<int>), "List<int>")]
    [Arguments(typeof(Dictionary<string, int>), "Dictionary<string, int>")]
    // [Arguments(typeof(double?), "double?")]
    [Arguments(typeof(Dictionary<string, List<int>>), "Dictionary<string, List<int>>")]
    [Arguments(typeof(KeyValuePair<string, int>), "KeyValuePair<string, int>")]
    [Arguments(
        typeof(KeyValuePair<List<List<KeyValuePair<string, int>>>, int>),
        "KeyValuePair<List<List<KeyValuePair<string, int>>>, int>"
    )]
    [Arguments(typeof(Dictionary<int, int>.KeyCollection), "Dictionary<int, int>.KeyCollection")]
    [Arguments(typeof(Dictionary<int, int>.KeyCollection), "Dictionary<int, int>.KeyCollection")]
    [Arguments(
        typeof(SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestChild<int, int>),
        "DotWrap.Tests.SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestChild<int, int>"
    )]
    [Arguments(
        typeof(SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestedStaticChild<
            int,
            int
        >),
        "DotWrap.Tests.SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestedStaticChild<int, int>"
    )]
    public async Task ExportedTypeId_Matches_Between_Type_And_ITypeSymbol(
        Type type,
        string typeString
    )
    {
        // Reflection-based
        var fromType = type.GetExportedTypeIdFromType();

        // Roslyn-based
        var code =
            $@"
using System;
using System.Collections.Generic;

class Dummy 
{{ 
    public {typeString} Field; 
}}";
        var tree = CSharpSyntaxTree.ParseText(code);
        var compilation = CSharpCompilation.Create(
            "DummyAssembly",
            new[] { tree },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(type.Assembly.Location),
            }
        );
        var model = compilation.GetSemanticModel(tree);
        var field = tree.GetRoot()
            .DescendantNodes()
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.FieldDeclarationSyntax>()
            .First();
        var symbol = model.GetDeclaredSymbol(field.Declaration.Variables.First()) as IFieldSymbol;
        var iTypeSymbol = symbol?.Type;
        await Assert.That(iTypeSymbol).IsNotNull();
        var fromSymbol = iTypeSymbol!.GetExportedTypeId();

        await Assert.That(fromType.Id).IsEqualTo(fromSymbol.Id);
    }
}
