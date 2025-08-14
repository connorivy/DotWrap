using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.Utils.Python;

public class PythonInteropUtils
{
    public static string? GetExternalResultAssignment(ExportedTypeDefinition typeDefinition)
    {
        return typeDefinition switch
        {
            _ when typeDefinition.TypeNameNoGenerics.Equals(
                    "string",
                    StringComparison.InvariantCultureIgnoreCase
                ) => $"{ExportedPyResult} = str(CString({InternalPyResult}))",

            _ when typeDefinition.TypeNameNoGenerics.Equals(
                    "bool",
                    StringComparison.InvariantCultureIgnoreCase
                ) => $"{ExportedPyResult} = bool({InternalPyResult})",
            _ when typeDefinition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Enum) =>
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.TypeNameNoGenerics)}({InternalPyResult})",
            { IsSameAsExposedType: false } => (
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.FullyQualifiedName)}.{FromPtr}({InternalPyResult})"
            ),
            _ => null,
        };
    }
}
