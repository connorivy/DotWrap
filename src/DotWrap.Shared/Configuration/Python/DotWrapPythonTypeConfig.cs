using System;
using DotWrap.Utils;

namespace DotWrap.Configuration.Python;

public abstract class DotWrapPythonTypeConfig
{
    /// <summary>
    /// The csharp type that is being translated to python code. This could be the type itself or
    /// an interface that the type implements or a base class that the type derives from.
    /// </summary>
    public abstract Type TypeToConfigure { get; }

    /// <summary>
    /// Determines whether this configuration should be applied to the given context.
    /// </summary>
    /// <param name="context"></param>
    /// <returns></returns>
    public virtual bool ShouldConfigure(PythonTypeConfigContext context) => true;

    /// <summary>
    /// Adds the necessary imports to the file.
    /// </summary>
    /// <param name="sb"></param>
    public virtual void ConfigureImports(IndentedStringBuilder sb) { }

    /// <summary>
    /// Enables the user to add custom code to the class body.
    /// </summary>
    /// <param name="context"></param>
    public virtual void ConfigureClassBody(PythonTypeConfigContext context) { }

    /// <summary>
    /// Enables the user to add custom code the generic definition of the class being configured
    /// (if applicable)
    /// </summary>
    /// <param name="context"></param>
    public virtual void ConfigureGenericClassBody(PythonTypeConfigContext context) { }
}

public readonly record struct PythonTypeConfigContext(
    Dictionary<string, ExportedTypeDefinition> TypeDefinitions,
    ExportedTypeDefinition ExportedType,
    Type MatchingType,
    IndentedStringBuilder ClassBody
);
