using System;
using DotWrap.Configuration;
using DotWrap.Utils.Python;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for enum types
/// </summary>
public class EnumTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return flags.HasFlag(TypeSpecialCaseFlags.Enum);
    }

    public ConversionResult Convert(ConversionContext context)
    {
        return context.Direction switch
        {
            ConversionDirection.PythonToC => ConvertPythonToC(context),
            ConversionDirection.CToPython => ConvertCToPython(context),
            ConversionDirection.ExposedToInternal => ConvertExposedToInternal(context),
            _ => throw new ArgumentOutOfRangeException(nameof(context.Direction))
        };
    }

    private static ConversionResult ConvertPythonToC(ConversionContext context)
    {
        var expression = $"{context.VariableName}{Typed} = {PythonNamingUtils.MapTypeToPython(context.TypeDefinition.ExposedTypeIfDifferent ?? context.TypeDefinition.FullyQualifiedName)}({context.VariableName}.value)";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        var expression = $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(context.TypeDefinition.TypeNameNoGenerics)}({InternalPyResult})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        var expression = $"var {context.VariableName}Typed = ({context.TypeDefinition.FullyQualifiedName}){context.VariableName};";
        return new ConversionResult(expression);
    }
}