using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

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

        await SnapshotVerifier.Verify(
            source,
            static result => result.Results[0].GeneratedSources[1].SourceText.ToString()
        );
    }
}
