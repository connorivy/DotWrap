using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for character types
/// </summary>
public class CharacterTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return fullyQualifiedTypeName.Equals("char", StringComparison.OrdinalIgnoreCase) ||
               fullyQualifiedTypeName.Equals("System.Char", StringComparison.OrdinalIgnoreCase);
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
        var expression = $"{context.VariableName}{Typed} = ord({context.VariableName})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        var expression = $"{ExportedPyResult} = chr({InternalPyResult})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        var expression = $"var {context.VariableName}Typed = (char){context.VariableName};";
        return new ConversionResult(expression);
    }
}