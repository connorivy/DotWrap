using DotWrap.Utils;

namespace DotWrap.Tests;

public class AssemblyNameUtilsTests
{
    [Test]
    public async Task Simplifies_Primitive_With_Assembly_Info()
    {
        var input =
            "System.Double, System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a";
        var expected = "System.Double";
        var actual = AssemblyNameUtils.GetSimplifiedAssemblyName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Simplifies_Generic_With_Assembly_Info()
    {
        var input =
            "System.Collections.Generic.List`1[[System.Int64, System.Runtime, Version=10.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a]]";
        var expected = "System.Collections.Generic.List`1[[System.Int64]]";
        var actual = AssemblyNameUtils.GetSimplifiedAssemblyName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Simplifies_Multiple_Generics_With_Assembly_Info()
    {
        var input =
            "System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        var expected = "System.Collections.Generic.KeyValuePair`2[[System.Int32],[System.Int64]]";
        var actual = AssemblyNameUtils.GetSimplifiedAssemblyName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }

    [Test]
    public async Task Simplifies_Deeply_Nested_Generics_With_Assembly_Info()
    {
        // var input = typeof(KeyValuePair<
        //     List<List<KeyValuePair<int, long>>>,
        //     int
        // >).AssemblyQualifiedName;
        var input =
            "System.Collections.Generic.KeyValuePair`2[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e";
        var expected =
            "System.Collections.Generic.KeyValuePair`2[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32],[System.Int64]]]]]]],[System.Int32]]";
        var actual = AssemblyNameUtils.GetSimplifiedAssemblyName(input);
        await Assert.That(actual).IsEqualTo(expected);
    }
}
