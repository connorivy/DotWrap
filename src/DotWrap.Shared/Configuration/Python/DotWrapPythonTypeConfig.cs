using System;
using DotWrap.Utils;

namespace DotWrap.Configuration.Python;

public abstract class DotWrapPythonTypeConfig
{
    public abstract Type TypeToConfigure { get; }

    public virtual bool ShouldConfigure(PythonTypeConfigContext context) => true;

    public virtual void ConfigureImports(IndentedStringBuilder sb) { }

    public virtual void ConfigureClassBody(PythonTypeConfigContext context) { }

    public virtual void ConfigureGenericClassBody(PythonTypeConfigContext context) { }
}

public readonly record struct PythonTypeConfigContext(
    Dictionary<string, ExportedTypeDefinition> TypeDefinitions,
    ExportedTypeDefinition ExportedType,
    Type MatchingType,
    IndentedStringBuilder ClassBody
);
