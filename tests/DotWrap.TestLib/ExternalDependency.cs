using DotWrap.TestLib.DependencyLib;
namespace DotWrap.TestLib;

[DotWrapExpose]
public class HasExternalDependency : MyDependencyClass
{
    public int Return5FromParentClass() => 5;

    public MyDependencyClass[] DependencyClassAsArray() => [];
}