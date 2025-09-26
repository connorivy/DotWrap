using DotWrap.Utils.Python;

namespace DotWrap.Tests.StringManipulation.Python;

public class PythonizeClassNameUtilTests
{
    [Test]
    [Arguments("System.Collections.Generic.List<System.Int32>", "ListOfint")]
    [Arguments("DotWrap.TestLib.ResultType\u003CDotWrap.TestLib.DeeplyNestedGeneric\u003CDotWrap.TestLib.DependencyLib.MyGenericDependencyClass\u003Cstring, System.Collections.Generic.List\u003Cint\u003E\u003E\u003E\u003E", "ResultTypeOfDeeplyNestedGenericOfMyGenericDependencyClassOfstringAndListOfint")]
    [Arguments("int?", "NullableOfint")]
    public void PythonizeClassName_HandlesDeeplyNestedGenerics(string fullTypeName, string expected)
    {
        var actual = PythonNamingUtils.PythonizeClassName(fullTypeName);
        if (actual != expected)
        {
            throw new InvalidOperationException($"Actual value {actual} did not match expected {expected}");
        }
    }
}