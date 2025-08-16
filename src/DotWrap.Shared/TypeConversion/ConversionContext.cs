using System;
using DotWrap.Configuration;

namespace DotWrap.TypeConversion;

/// <summary>
/// Defines the context in which a type conversion is being performed.
/// This determines the appropriate conversion strategy and format.
/// </summary>
public enum ConversionDirection
{
    /// <summary>
    /// Converting from Python type to C type (for parameters)
    /// </summary>
    PythonToC,
    
    /// <summary>
    /// Converting from C type back to Python type (for return values)
    /// </summary>
    CToPython,
    
    /// <summary>
    /// Converting from exposed type to internal C# type (for source generation)
    /// </summary>
    ExposedToInternal
}

/// <summary>
/// Additional context information for type conversion
/// </summary>
public readonly record struct ConversionContext(
    ConversionDirection Direction,
    bool IsNullable,
    bool IsOutParameter,
    ExportedTypeDefinition TypeDefinition,
    string VariableName
);

/// <summary>
/// Result of a type conversion operation
/// </summary>
public readonly record struct ConversionResult(
    string ConversionExpression,
    bool RequiresNullCheck = false,
    string[]? RequiredImports = null
);