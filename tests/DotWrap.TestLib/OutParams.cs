namespace DotWrap.TestLib;

[DotWrapExpose]
public class OutParams
{
    public static void OutInt42(out int result)
    {
        result = 42;
    }

    public static void OutStringHelloWorld(out string result)
    {
        result = "HelloWorld";
    }
}
