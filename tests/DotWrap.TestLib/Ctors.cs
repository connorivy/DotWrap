namespace DotWrap.TestLib;

[DotWrapExpose]
public class NoCtor : CtorBase
{
    public required int Value2 { get; set; } = 42;
}

public class CtorBase
{
    public CtorBase(int value)
    {
        this.Value = value;
    }

    public CtorBase()
    {
    }

    public required int Value { get; set; } = 42;
}