using DotWrap.TestLib.DependencyLib;

namespace DotWrap.TestLib;

public class ResultType<T>(T value)
{
    public T GetValue() => value;
}

public class DeeplyNestedGeneric<T>
    where T : class
{
}

[DotWrapExpose]
public class UsesDeeplyNestedGeneric
{
    public ResultType<DeeplyNestedGeneric<MyGenericDependencyClass<string, List<int>>>> MethodReturningDeep()
    {
        var dep = new DeeplyNestedGeneric<MyGenericDependencyClass<string, List<int>>>();
        return new ResultType<DeeplyNestedGeneric<MyGenericDependencyClass<string, List<int>>>>(
            dep
        );
    }
}