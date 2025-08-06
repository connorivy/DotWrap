using System;
using DotWrap.Utils;

namespace DotWrap;

public abstract class DotWrapPythonTypeConfig
{
    public abstract Type TypeToConfigure { get; }

    public virtual void ConfigureImports(IndentedStringBuilder sb) { }

    public virtual void ConfigureClassBody(Type matchingType, IndentedStringBuilder classBody) { }

    public virtual void ConfigureGenericClassBody(
        Type matchingType,
        IndentedStringBuilder genericClassBodyBuilder
    ) { }
}
