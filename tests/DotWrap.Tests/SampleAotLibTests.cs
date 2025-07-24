using Xunit;

namespace DotWrap.Tests;

public class SampleAotLibTests
{
    [Fact]
    public void Add_ReturnsSum()
    {
        // Direct call for now; in real use, would test via interop
        Assert.Equal(5, DotWrap.SampleAotLib.SampleClass.Add(2, 3));
    }
}
