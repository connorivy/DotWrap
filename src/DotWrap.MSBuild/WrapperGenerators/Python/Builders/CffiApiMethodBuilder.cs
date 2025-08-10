using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiMethodBuilder(ClassBuilderContext classContext, IndentedStringBuilder mainPy)
{
    private readonly HashSet<string> methodNames = [];

    public IEnumerable<string> MethodNames => this.methodNames;

    public void AddClassToMainAndInitPy(
        ExportedMethodInfo method,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        var context = new MethodBuilderContext(classContext, method);
        var exportedResultAssignment = GetExternalResultAssignment(method);

        this.GenerateSingleMethod(context, exportedResultAssignment, genericClassBodyBuilder);
    }

    public static (string prefix, string suffix) GetToPythonTransformation(
        IHasOriginalAndExposedTypes method
    )
    {
        return method switch
        {
            { OriginalTypeName: "string" } => ($"str(CString(", "))"),
            { OriginalTypeName: "bool" } => ($"bool(", ")"),
            { ExposedTypeIfDifferent: not null } => (
                $"{method.OriginalTypeWrapper}.{FromPtr}(",
                ")"
            ),
            _ => ("", ""),
        };
    }

    public static string? GetExternalResultAssignment(ExportedMethodInfo method)
    {
        return method switch
        {
            { OriginalTypeName: "string" } =>
                $"{ExportedPyResult} = str(CString({InternalPyResult}))",
            { OriginalTypeName: "bool" } => $"{ExportedPyResult} = bool({InternalPyResult})",
            _ when method.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.EnumReturnType) =>
                $"{ExportedPyResult} = {PythonNamingUtils.PythonizeClassName(method.OriginalTypeName)}({InternalPyResult})",
            { ExposedTypeIfDifferent: not null } => (
                $"{ExportedPyResult} = {method.OriginalTypeWrapper}.{FromPtr}({InternalPyResult})"
            ),
            _ => null,
        };
    }

    public void GenerateSingleMethod(
        MethodBuilderContext context,
        string? exportedResultAssignment,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        var methodInfo = context.MethodInfo;
        var cLibMethodArgs = context.GetCMethodCallArgumentsString();

        // var pyReturnType = context.GetReturnType(null);
        var pyReturnType = PythonNamingUtils.MapTypeToPython(
            context.MethodInfo.OriginalTypeName,
            context.ClassContext.ClassInfo.GenericTypeArgumentsToParameters,
            false
        );
        var genericReturnType = PythonNamingUtils.MapTypeToPython(
            context.MethodInfo.OriginalTypeName,
            context.ClassContext.ClassInfo.GenericTypeArgumentsToParameters,
            true
        );
        var methodName = context.GetMethodName(this.methodNames);
        var paramListWithHints = context.PythonMethodParamListWithHints();
        var genericParamListWithHints = context.PythonGenericMethodParamListWithHints();

        string? internalResultAssignment;
        internalResultAssignment = $"{InternalPyResult} = ";
        var returnCall = "return ";
        if (methodName == "__init__")
        {
            paramListWithHints = paramListWithHints.Prepend("self");
            genericParamListWithHints = genericParamListWithHints.Prepend("self");
            pyReturnType = "None";
            // resultToExportTypePrefix = $"self.{Ptr} = ";
            // resultToExportTypeSuffix = string.Empty;
            internalResultAssignment = $"self.{Ptr} = ";
            returnCall = string.Empty;
        }

        if (methodInfo.IsStatic && methodName != "__init__")
        {
            mainPy.AppendLine($"@staticmethod");
            genericClassBodyBuilder?.AppendLine($"@staticmethod");
        }
        else if (
            methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertyGetter)
            && !methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer)
        )
        {
            methodName = methodName["get_".Length..];
            mainPy.AppendLine($"@property");
            genericClassBodyBuilder?.AppendLine($"@property");
        }
        else if (
            methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertySetter)
            && !methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer)
        )
        {
            methodName = methodName["set_".Length..];
            mainPy.AppendLine($"@{methodName}.setter");
            genericClassBodyBuilder?.AppendLine($"@{methodName}.setter");
        }

        mainPy.AppendLine(
            $"def {methodName}({string.Join(", ", paramListWithHints)}){$" -> {pyReturnType}"}:"
        );
        genericClassBodyBuilder?.AppendLine(
            $"def {methodName}({string.Join(", ", genericParamListWithHints)}){$" -> {genericReturnType ?? pyReturnType}"}:"
        );
        genericClassBodyBuilder?.AppendLine("    pass");
        using var indent = mainPy.IndentUntilDispose();

        var docComment = methodInfo.GetMethodComment();
        if (!string.IsNullOrWhiteSpace(docComment))
        {
            mainPy.AppendLine(docComment);
        }

        var pythonParamsToCParams = context.ConvertPythonParamsToCParams();
        foreach (var param in pythonParamsToCParams)
        {
            mainPy.AppendLine(param);
        }

        var libCall = $"{Lib}.{context.ClassContext.ClassInfo.EntryPrefix}{methodInfo.StampedName}";

        mainPy.AppendLine($"{internalResultAssignment}{libCall}({cLibMethodArgs})");

        if (returnCall == string.Empty)
        {
            mainPy.AppendLine();
            return;
        }

        if (exportedResultAssignment is not null)
        {
            mainPy.AppendLine(exportedResultAssignment);
            mainPy.AppendLine($"return {ExportedPyResult}");
        }
        else if (internalResultAssignment is not null)
        {
            mainPy.AppendLine($"return {InternalPyResult}");
        }

        // mainPy.AppendLine(
        //     $"        {returnCall}{resultToExportTypePrefix}{libCall}({cLibMethodArgs}){resultToExportTypeSuffix}"
        // );

        mainPy.AppendLine();
    }
}
