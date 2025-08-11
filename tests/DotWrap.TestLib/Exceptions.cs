namespace DotWrap.TestLib;

[DotWrapExpose]
public class Exceptions
{
    public void ThrowInvalidOperationException(int paramName)
    {
        throw new InvalidOperationException("This is an invalid operation exception.");
    }
}
