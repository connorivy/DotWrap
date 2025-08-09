using DotWrap.Extensions;
using DotWrap.Generator.Extensions;
using DotWrap.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotWrap.Tests;

public class ExportedTypeIdConsistencyTests
{
    [Test]
    [Arguments(typeof(int))]
    [Arguments(typeof(string))]
    [Arguments(typeof(List<int>))]
    [Arguments(typeof(Dictionary<string, int>))]
    // [Arguments(typeof(double?))]
    [Arguments(typeof(Dictionary<string, List<int>>))]
    [Arguments(typeof(KeyValuePair<string, int>))]
    // [Arguments(typeof(KeyValuePair<List<List<KeyValuePair<string, int>>>, int>))]
    public async Task ExportedTypeId_Matches_Between_Type_And_ITypeSymbol(Type type)
    {
        // Reflection-based
        var fromType = type.GetExportedTypeIdFromType();

        // Roslyn-based
        var originalTypeString = DotWrapUtils.GetOriginalTypeString(
            type.FullName ?? throw new InvalidOperationException("Type name cannot be null")
        );
        var tree = CSharpSyntaxTree.ParseText(
            $"class Dummy {{ public {originalTypeString} Field; }}"
        );
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
