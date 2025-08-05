namespace DotWrap.TestLib;

public enum TestEnum : long
{
    ValueZero = 0,
    ValueOne = 1,
    ValueTwoNoNumber,
    ValueFive = 5,
}

[DotWrapExpose]
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
