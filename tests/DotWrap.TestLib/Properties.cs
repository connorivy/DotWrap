namespace DotWrap.TestLib;

[DotWrapExpose]
public class Properties
{
    public static string StaticHelloWorld => "HelloWorld";
    public string InstanceHelloWorld => "HelloWorld";
    public static int GetAndSetProperty { get; set; } = 42;

    public static int get_RandomMethodStartingWithGet() => 100;
}
