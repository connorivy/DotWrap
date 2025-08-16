using System;
using DotWrap.Configuration;
using DotWrap.TypeConversion;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.Utils.Python;

public class PythonInteropUtils
{
    private static readonly ITypeConversionService _conversionService = new TypeConversionService();

    public static string? GetExternalResultAssignment(
        ExportedTypeDefinition typeDefinition,
        bool isNullable
    )
    {
        // Use the consolidated conversion service
        return _conversionService.ConvertReturnValueCToPython(typeDefinition, isNullable);
    }
}
