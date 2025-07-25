using System.Collections.Generic;
using System.Text;
using static DotWrap.Internal.Constants;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiClassBuilder(
    PythonProjectInfo pythonProjectInfo,
    StringBuilder mainPy,
    StringBuilder initPy
)
{
    public void AddClassesToMainAndInitPy(IEnumerable<ExportedClassInfo> classes)
    {
        foreach (var cls in classes)
        {
            AddClassToMainAndInitPy(cls);
        }
    }

    public void AddClassToMainAndInitPy(ExportedClassInfo classInfo)
    {
        initPy.AppendLine($"from .main import {classInfo.ClassName}");
        mainPy.AppendLine($"class {classInfo.ClassName}:");

        if (!string.IsNullOrWhiteSpace(classInfo.SummaryComment))
        {
            mainPy.AppendLine(
                @$"    
    """"""
    {classInfo.SummaryComment}
    """""""
            );
        }

        mainPy.AppendLine($"    @classmethod");
        mainPy.AppendLine($"    def {FromPtr}(cls, ptr: int):");
        mainPy.AppendLine($"        instance = object.__new__(cls)");
        mainPy.AppendLine($"        instance.{Ptr} = ptr");
        mainPy.AppendLine($"        return instance");
        mainPy.AppendLine();

        var classContext = new ClassBuilderContext(pythonProjectInfo, classInfo);
        foreach (var method in classInfo.Methods)
        {
            var methodBuilder = new CffiApiMethodBuilder(classContext, mainPy);
            methodBuilder.AddClassToMainAndInitPy(method);
        }

        mainPy.AppendLine("    def __del__(self):");
        mainPy.AppendLine($"        {Lib}.{classInfo.EntryPrefix}{Destroy}(self.{Ptr})");
        mainPy.AppendLine();
    }
}
