using System;
using DotWrap.Configuration;

namespace DotWrap.TypeConversion;

/// <summary>
/// Interface for type converters that handle conversion between different type representations
/// </summary>
public interface ITypeConverter
{
    /// <summary>
    /// Determines if this converter can handle the specified type
    /// </summary>
    bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags);
    
    /// <summary>
    /// Converts a type according to the specified context
    /// </summary>
    ConversionResult Convert(ConversionContext context);
}

/// <summary>
/// Main service for consolidating type conversion logic
/// </summary>
public interface ITypeConversionService
{
    /// <summary>
    /// Converts a parameter from Python to C format
    /// </summary>
    string ConvertParameterPythonToC(
        string variableName,
        ExportedTypeDefinition typeDefinition,
        bool isNullable,
        bool isOutParameter = false);
    
    /// <summary>
    /// Converts a return value from C to Python format
    /// </summary>
    string? ConvertReturnValueCToPython(
        ExportedTypeDefinition typeDefinition,
        bool isNullable);
    
    /// <summary>
    /// Converts an exposed parameter type to internal C# type (for source generation)
    /// </summary>
    string? ConvertExposedToInternal(
        string variableName,
        string fullyQualifiedTypeName,
        TypeSpecialCaseFlags flags,
        bool isNullable);
}