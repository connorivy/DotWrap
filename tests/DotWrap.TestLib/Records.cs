
using DotWrap;

namespace DotWrap.TestLib;

[DotWrapExpose]
public record Records1(int X, int Y);

[DotWrapExpose]
public record Records2
{
    public int X { get; init; }
    public int Y { get; init; }
}