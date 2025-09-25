using System;
using DotWrap.Configuration;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.Utils.Python;

public class PythonInteropUtils
{
    public static string? GetExternalResultAssignment(
        ExportedTypeDefinition typeDefinition,
        bool isNullable
    )
    {
        var resultAssignment = (typeDefinition, isNullable) switch
        {
            _ when typeDefinition.FullyQualifiedName.Equals(
                    "string",
                    StringComparison.OrdinalIgnoreCase
                ) => $"{ExportedPyResult} = str(CString({InternalPyResult}))",
            _ when typeDefinition.FullyQualifiedName.Equals(
                    "system.guid",
                    StringComparison.OrdinalIgnoreCase
                ) => @$"
{InternalPythonPrefix}guid_bytes = bytes({Ffi}.buffer({InternalPyResult}, 16))
{ExportedPyResult} = uuid.UUID(bytes={InternalPythonPrefix}guid_bytes)",

            _ when typeDefinition.FullyQualifiedName.Equals(
                    "bool",
                    StringComparison.OrdinalIgnoreCase
                ) => $"{ExportedPyResult} = bool({InternalPyResult})",
            _ when typeDefinition.FullyQualifiedName.Equals(
                    "char",
                    StringComparison.OrdinalIgnoreCase
                ) => $"{ExportedPyResult} = chr({InternalPyResult})",
            _ when typeDefinition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Enum) =>
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.TypeNameNoGenerics)}({InternalPyResult})",
            // (_, true)
            //     when typeDefinition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.ValueType) =>
            //     $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName("Nullable[[" + typeDefinition.SimplifiedAssemblyQualifiedName + "]]")}.{FromPtr}({InternalPyResult})",
            ({ IsSameAsExposedType: false }, _) => (
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(typeDefinition.SimplifiedAssemblyQualifiedName)}.{FromPtr}({InternalPyResult})"
            ),
            _ => null,
        };

        if (isNullable)
        {
            if (typeDefinition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.ValueType))
            {
                return $"""
{resultAssignment ?? $"{ExportedPyResult} = {InternalPyResult}"}
{InternalPythonPrefix}nullable = {PythonNamingUtils.PythonizeClassName(
                        "Nullable[[" + typeDefinition.SimplifiedAssemblyQualifiedName + "]]"
                    )}.{FromPtr}({ExportedPyResult})
if {InternalPythonPrefix}nullable.has_value:
    {ExportedPyResult} = {InternalPythonPrefix}nullable.value
else:
    {ExportedPyResult} = None
""";
            }
            return $"""
if {InternalPyResult} == {Ffi}.NULL:
    {ExportedPyResult} = None
else:
    {resultAssignment}
""";
        }
        return resultAssignment;
    }
}
