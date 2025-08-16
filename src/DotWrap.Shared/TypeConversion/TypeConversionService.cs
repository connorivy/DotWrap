using System;
using System.Collections.Generic;
using System.Linq;
using DotWrap.Configuration;
using DotWrap.TypeConversion.Converters;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.TypeConversion;

/// <summary>
/// Main service for consolidating type conversion logic across the DotWrap system
/// </summary>
public class TypeConversionService : ITypeConversionService
{
    private readonly IReadOnlyList<ITypeConverter> _converters;

    public TypeConversionService()
    {
        _converters = new List<ITypeConverter>
        {
            new StringTypeConverter(),
            new BooleanTypeConverter(),
            new CharacterTypeConverter(),
            new EnumTypeConverter(),
            new HalfTypeConverter(),
            new ComplexObjectTypeConverter() // For wrapped objects
        };
    }

    public string ConvertParameterPythonToC(
        string variableName,
        ExportedTypeDefinition typeDefinition,
        bool isNullable,
        bool isOutParameter = false)
    {
        var context = new ConversionContext(
            ConversionDirection.PythonToC,
            isNullable,
            isOutParameter,
            typeDefinition,
            variableName);

        var converter = FindConverter(typeDefinition.FullyQualifiedName, typeDefinition.SpecialCaseFlags);
        if (converter == null)
        {
            throw new NotSupportedException($"No converter found for type: {typeDefinition.FullyQualifiedName}");
        }

        var result = converter.Convert(context);

        if (isNullable && result.RequiresNullCheck)
        {
            return $"""
if {variableName} is None:
    {variableName}{Typed} = {Ffi}.NULL
else:
    {result.ConversionExpression}

""";
        }

        return result.ConversionExpression;
    }

    public string? ConvertReturnValueCToPython(
        ExportedTypeDefinition typeDefinition,
        bool isNullable)
    {
        var context = new ConversionContext(
            ConversionDirection.CToPython,
            isNullable,
            IsOutParameter: false,
            typeDefinition,
            VariableName: ""); // Not used for return values

        var converter = FindConverter(typeDefinition.FullyQualifiedName, typeDefinition.SpecialCaseFlags);
        if (converter == null)
        {
            return null; // No conversion needed
        }

        var result = converter.Convert(context);

        if (isNullable && result.RequiresNullCheck)
        {
            return $"""
if {InternalPyResult} == {Ffi}.NULL:
    {ExportedPyResult} = None
else:
    {result.ConversionExpression}
""";
        }

        return result.ConversionExpression;
    }

    public string? ConvertExposedToInternal(
        string variableName,
        string fullyQualifiedTypeName,
        TypeSpecialCaseFlags flags,
        bool isNullable)
    {
        var context = new ConversionContext(
            ConversionDirection.ExposedToInternal,
            isNullable,
            IsOutParameter: false,
            new ExportedTypeDefinition { FullyQualifiedName = fullyQualifiedTypeName, SpecialCaseFlags = flags },
            variableName);

        var converter = FindConverter(fullyQualifiedTypeName, flags);
        if (converter == null)
        {
            return null; // No conversion needed
        }

        var result = converter.Convert(context);
        return result.ConversionExpression;
    }

    private ITypeConverter? FindConverter(string fullyQualifiedTypeName, TypeSpecialCaseFlags flags)
    {
        return _converters.FirstOrDefault(c => c.CanHandle(fullyQualifiedTypeName, flags));
    }
}