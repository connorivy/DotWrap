using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for string types
/// </summary>
public class StringTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return fullyQualifiedTypeName.Equals("string", StringComparison.OrdinalIgnoreCase) ||
               fullyQualifiedTypeName.Equals("System.String", StringComparison.OrdinalIgnoreCase);
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
        var expression = $"{context.VariableName}{Typed} = {Ffi}.new(\"char[]\", {context.VariableName}.encode(\"utf-8\"))";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        var expression = $"{ExportedPyResult} = str(CString({InternalPyResult}))";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        var expression = $"var {context.VariableName}Typed = System.Runtime.InteropServices.Marshal.PtrToStringAnsi({context.VariableName});";
        return new ConversionResult(expression);
    }
}