using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiMethodBuilder(ClassBuilderContext classContext, StringBuilder mainPy)
{
    private readonly HashSet<string> methodNames = [];

    public IEnumerable<string> MethodNames => this.methodNames;

    public void AddClassToMainAndInitPy(ExportedMethodInfo method)
    {
        var context = new MethodBuilderContext(classContext, method);
        var (returnWrapPrefix, returnWrapSuffix) = method switch
        {
            { OriginalType: "string" } => ($"str(CString(", "))"),
            { OriginalType: "bool" } => ($"bool(", ")"),
            { OriginalType: "int[]" } => ($"Collection[int](", ")"),
            { ExposedTypeIfDifferent: not null } => (
                $"{method.OriginalTypeWrapper}.{FromPtr}(",
                ")"
            ),
            _ => ("", ""),
        };

        this.GenerateSingleMethod(context, returnWrapPrefix, returnWrapSuffix);
    }

    public static (string prefix, string suffix) GetToPythonTransformation(
        IHasOriginalAndExposedTypes method
    )
    {
        return method switch
        {
            { OriginalType: "string" } => ($"str(CString(", "))"),
            { OriginalType: "bool" } => ($"bool(", ")"),
            { ExposedTypeIfDifferent: not null } => (
                $"{method.OriginalTypeWrapper}.{FromPtr}(",
                ")"
            ),
            _ => ("", ""),
        };
    }

    public void GenerateSingleMethod(
        MethodBuilderContext context,
        string? resultToExportTypePrefix,
        string? resultToExportTypeSuffix
    )
    {
        var methodInfo = context.MethodInfo;
        var cLibMethodArgs = context.GetCMethodCallArgumentsString();

        var pyReturnType = context.GetReturnType();
        var methodName = context.GetMethodName(this.methodNames);
        var paramListWithHints = context.PythonMethodParamListWithHints();

        var returnCall = "return ";
        if (methodName == "__init__")
        {
            paramListWithHints = paramListWithHints.Prepend("self");
            pyReturnType = "None";
            resultToExportTypePrefix = $"self.{Ptr} = ";
            resultToExportTypeSuffix = string.Empty;
            returnCall = string.Empty;
        }

        if (methodInfo.IsStatic && methodName != "__init__")
        {
            mainPy.AppendLine($"    @staticmethod");
        }
        else if (methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertyGetter))
        {
            methodName = methodName["get_".Length..];
            mainPy.AppendLine($"    @property");
        }
        else if (methodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertySetter))
        {
            methodName = methodName["set_".Length..];
            mainPy.AppendLine($"    @{methodName}.setter");
        }

        mainPy.AppendLine(
            $"    def {methodName}({string.Join(", ", paramListWithHints)}){$" -> {pyReturnType}"}:"
        );

        var docComment = methodInfo.GetMethodComment("        ");
        if (!string.IsNullOrWhiteSpace(docComment))
        {
            mainPy.AppendLine(docComment);
        }

        var libCall = $"{Lib}.{context.ClassContext.ClassInfo.EntryPrefix}{methodInfo.StampedName}";

        mainPy.AppendLine(
            $"        {returnCall}{resultToExportTypePrefix}{libCall}({cLibMethodArgs}){resultToExportTypeSuffix}"
        );

        mainPy.AppendLine();
    }
}
