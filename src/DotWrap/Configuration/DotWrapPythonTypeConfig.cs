using System;
using DotWrap.Utils;

namespace DotWrap.Configuration;

public abstract class DotWrapPythonTypeConfig
{
    public abstract Type TypeToConfigure { get; }

    public virtual bool ShouldConfigure(ExportedTypeDefinition exportedType, Type matchingType) =>
        true;

    public virtual void ConfigureImports(IndentedStringBuilder sb) { }

    public virtual void ConfigureClassBody(
        ExportedTypeDefinition exportedType,
        Type matchingType,
        IndentedStringBuilder classBody
    ) { }

    public virtual void ConfigureGenericClassBody(
        ExportedTypeDefinition exportedType,
        Type matchingType,
        IndentedStringBuilder genericClassBodyBuilder
    ) { }
}
