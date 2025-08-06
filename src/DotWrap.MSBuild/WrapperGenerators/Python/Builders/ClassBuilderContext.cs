using System;
using System.Collections.Generic;
using System.Reflection;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record ClassBuilderContext(
    GlobalContext GlobalContext,
    PythonProjectInfo ProjectInfo,
    ExportedTypeDefinitionInfo ClassInfo
)
{
    public bool IsGeneric => this.ClassInfo.TypeName.Contains('<');
};

public record GlobalContext(
    Dictionary<string, ExportedTypeDefinitionInfo> TypeDefinitions,
    HashSet<string> EnumNames,
    Dictionary<Type, DotWrapPythonTypeConfig> Configs
);
