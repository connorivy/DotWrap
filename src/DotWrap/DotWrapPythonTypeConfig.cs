using System;
using DotWrap.MSBuild;
using DotWrap.Utils;

namespace DotWrap;

public abstract class DotWrapPythonTypeConfig
{
    public abstract Type TypeToConfigure { get; }

    public virtual void ConfigureImports(IndentedStringBuilder sb) { }

    public virtual void ConfigureClassBody(
        ExportedTypeDefinitionInfo exportedType,
        Type matchingType,
        IndentedStringBuilder classBody
    ) { }

    public virtual void ConfigureGenericClassBody(
        ExportedTypeDefinitionInfo exportedType,
        Type matchingType,
        IndentedStringBuilder genericClassBodyBuilder
    ) { }
}
