using System;
using DotWrap.Configuration;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.Utils.Python;

public class PythonInteropUtils
{
    public static string? GetExternalResultAssignment(ExportedTypeDefinition typeDefinition)
    {
        return typeDefinition switch
        {
            { TypeNameNoGenerics: "string" } =>
                $"{ExportedPyResult} = str(CString({InternalPyResult}))",
            { TypeNameNoGenerics: "bool" } => $"{ExportedPyResult} = bool({InternalPyResult})",
            _ when typeDefinition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Enum) =>
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.TypeNameNoGenerics)}({InternalPyResult})",
            { IsSameAsExposedType: false } => (
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.FullyQualifiedName)}.{FromPtr}({InternalPyResult})"
            ),
            _ => null,
        };
    }
}
