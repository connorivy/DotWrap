using System;
using DotWrap.Configuration;
using DotWrap.Utils.Python;
using static DotWrap.Utils.Python.PythonConstants;
using static DotWrap.Internal.Constants;

namespace DotWrap.TypeConversion.Converters;

/// <summary>
/// Handles conversion for complex object types (wrapped objects)
/// </summary>
public class ComplexObjectTypeConverter : ITypeConverter
{
    public bool CanHandle(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        // This converter handles any type that isn't a primitive type
        // It's the fallback for complex objects that need wrapping
        return !IsHandledByOtherConverters(fullyQualifiedTypeName, flags);
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
        if (context.IsOutParameter)
        {
            var expression = $"{context.VariableName}{Typed} = {context.VariableName}.{OutVal}";
            return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
        }
        else
        {
            var expression = $"{context.VariableName}{Typed} = {context.VariableName}.{Ptr}";
            return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
        }
    }

    private static ConversionResult ConvertCToPython(ConversionContext context)
    {
        if (context.TypeDefinition.IsSameAsExposedType)
        {
            // No conversion needed for same type
            return new ConversionResult("", RequiresNullCheck: false);
        }

        var expression = $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(context.TypeDefinition.SimplifiedAssemblyQualifiedName)}.{FromPtr}({InternalPyResult})";
        return new ConversionResult(expression, RequiresNullCheck: context.IsNullable);
    }

    private static ConversionResult ConvertExposedToInternal(ConversionContext context)
    {
        // For complex objects, we need to unwrap from the wrapper class
        var getMethod = context.IsNullable ? GetOrDefault : Get;
        var wrapperName = PythonNamingUtils.PythonizeClassName(context.TypeDefinition.FullyQualifiedName);
        var expression = $"var {context.VariableName}{Typed} = {wrapperName}.{getMethod}({context.VariableName});";
        return new ConversionResult(expression);
    }

    private static bool IsHandledByOtherConverters(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        // Check if this type would be handled by primitive type converters
        var lowerTypeName = fullyQualifiedTypeName.ToLowerInvariant();
        
        return lowerTypeName.Contains("string") ||
               lowerTypeName.Contains("bool") ||
               lowerTypeName.Contains("char") ||
               lowerTypeName.Contains("system.half") ||
               flags.HasFlag(TypeSpecialCaseFlags.Enum);
    }
}