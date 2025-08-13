using DotWrap.Configuration;
using DotWrap.Utils;
using DotWrap.Utils.Python;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

internal class CffiApiEnumBuilder(PythonContext pythonProjectInfo, IndentedStringBuilder mainPy)
{
    public void AddClassesToMainAndInitPy(IEnumerable<ExportedEnumInfo> enums)
    {
        foreach (var enumInfo in enums)
        {
            AddClassToMainAndInitPy(enumInfo);
        }
    }

    public void AddClassToMainAndInitPy(ExportedEnumInfo cls)
    {
        string className = PythonNamingUtils.PythonizeClassName(cls.TypeNameNoGenerics);
        var initFileBuilder = pythonProjectInfo.ModuleBuilder.GetImportFile(cls.Namespace);
        initFileBuilder.AddTypeImport(className);

        mainPy.AppendLine($"class {className}(Enum):");
        using var indent = mainPy.IndentUntilDispose();

        foreach (var kvp in cls.Options)
        {
            mainPy.AppendLine($"{PythonNamingUtils.ToSnakeCase(kvp.Key)} = {kvp.Value}");
        }
    }
}
