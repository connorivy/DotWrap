using DotWrap;

namespace DotWrap.TestLib;

[DotWrapExpose]
public struct Structs
{
    public int X;
    public int Y;
}

[DotWrapExpose]
public class ReturnAndAcceptStructs
{
    public static Span<int> GetInts(Span<int> input)
    {
        return input;
    }

    public static Structs GetStructs(Structs input)
    {
        return input;
    }
}