namespace DotWrap.Tests;

public class NullableTests
{
    [Test]
    public async Task TestNullableAndNonNullableReferenceTypes_ShouldntGenerateDuplicateWrappers()
    {
        var source = $$"""
using DotWrap;

[DotWrapExpose]
public static class NullableTypes
{
    public static int? NullableInt(int? value)
    {
        return value;
    }

    public static string? NullableString(string? value)
    {
        return value;
    }

    public static CustomClass? NullableCustomClass(CustomClass? value)
    {
        return value;
    }
}

[DotWrapExpose]
public class CustomClass { }

""";
        await SnapshotVerifier.Verify(
            source,
            static result => result.Results[0].GeneratedSources[1].SourceText.ToString()
        );
    }
}
