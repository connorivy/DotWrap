using System.Collections.Generic;
using System.Linq;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

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
                    p.ExposedTypeIfDifferent is null ? p.Name : $"{p.Name}.{Ptr}"
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

        return PythonUtils.ToSnakeCase(methodName);
    }

    public IEnumerable<string> PythonMethodParamListWithHints(
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
            $"{p.Name}: {p.MapOriginalTypeToPython(genericParamsToArgsDict)}"
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
};
