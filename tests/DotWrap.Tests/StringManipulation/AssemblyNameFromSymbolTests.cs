using DotWrap.Extensions;
using DotWrap.Generator.Extensions;
using DotWrap.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace DotWrap.Tests;

public class AssemblyNameFromSymbolTests
{
    [Test]
    [Arguments(
        typeof(int),
        "System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(List<int>),
        "System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(int[]),
        "System.Int32[], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(Dictionary<List<List<KeyValuePair<int, string>>>, string>),
        "System.Collections.Generic.Dictionary`2[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    public async Task GetAssemblyNameFromSymbol_ReturnsExpectedName(Type type, string expectedName)
    {
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
        // var assemblyQualifiedName = iTypeSymbol!.GetAssemblyQualifiedName();
        var assemblyQualifiedName =
            DotWrap.Generator.Utils.AssemblyNameUtils.GetAssemblyQualifiedName(iTypeSymbol!);

        await Assert.That(assemblyQualifiedName).IsEqualTo(expectedName);
    }
}
