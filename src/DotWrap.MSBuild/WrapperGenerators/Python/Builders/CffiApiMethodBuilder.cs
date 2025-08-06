using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.Internal;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

internal class CffiApiMethodBuilder(ClassBuilderContext classContext, IndentedStringBuilder mainPy)
{
    private readonly HashSet<string> methodNames = [];

    public IEnumerable<string> MethodNames => this.methodNames;

    public void AddClassToMainAndInitPy(ExportedMethodInfo method)
    {
        var context = new MethodBuilderContext(classContext, method);
        // var (returnWrapPrefix, returnWrapSuffix) = GetToPythonTransformation(method);
        var exportedResultAssignment = GetExternalResultAssignment(method);

        this.GenerateSingleMethod(context, exportedResultAssignment);
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
            { OriginalTypeName: "string" } => $"{ExportedResult} = str(CString({InternalResult}))",
            { OriginalTypeName: "bool" } => $"{ExportedResult} = bool({InternalResult})",
            _ when method.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.EnumReturnType) =>
                $"{ExportedResult} = {PythonUtils.PythonizeClassName(method.OriginalTypeName)}({InternalResult})",
            { ExposedTypeIfDifferent: not null } => (
                $"{ExportedResult} = {method.OriginalTypeWrapper}.{FromPtr}({InternalResult})"
            ),
            _ => null,
        };
    }

    public void GenerateSingleMethod(
        MethodBuilderContext context,
        // string? resultToExportTypePrefix,
        // string? resultToExportTypeSuffix,
        string? exportedResultAssignment
    )
    {
        var methodInfo = context.MethodInfo;
        var cLibMethodArgs = context.GetCMethodCallArgumentsString();

        var pyReturnType = context.GetReturnType(null);
        var methodName = context.GetMethodName(this.methodNames);
        var paramListWithHints = context.PythonMethodParamListWithHints();

        string? internalResultAssignment;
        internalResultAssignment = $"{InternalResult} = ";
        var returnCall = "return ";
        if (methodName == "__init__")
        {
            paramListWithHints = paramListWithHints.Prepend("self");
            pyReturnType = "None";
            // resultToExportTypePrefix = $"self.{Ptr} = ";
            // resultToExportTypeSuffix = string.Empty;
            internalResultAssignment = $"self.{Ptr} = ";
            returnCall = string.Empty;
        }

        if (methodInfo.IsStatic && methodName != "__init__")
        {
            mainPy.AppendLine($"@staticmethod");
        }
        else if (
            methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertyGetter)
            && !methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer)
        )
        {
            methodName = methodName["get_".Length..];
            mainPy.AppendLine($"@property");
        }
        else if (
            methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertySetter)
            && !methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer)
        )
        {
            methodName = methodName["set_".Length..];
            mainPy.AppendLine($"@{methodName}.setter");
        }

        mainPy.AppendLine(
            $"def {methodName}({string.Join(", ", paramListWithHints)}){$" -> {pyReturnType}"}:"
        );
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
            mainPy.AppendLine($"return {ExportedResult}");
        }
        else if (internalResultAssignment is not null)
        {
            mainPy.AppendLine($"return {InternalResult}");
        }

        // mainPy.AppendLine(
        //     $"        {returnCall}{resultToExportTypePrefix}{libCall}({cLibMethodArgs}){resultToExportTypeSuffix}"
        // );

        mainPy.AppendLine();
    }
}
