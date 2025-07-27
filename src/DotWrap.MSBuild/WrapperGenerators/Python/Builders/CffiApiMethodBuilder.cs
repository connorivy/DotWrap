using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiMethodBuilder(ClassBuilderContext classContext, StringBuilder mainPy)
{
    private readonly HashSet<string> methodNames = [];

    public void AddClassToMainAndInitPy(ExportedMethodInfo method)
    {
        var context = new MethodBuilderContext(classContext, method);
        var (returnWrapPrefix, returnWrapSuffix) = method switch
        {
            { OriginalType: "string" } => ($"str(CString(", "))"),
            { OriginalType: "bool" } => ($"bool(", ")"),
            { OriginalType: "int[]" } => ($"Collection[int](", ")"),
            { ExposedTypeIfDifferent: not null } => (
                $"{method.OriginalTypeSimple}.{FromPtr}(",
                ")"
            ),
            _ => ("", ""),
        };

        this.GenerateSingleMethod(context, returnWrapPrefix, returnWrapSuffix);
    }

    public void GenerateSingleMethod(
        MethodBuilderContext context,
        string? resultToExportTypePrefix,
        string? resultToExportTypeSuffix
    )
    {
        var methodInfo = context.MethodInfo;
        var cLibMethodArgs = context.GetCMethodCallArgumentsString();

        var paramListWithHints = string.Join(
            ", ",
            methodInfo.Parameters.Select(p => $"{p.Name}: {p.MapOriginalTypeToPython()}")
        );
        var paramNames = string.Join(", ", methodInfo.Parameters.Select(p => p.Name));
        var pyReturnType = methodInfo.MapOriginalTypeToPython();

        int numTries = 0;
        string methodName = methodInfo.OriginalName;
        while (!this.methodNames.Add(methodName))
        {
            numTries++;
            methodName = $"{methodInfo.OriginalName}_{numTries}";
        }

        var returnCall = "return ";
        if (methodName == "Constructor")
        {
            methodName = "__init__";
            pyReturnType = "None";
            resultToExportTypePrefix = $"self.{Ptr} = ";
            resultToExportTypeSuffix = string.Empty;
            returnCall = string.Empty;
        }

        string selfMethodParameter;
        if (methodInfo.IsStatic && methodName != "__init__")
        {
            mainPy.AppendLine($"    @staticmethod");
            selfMethodParameter = string.Empty;
        }
        else
        {
            selfMethodParameter = $"self{(methodInfo.Parameters.Count > 0 ? ", " : "")}";
        }

        mainPy.AppendLine(
            $"    def {methodName}({selfMethodParameter}{paramListWithHints}){$" -> {pyReturnType}"}:"
        );

        var docComment = methodInfo.GetMethodComment("        ");
        if (!string.IsNullOrWhiteSpace(docComment))
        {
            mainPy.AppendLine(docComment);
        }

        mainPy.AppendLine(
            $"        {returnCall}{resultToExportTypePrefix}{Lib}.{context.ClassContext.ClassInfo.EntryPrefix}{methodInfo.StampedName}({cLibMethodArgs}){resultToExportTypeSuffix}"
        );

        mainPy.AppendLine();
    }
}

public record ClassBuilderContext(PythonProjectInfo ProjectInfo, ExportedClassInfo ClassInfo) { };

public record MethodBuilderContext(ClassBuilderContext ClassContext, ExportedMethodInfo MethodInfo)
{
    internal string GetCMethodCallArgumentsString()
    {
        string self;
        if (this.MethodInfo.IsStatic)
        {
            self = string.Empty;
        }
        else
        {
            self = $"self.{Ptr}{(this.MethodInfo.Parameters.Count > 0 ? ", " : "")}";
        }

        return self
            + string.Join(
                ", ",
                this.MethodInfo.Parameters.Select(p =>
                {
                    return p.ExposedTypeIfDifferent is null ? p.Name : $"{p.Name}.{Ptr}";
                })
            );
    }
};
