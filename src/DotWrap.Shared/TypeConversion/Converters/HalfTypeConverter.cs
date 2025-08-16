using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for System.Half types
/// </summary>
public class HalfTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return fullyQualifiedTypeName.Equals("System.Half", StringComparison.OrdinalIgnoreCase);
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
        // Half is already represented by a float and does not need conversion
        var expression = $"{context.VariableName}{Typed} = {context.VariableName}";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        // Half values are converted to float in Python
        var expression = $"{ExportedPyResult} = float({InternalPyResult})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        var expression = $"var {context.VariableName}Typed = (Half){context.VariableName};";
        return new ConversionResult(expression);
    }
}