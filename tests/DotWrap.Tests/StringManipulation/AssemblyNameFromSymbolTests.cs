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
        "int",
        "System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(List<int>),
        "List<int>",
        "System.Collections.Generic.List`1[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(int[]),
        "int[]",
        "System.Int32[], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(Dictionary<List<List<KeyValuePair<int, string>>>, string>),
        "Dictionary<List<List<KeyValuePair<int, string>>>, string>",
        "System.Collections.Generic.Dictionary`2[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.String, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(Dictionary<int, int>.KeyCollection),
        "Dictionary<int, int>.KeyCollection",
        "System.Collections.Generic.Dictionary`2+KeyCollection[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e"
    )]
    [Arguments(
        typeof(SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestChild<int, int>),
        "DotWrap.Tests.SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestChild<int, int>",
        "DotWrap.Tests.SampleParent`2+SampleNestedChild`2+SampleDoubleNestChild`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DotWrap.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    )]
    [Arguments(
        typeof(SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestedStaticChild<
            int,
            int
        >),
        "DotWrap.Tests.SampleParent<int, int>.SampleNestedChild<int, int>.SampleDoubleNestedStaticChild<int, int>",
        "DotWrap.Tests.SampleParent`2+SampleNestedChild`2+SampleDoubleNestedStaticChild`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], DotWrap.Tests, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
    )]
    public async Task GetAssemblyNameFromSymbol_ReturnsExpectedName(
        Type type,
        string typeString,
        string expectedName
    )
    {
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
        // var assemblyQualifiedName = iTypeSymbol!.GetAssemblyQualifiedName();
        var assemblyQualifiedName =
            DotWrap.Generator.Utils.AssemblyNameUtils.GetAssemblyQualifiedName(iTypeSymbol!);

        await Assert.That(assemblyQualifiedName).IsEqualTo(expectedName);
    }
}

public class SampleParent<TKey, TValue>
{
    public class SampleNestedChild<TKey2, TValue2>
    {
        public class SampleDoubleNestChild<TKey3, TValue3> { }

        public static class SampleDoubleNestedStaticChild<TKey3, TValue3> { }
    }
}
