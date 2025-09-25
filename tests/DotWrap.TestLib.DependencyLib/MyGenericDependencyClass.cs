namespace DotWrap.TestLib.DependencyLib;

public class MyGenericDependencyClass<T, U>(T value, U anotherValue)
{
    public string ReturnHelloWorld() => "HelloWorld";
    public T GetValue() => value;
    public U GetAnotherValue() => anotherValue;
}
