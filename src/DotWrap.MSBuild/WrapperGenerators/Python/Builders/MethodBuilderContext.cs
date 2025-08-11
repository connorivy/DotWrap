using System.Collections.Generic;
using System.Linq;
using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using DotWrap.Utils.Python;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

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
                    p.ExposedTypeIfDifferent is null ? p.Name : $"{p.Name}{Typed}"
                )
            );
    }

    public string GetReturnType(IDictionary<string, string>? genericParamsToArgsDict) =>
        this.MethodInfo.MapOriginalTypeToPython(genericParamsToArgsDict);

    public string GetMethodName(HashSet<string> methodNames)
    {
        int numTries = 0;
        string methodName = this.MethodInfo.OriginalName;
        while (!methodNames.Add(methodName))
        {
            numTries++;
            methodName = $"{this.MethodInfo.OriginalName}_{numTries}";
        }

        if (methodName == "Constructor")
        {
            methodName = "__init__";
        }

        if (this.MethodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertyGetter))
        {
            if (this.MethodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer))
            {
                methodName = "__getitem__";
            }
        }
        else if (this.MethodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertySetter))
        {
            if (this.MethodInfo.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.Indexer))
            {
                methodName = "__setitem__";
            }
        }

        return PythonNamingUtils.ToSnakeCase(methodName);
    }

    public IEnumerable<string> PythonMethodParamListWithHints(
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        // var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
        //     $"{p.Name}: {p.MapOriginalTypeToPython(genericParamsToArgsDict)}"
        // );
        var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
            $"{p.Name}: {p.PythonizeTypeName(genericParamsToArgsDict, this.ClassContext.GlobalContext.TypeDefinitions)}"
        );

        if (!this.MethodInfo.IsStatic)
        {
            paramListWithHints = paramListWithHints.Prepend("self");
        }
        return paramListWithHints;
    }

    public IEnumerable<string> PythonGenericMethodParamListWithHints(
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
            $"{p.Name}: {this.ClassContext.ClassInfo.GenericTypeArgumentsToParameters?.GetValueOrDefault(p.OriginalTypeName) ?? p.MapOriginalTypeToPython(genericParamsToArgsDict)}"
        );

        if (!this.MethodInfo.IsStatic)
        {
            paramListWithHints = paramListWithHints.Prepend("self");
        }
        return paramListWithHints;
    }

    public string PythonMethodGenericParamListWithHints(
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
            $"{p.Name}: {p.GenericTypeName ?? p.MapOriginalTypeToPython(genericParamsToArgsDict)}"
        );

        if (!this.MethodInfo.IsStatic)
        {
            paramListWithHints = paramListWithHints.Prepend("self");
        }
        return string.Join(", ", paramListWithHints);
    }

    public IEnumerable<string> ConvertPythonParamsToCParams()
    {
        foreach (var param in this.MethodInfo.Parameters)
        {
            if (param.ExposedTypeIfDifferent is null)
            {
                continue;
            }

            var definition = ClassContext.GlobalContext.TypeDefinitions[
                param.Type.DefinitionId.ToString()
            ];

            if (param.SpecialCaseFlags.HasFlag(ParameterSpecialCaseFlags.Out))
            {
                var outTypeName = param.PythonizeTypeName(
                    null,
                    this.ClassContext.GlobalContext.TypeDefinitions
                );
                ClassContext.GlobalContext.OutParams.Add(new OutParamInfo(outTypeName, definition));
                yield return $"{param.Name}{Typed} = {param.Name}.{OutVal}";
            }
            else if (definition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Enum))
            {
                yield return $"{param.Name}{Typed} = {PythonNamingUtils.MapTypeToPython(param.ExposedTypeIfDifferent)}({param.Name}.value)";
            }
            else
            {
                yield return $"{param.Name}{Typed} = {param.Name}.{Ptr}";
            }
        }
    }
};
