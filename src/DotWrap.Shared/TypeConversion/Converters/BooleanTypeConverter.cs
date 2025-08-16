using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for boolean types
/// </summary>
public class BooleanTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return fullyQualifiedTypeName.Equals("bool", StringComparison.OrdinalIgnoreCase) ||
               fullyQualifiedTypeName.Equals("System.Boolean", StringComparison.OrdinalIgnoreCase);
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
        var expression = $"{context.VariableName}{Typed} = int({context.VariableName})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        var expression = $"{ExportedPyResult} = bool({InternalPyResult})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        var expression = $"var {context.VariableName}Typed = {context.VariableName} != 0;";
        return new ConversionResult(expression);
    }
}