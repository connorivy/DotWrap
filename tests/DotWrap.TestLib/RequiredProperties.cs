using System.Diagnostics.CodeAnalysis;

namespace DotWrap.TestLib;

[DotWrapExpose]
public class RequiredProperties
{
    public RequiredProperties()
    {
    }

    [SetsRequiredMembers]
    public RequiredProperties(string name)
    {
        Name = name;
        Age = 100;
    }

    public required string Name { get; set; }
    public required int Age { get; set; }
}

[DotWrapExpose]
public class RequiredPropertiesWithPropSettingCtor
{
    public RequiredPropertiesWithPropSettingCtor()
    {
    }

    [SetsRequiredMembers]
    public RequiredPropertiesWithPropSettingCtor(string name, int age)
    {
        Name = name;
        Age = age;
    }

    public required string Name { get; set; }
    public required int Age { get; set; }
}