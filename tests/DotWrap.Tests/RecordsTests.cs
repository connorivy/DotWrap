namespace DotWrap.Tests;

public class RecordsTests
{
    [Test]
    public async Task TestNullableAndNonNullableReferenceTypes_ShouldntGenerateDuplicateWrappers()
    {
        var source = $$"""
using DotWrap;
using DotWrap.Configuration;

namespace DotWrap.Generator
{
    [DotWrapExpose]
    public record Records1(int X, int Y);
}

""";
        await SnapshotVerifier.Verify(
            source,
            static result => result.Results[0].GeneratedSources[1].SourceText.ToString()
        );
    }
}
