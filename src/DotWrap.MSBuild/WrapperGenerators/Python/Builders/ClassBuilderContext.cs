using System.Reflection;
using DotWrap.Configuration;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record ClassBuilderContext(
    PythonContext PythonContext,
    PythonProjectInfo ProjectInfo,
    ExportedTypeDefinition ClassInfo
)
{
    public bool IsGeneric => this.ClassInfo.FullyQualifiedName.Contains('<');
};
