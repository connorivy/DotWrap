using System;
using System.Collections.Generic;
using DotWrap.Configuration;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record GlobalContext(
    Dictionary<string, ExportedTypeDefinition> TypeDefinitions,
    HashSet<string> EnumNames,
    Dictionary<Type, DotWrapPythonTypeConfig> Configs
);
