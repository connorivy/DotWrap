using System;
using System.Collections.Generic;
using System.Reflection;
using DotWrap.Configuration;
using DotWrap.Configuration.Python;
using DotWrap.Utils;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record GlobalContext(
    Dictionary<string, ExportedTypeDefinition> TypeDefinitions,
    // HashSet<string> EnumNames,
    // Dictionary<Type, DotWrapPythonTypeConfig> Configs,
    List<OutParamInfo> OutParams,
    Assembly Assembly
);

public record OutParamInfo(string TypeName, ExportedTypeDefinition ExportedTypeDefinition);

// public record ImportFileInfo( string ModuleName,
//     IndentedPythonStringBuilder Builder,
//     HashSet<string> ImportedNames
// );

public record PythonContext(
    GlobalContext GlobalContext,
    PythonProjectInfo ProjectInfo,
    ModuleBuilder ModuleBuilder,
    Dictionary<Type, DotWrapPythonTypeConfig> Configs,
    DotWrapPythonGlobalConfig? GlobalConfig = null
);
