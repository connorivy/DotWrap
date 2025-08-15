using System.Collections.Generic;
using System.Linq;
using DotWrap.Configuration;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using DotWrap.Utils.Python;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public record MethodBuilderContext(ClassBuilderContext ClassContext, ExportedMethodInfo MethodInfo)
{
    internal string GetCMethodCallArgumentsString()
    {
        var parameterStrings = this
            .MethodInfo.Parameters.Select(p =>
                p.ExposedTypeIfDifferent is null ? p.Name : $"{p.Name}{Typed}"
            )
            .Append(ExceptionInfoArg);

        if (!this.MethodInfo.IsStatic)
        {
            parameterStrings = parameterStrings.Prepend($"self.{Ptr}");
        }

        // var parameters = this.MethodInfo.Parameters.Append(
        //     new()
        //     {
        //         Name = "ExceptionInfoPtr",
        //         OriginalTypeName = "IntPtr",
        //         ExposedTypeIfDifferent = "IntPtr",
        //         SpecialCaseFlags = ParameterSpecialCaseFlags.Out,
        //         GenericTypeName = "IntPtr",
        //         Type = new ExportedTypeInstanceInfo
        //         {
        //             DefinitionId = ExportedTypeId.IntPtr,
        //             DefinitionGenericArgs = null,
        //             GenericName = "IntPtr",
        //             IsNullable = false,
        //         },
        //     }
        // );

        return string.Join(", ", parameterStrings);
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
        var paramListWithHints = this.MethodInfo.Parameters.Select(p =>
        {
            var IsNullable = p.Type.IsNullable;
            var returnTypeAnnotation = p.PythonizeTypeName(
                genericParamsToArgsDict,
                this.ClassContext.PythonContext.GlobalContext.TypeDefinitions
            );
            return $"{p.Name}: {(IsNullable ? "Optional[" + returnTypeAnnotation + "]" : returnTypeAnnotation)}";
        });

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

            var definition = ClassContext.PythonContext.GlobalContext.TypeDefinitions[
                param.Type.DefinitionId.ToString()
            ];

            // string nullableSuffix;

            string typedVarAssignment;
            if (param.SpecialCaseFlags.HasFlag(ParameterSpecialCaseFlags.Out))
            {
                var outTypeName = param.PythonizeTypeName(
                    null,
                    this.ClassContext.PythonContext.GlobalContext.TypeDefinitions
                );
                ClassContext.PythonContext.GlobalContext.OutParams.Add(
                    new OutParamInfo(outTypeName, param.Type, definition)
                );
                typedVarAssignment = $"{param.Name}{Typed} = {param.Name}.{OutVal}";
            }
            else if (definition.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Enum))
            {
                typedVarAssignment =
                    $"{param.Name}{Typed} = {PythonNamingUtils.MapTypeToPython(param.ExposedTypeIfDifferent)}({param.Name}.value)";
            }
            else if (
                definition.FullyQualifiedName.Equals("string", StringComparison.OrdinalIgnoreCase)
            )
            {
                typedVarAssignment =
                    $"{param.Name}{Typed} = {Ffi}.new(\"char[]\", {param.Name}.encode(\"utf-8\"))";
            }
            else if (
                definition.FullyQualifiedName.Equals("bool", StringComparison.OrdinalIgnoreCase)
            )
            {
                typedVarAssignment = $"{param.Name}{Typed} = int({param.Name})";
            }
            else if (
                definition.FullyQualifiedName.Equals("char", StringComparison.OrdinalIgnoreCase)
            )
            {
                typedVarAssignment = $"{param.Name}{Typed} = ord({param.Name})";
            }
            else if (
                definition.FullyQualifiedName.Equals(
                    "System.Half",
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                typedVarAssignment = $"{param.Name}{Typed} = {param.Name}"; // half is already represented by a float and does not need conversion
            }
            else
            {
                typedVarAssignment = $"{param.Name}{Typed} = {param.Name}.{Ptr}";
            }

            if (param.Type.IsNullable)
            {
                // nullableSuffix = $" or {Ffi}.NULL";
                yield return $"""
if {param.Name} is None:
    {param.Name}{Typed} = {Ffi}.NULL
else:
    {typedVarAssignment}

""";
            }
            else
            {
                yield return typedVarAssignment;
            }
        }
    }

    public ExportedTypeDefinition ReturnTypeDefinition =>
        field ??= ClassContext.PythonContext.GlobalContext.TypeDefinitions[
            MethodInfo.ReturnType.DefinitionId.ToString()
        ];
};
