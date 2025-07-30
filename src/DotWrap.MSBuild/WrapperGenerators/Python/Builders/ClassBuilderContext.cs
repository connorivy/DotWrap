namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record ClassBuilderContext(PythonProjectInfo ProjectInfo, ExportedClassInfo ClassInfo)
{
    public bool IsGeneric => this.ClassInfo.ClassName.Contains('<');
};
