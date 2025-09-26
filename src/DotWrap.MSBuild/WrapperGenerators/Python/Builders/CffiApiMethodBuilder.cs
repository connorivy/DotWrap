using System.Text.Json;
using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiMethodBuilder(ClassBuilderContext classContext, IndentedStringBuilder mainPy)
{
    private readonly HashSet<string> methodNames = [];

    public IEnumerable<string> MethodNames => this.methodNames;

    private static readonly Dictionary<string, string> methodNameReplacements = new()
    {
        { "from", "from_" },
        { "global", "global_" },
        { "nonlocal", "nonlocal_" },
        { "pass", "pass_" },
        { "raise", "raise_" },
        { "with", "with_" },
        { "as", "as_" },
        { "assert", "assert_" },
        { "break", "break_" },
        { "class", "class_" },
        { "continue", "continue_" },
        { "def", "def_" },
        { "del", "del_" },
        { "elif", "elif_" },
        { "else", "else_" },
        { "except", "except_" },
        { "finally", "finally_" },
        { "for", "for_" },
        { "if", "if_" },
        { "import", "import_" },
        { "in", "in_" },
        { "is", "is_" },
        { "lambda", "lambda_" },
        { "not", "not_" },
        { "or", "or_" },
        { "return", "return_" },
        { "try", "try_" },
        { "while", "while_" },
        { "yield", "yield_" },
    };

    public void AddClassToMainAndInitPy(
        ExportedMethodInfo method,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        var context = new MethodBuilderContext(classContext, method);
        Logger.LogDebug(
            $"Return type definition: {JsonSerializer.Serialize(context.ReturnTypeDefinition)}"
        );

        var exportedResultAssignment = PythonInteropUtils.GetExternalResultAssignment(
            context.ReturnTypeDefinition,
            context.MethodInfo.ReturnType.IsNullable
        );

        this.GenerateSingleMethod(context, exportedResultAssignment, genericClassBodyBuilder);
    }

    public void GenerateSingleMethod(
        MethodBuilderContext context,
        string? exportedResultAssignment,
        IndentedStringBuilder? genericClassBodyBuilder
    )
    {
        var methodInfo = context.MethodInfo;
        var cLibMethodArgs = context.GetCMethodCallArgumentsString();

        var pyReturnType = PythonNamingUtils.MapTypeToPython(
            context.ReturnTypeDefinition.FullyQualifiedName,
            context.ClassContext.ClassInfo.GenericTypeParametersToArguments,
            false
        );
        pyReturnType = context.MethodInfo.ReturnType.IsNullable
            ? $"Optional[{pyReturnType}]"
            : pyReturnType;
        pyReturnType = "\"" + pyReturnType + "\"";

        var genericReturnType = PythonNamingUtils.MapTypeToPython(
                context.ReturnTypeDefinition.FullyQualifiedName,
                context.MethodInfo.ReturnType.DefinitionGenericParamsToArgs,
                true
            );
        genericReturnType = genericReturnType is not null
            ? "\"" + genericReturnType + "\""
            : null;

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

        methodName = methodNameReplacements.GetValueOrDefault(methodName, methodName);

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

        mainPy.AppendLine($"{ExceptionInfoArg} = {Ffi}.new(\"ExceptionInfo *\")");
        mainPy.AppendLine($"{internalResultAssignment}{libCall}({cLibMethodArgs})");
        mainPy.AppendLine($"_raise_exception({ExceptionInfoArg})");

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
