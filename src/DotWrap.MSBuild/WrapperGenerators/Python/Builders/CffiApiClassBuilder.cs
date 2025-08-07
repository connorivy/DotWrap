using System;
using System.Collections.Generic;
using System.Linq;
using DotWrap.Configuration;
using DotWrap.Utils;
using static DotWrap.Internal.Constants;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

internal class CffiApiClassBuilder(
    GlobalContext globalContext,
    PythonProjectInfo pythonProjectInfo,
    IndentedStringBuilder mainPy,
    IndentedStringBuilder initPy
)
{
    private HashSet<string> classNames = new();

    public void AddClassesToMainAndInitPy(IEnumerable<ExportedTypeDefinitionInfo> classes)
    {
        foreach (var cls in classes)
        {
            IndentedPythonStringBuilder classBodyBuilder = new();

            string? genericClassName = PythonUtils.GetGenericBaseNameOrNull(cls.TypeName);
            IndentedPythonStringBuilder? genericClassBodyBuilder = null;
            if (genericClassName is not null && this.classNames.Add(genericClassName))
            {
                genericClassBodyBuilder = this.CreateGenericClassBodyBuilder(cls);
            }

            using var indentGeneric = genericClassBodyBuilder?.IndentUntilDispose();
            AddClassToMainAndInitPy(cls, classBodyBuilder, genericClassBodyBuilder);

            using var indent = classBodyBuilder.IndentUntilDispose();
            foreach (var config in GetApplicableConfigs(globalContext.Configs, cls))
            {
                if (genericClassBodyBuilder is not null)
                {
                    config.Item2.ConfigureGenericClassBody(
                        cls,
                        config.Item1,
                        genericClassBodyBuilder
                    );
                }
                config.Item2.ConfigureClassBody(cls, config.Item1, classBodyBuilder);
            }

            if (genericClassBodyBuilder is not null)
            {
                mainPy.AppendLine("");
                mainPy.AppendLine(genericClassBodyBuilder.ToString());
            }
            mainPy.AppendLine(classBodyBuilder.ToString());
        }
    }

    private IndentedPythonStringBuilder CreateGenericClassBodyBuilder(
        ExportedTypeDefinitionInfo cls
    )
    {
        IndentedPythonStringBuilder genericClassBuilder = new();
        string genericClassName =
            PythonUtils.GetGenericBaseNameOrNull(cls.TypeName)
            ?? throw new ArgumentException("Class name must be a generic type with '<' and '>'");
        var genericParams = cls.GenericTypeArgumentsToParameters.Select(kvp => kvp.Value).ToList();

        foreach (var param in genericParams)
        {
            genericClassBuilder.AppendLine($"{param} = TypeVar('{param}')");
        }
        genericClassBuilder.AppendLine(
            $"class {genericClassName}(Generic[{string.Join(", ", genericParams)}]):"
        );
        using var _ = genericClassBuilder.IndentUntilDispose();

        if (!string.IsNullOrWhiteSpace(cls.SummaryComment))
        {
            genericClassBuilder.AppendLine(
                @$"    
""""""
{cls.SummaryComment}
"""""""
            );
        }
        genericClassBuilder.AppendLine(
            @$"
def __del__(self) -> None:
    pass
        "
        );

        return genericClassBuilder;
    }

    private void AddClassToMainAndInitPy(
        ExportedTypeDefinitionInfo classInfo,
        IndentedPythonStringBuilder classBodyBuilder,
        IndentedPythonStringBuilder? genericClassBodyBuilder
    )
    {
        var baseClassName = PythonUtils.GetGenericBaseNameOrNull(classInfo.TypeName);
        string className = PythonUtils.PythonizeClassName(classInfo.TypeName);

        if (genericClassBodyBuilder is not null)
        {
            initPy.AppendLine($"from .main import {baseClassName}");
        }
        else
        {
            initPy.AppendLine($"from .main import {className}");
        }

        var genericDef = string.Join(
            ", ",
            classInfo.GenericTypeArgumentsToParameters.Select(kvp =>
                PythonUtils.MapTypeToPython(kvp.Key)
            )
        );
        if (!string.IsNullOrEmpty(genericDef))
        {
            genericDef = $"({baseClassName}[{genericDef}])";
        }

        classBodyBuilder.AppendLine($"class {className}{genericDef}:");
        using var _ = classBodyBuilder.IndentUntilDispose();

        if (!string.IsNullOrWhiteSpace(classInfo.SummaryComment))
        {
            classBodyBuilder.AppendLine(
                @$"    
""""""
{classInfo.SummaryComment}
"""""""
            );
        }

        var classContext = new ClassBuilderContext(globalContext, pythonProjectInfo, classInfo);
        var methodBuilder = new CffiApiMethodBuilder(classContext, classBodyBuilder);
        foreach (var method in classInfo.Methods)
        {
            if (method.OriginalName.StartsWith(InternalPrefix))
            {
                continue;
            }
            methodBuilder.AddClassToMainAndInitPy(method, genericClassBodyBuilder);
        }

        if (!classInfo.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Static))
        {
            classBodyBuilder.AppendLine(
                @$"
@classmethod
def {FromPtr}(cls, ptr: int):
    instance = object.__new__(cls)
    instance.{Ptr} = ptr
    return instance

def __del__(self):
    {Lib}.{classInfo.EntryPrefix}{Destroy}(self.{Ptr})
"
            );
        }

        if (
            classInfo.TryGetICollectionType(out var genericType)
            || classInfo.TryGetIReadonlyCollectionType(out genericType)
        )
        {
            var genericArg = PythonUtils.MapTypeToPython(genericType);
            var exposedType = DotWrapUtils.GetExposedTypeFromCsType(
                genericType,
                out bool isOriginalType
            );
            var numpyType = PythonUtils.MapTypeToNumpy(exposedType);
            var tolistMethodDef = $"def to_list(self) -> list[\"{genericArg}\"]:";
            classBodyBuilder.AppendLine(tolistMethodDef);
            genericClassBodyBuilder?.AppendLine(tolistMethodDef);
            genericClassBodyBuilder?.AppendLine("    pass");
            using var indent1 = classBodyBuilder.IndentUntilDispose();
            classBodyBuilder.AppendLine(
                @$"
""""""
Converts the array data to a list of the specified dtype.
""""""
length = {Lib}.{classInfo.EntryPrefix}{GetCount}(self.{Ptr})
arr = np.empty(length, dtype={numpyType})

# get stable pointer to the array data
arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
{Lib}.{classInfo.EntryPrefix}{FillArr}(self.{Ptr}, arr_ptr, length)
        "
            );

            if (isOriginalType)
            {
                classBodyBuilder.AppendLine("return arr.tolist()");
            }
            else
            {
                OriginalAndExposedTypeInfo genericTypeInfo = new(
                    genericType,
                    isOriginalType ? null : exposedType
                );

                var (prefix, suffix) = CffiApiMethodBuilder.GetToPythonTransformation(
                    genericTypeInfo
                );
                classBodyBuilder.AppendLine("final_list = []");
                using (
                    var forBlock = classBodyBuilder.AppendLineWithNewBlock(
                        "for i in range(length):"
                    )
                )
                {
                    if (numpyType == "np.intp")
                    {
                        classBodyBuilder.AppendLine($"val = {Ffi}.cast('void *', arr[i])");
                    }
                    else
                    {
                        classBodyBuilder.AppendLine($"val = arr[i]");
                    }
                    classBodyBuilder.AppendLine($"final_list.append({prefix}val{suffix})");
                }
                classBodyBuilder.AppendLine("return final_list");
            }
        }
    }

    /// <summary>
    /// Gets the applicable config objects for a type info object.
    /// </summary>
    /// <param name="configs"></param>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public IEnumerable<(Type, DotWrapPythonTypeConfig)> GetApplicableConfigs(
        Dictionary<Type, DotWrapPythonTypeConfig> configs,
        ExportedTypeDefinitionInfo typeInfo
    )
    {
        foreach (var strongType in GetTypesThatCouldHaveConfigs(typeInfo))
        {
            if (configs.TryGetValue(strongType, out var config))
            {
                yield return (strongType, config);
            }

            if (strongType.IsGenericType)
            {
                var genericTypeDef = strongType.GetGenericTypeDefinition();
                if (configs.TryGetValue(genericTypeDef, out var genericConfig))
                {
                    yield return (strongType, genericConfig);
                }
            }
        }
    }

    private static IEnumerable<Type> GetTypesThatCouldHaveConfigs(
        ExportedTypeDefinitionInfo typeInfo
    )
    {
        foreach (var typeString in typeInfo.Interfaces.Prepend(typeInfo.FullyQualifiedName))
        {
            // Logger.LogDebug($"Checking for config for type {typeString}");
            var strongType = Type.GetType(typeString);

            if (strongType is null)
            {
                Logger.LogWarning(
                    $"Could not find type {typeString} for class {typeInfo.TypeName}."
                );
                continue;
            }

            yield return strongType;

            var baseType = strongType.BaseType;
            while (baseType is not null && baseType != typeof(object))
            {
                yield return baseType;
                baseType = baseType.BaseType;
            }
        }
    }
}

public record OriginalAndExposedTypeInfo(
    string OriginalTypeName,
    string? ExposedTypeIfDifferent = null
) : IHasOriginalAndExposedTypes
{
    public string Name => OriginalTypeName;
};
