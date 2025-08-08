using System.Reflection;
using DotWrap.Configuration;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record ClassBuilderContext(
    GlobalContext GlobalContext,
    PythonProjectInfo ProjectInfo,
    ExportedTypeDefinitionInfo ClassInfo
)
{
    public bool IsGeneric => this.ClassInfo.TypeName.Contains('<');
};
