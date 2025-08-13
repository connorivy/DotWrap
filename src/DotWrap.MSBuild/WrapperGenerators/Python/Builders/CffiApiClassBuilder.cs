using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using DotWrap.Configuration;
using DotWrap.Utils;
using DotWrap.Utils.Python;
using static DotWrap.Internal.Constants;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

internal class CffiApiClassBuilder(
    PythonContext pythonContext,
    PythonProjectInfo pythonProjectInfo,
    IndentedStringBuilder mainPy
)
{
    private HashSet<string> classNames = new();

    public void AddClassesToMainAndInitPy(IEnumerable<ExportedTypeDefinition> classes)
    {
        var globalContext = pythonContext.GlobalContext;
        foreach (var cls in classes)
        {
            if (
                cls.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.DirectlyBlittable)
                || cls.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.IndirectlyBlittable)
            )
            {
                continue;
            }

            IndentedPythonStringBuilder classBodyBuilder = new();

            IndentedPythonStringBuilder? genericClassBodyBuilder = null;
            var baseClassName = PythonNamingUtils.PythonizeClassName(cls.TypeNameNoGenerics);
            if (cls.GenericTypeArgumentsToParameters.Count > 0 && classNames.Add(baseClassName))
            {
                genericClassBodyBuilder = this.CreateGenericClassBodyBuilder(cls);
            }

            using var indentGeneric = genericClassBodyBuilder?.IndentUntilDispose();
            var initFileBuilder = pythonContext.ModuleBuilder.GetImportFile(cls.Namespace);
            AddClassToMainAndInitPy(
                cls,
                classBodyBuilder,
                genericClassBodyBuilder,
                initFileBuilder
            );

            using var indent = classBodyBuilder.IndentUntilDispose();
            foreach (var config in GetApplicableConfigs(pythonContext.Configs, cls))
            {
                PythonTypeConfigContext context = new(
                    globalContext.TypeDefinitions,
                    cls,
                    config.Item1,
                    classBodyBuilder
                );
                if (!config.Item2.ShouldConfigure(context))
                {
                    continue;
                }
                if (genericClassBodyBuilder is not null)
                {
                    config.Item2.ConfigureGenericClassBody(
                        context with
                        {
                            ClassBody = genericClassBodyBuilder,
                        }
                    );
                }
                config.Item2.ConfigureClassBody(context);
            }

            if (genericClassBodyBuilder is not null)
            {
                mainPy.AppendLine("");
                mainPy.AppendLine(genericClassBodyBuilder.ToString());
            }
            mainPy.AppendLine(classBodyBuilder.ToString());
        }
    }

    private IndentedPythonStringBuilder CreateGenericClassBodyBuilder(ExportedTypeDefinition cls)
    {
        IndentedPythonStringBuilder genericClassBuilder = new();
        string genericClassName = cls.TypeNameNoGenerics;
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
        ExportedTypeDefinition classInfo,
        IndentedPythonStringBuilder classBodyBuilder,
        IndentedPythonStringBuilder? genericClassBodyBuilder,
        InitFileBuilder initFileBuilder
    )
    {
        var baseClassName = PythonNamingUtils.PythonizeClassName(classInfo.TypeNameNoGenerics);
        var className = PythonNamingUtils.PythonizeClassName(classInfo.FullyQualifiedName);
        Logger.LogDebug(
            $"Adding class {className} with baseClass {baseClassName} to main.py with number of methods: {classInfo.Methods.Count}"
        );
        var isGeneric = classInfo.GenericTypeArgumentsToParameters.Count > 0;
        if (!isGeneric || genericClassBodyBuilder is not null)
        {
            initFileBuilder.AddTypeImport(baseClassName);
        }

        var genericDef = string.Join(
            ", ",
            classInfo.GenericTypeArgumentsToParameters.Select(kvp =>
                PythonNamingUtils.MapTypeToPython(kvp.Key)
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

        var classContext = new ClassBuilderContext(pythonContext, pythonProjectInfo, classInfo);
        var methodBuilder = new CffiApiMethodBuilder(classContext, classBodyBuilder);
        Logger.LogDebug(
            $"Adding class {className} to main.py with number of methods: {classInfo.Methods.Count}"
        );
        foreach (var method in classInfo.Methods)
        {
            Logger.LogDebug($"Adding method {method.OriginalName} to class {className} in main.py");
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

        //         if (
        //             classInfo.TryGetICollectionType(out var genericType)
        //             || classInfo.TryGetIReadonlyCollectionType(out genericType)
        //         )
        //         {
        //             var genericArg = PythonNamingUtils.MapTypeToPython(genericType);
        //             var exposedType = DotWrapUtils.GetExposedTypeFromCsType(
        //                 genericType,
        //                 out bool isOriginalType
        //             );
        //             var numpyType = PythonNamingUtils.MapTypeToNumpy(exposedType);
        //             var tolistMethodDef = $"def to_list(self) -> list[\"{genericArg}\"]:";
        //             classBodyBuilder.AppendLine(tolistMethodDef);
        //             genericClassBodyBuilder?.AppendLine(tolistMethodDef);
        //             genericClassBodyBuilder?.AppendLine("    pass");
        //             using var indent1 = classBodyBuilder.IndentUntilDispose();
        //             classBodyBuilder.AppendLine(
        //                 @$"
        // """"""
        // Converts the array data to a list of the specified dtype.
        // """"""
        // length = {Lib}.{classInfo.EntryPrefix}{GetCount}(self.{Ptr})
        // arr = np.empty(length, dtype={numpyType})

        // # get stable pointer to the array data
        // arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
        // {Lib}.{classInfo.EntryPrefix}{FillArr}(self.{Ptr}, arr_ptr, length)
        //         "
        //             );

        //             if (isOriginalType)
        //             {
        //                 classBodyBuilder.AppendLine("return arr.tolist()");
        //             }
        //             else
        //             {
        //                 OriginalAndExposedTypeInfo genericTypeInfo = new(
        //                     genericType,
        //                     isOriginalType ? null : exposedType
        //                 );

        //                 var (prefix, suffix) = CffiApiMethodBuilder.GetToPythonTransformation(
        //                     genericTypeInfo
        //                 );
        //                 classBodyBuilder.AppendLine("final_list = []");
        //                 using (
        //                     var forBlock = classBodyBuilder.AppendLineWithNewBlock(
        //                         "for i in range(length):"
        //                     )
        //                 )
        //                 {
        //                     if (numpyType == "np.intp")
        //                     {
        //                         classBodyBuilder.AppendLine($"val = {Ffi}.cast('void *', arr[i])");
        //                     }
        //                     else
        //                     {
        //                         classBodyBuilder.AppendLine($"val = arr[i]");
        //                     }
        //                     classBodyBuilder.AppendLine($"final_list.append({prefix}val{suffix})");
        //                 }
        //                 classBodyBuilder.AppendLine("return final_list");
        //             }
        //         }
    }

    /// <summary>
    /// Gets the applicable config objects for a type info object.
    /// </summary>
    /// <param name="configs"></param>
    /// <param name="typeInfo"></param>
    /// <returns></returns>
    public IEnumerable<(Type, DotWrapPythonTypeConfig)> GetApplicableConfigs(
        Dictionary<Type, DotWrapPythonTypeConfig> configs,
        ExportedTypeDefinition typeInfo
    )
    {
        var originalType =
            Type.GetType(typeInfo.AssemblyQualifiedName)
            ?? throw new InvalidOperationException(
                $"Could not find type {typeInfo.AssemblyQualifiedName} for class {typeInfo.TypeNameNoGenerics}."
            );

        foreach (var strongType in GetTypesThatCouldHaveConfigs(originalType))
        {
            if (configs.TryGetValue(strongType, out var config))
            {
                yield return (originalType, config);
            }

            if (strongType.IsGenericType)
            {
                var genericTypeDef = strongType.GetGenericTypeDefinition();
                if (configs.TryGetValue(genericTypeDef, out var genericConfig))
                {
                    yield return (originalType, genericConfig);
                }
            }
        }
    }

    private static IEnumerable<Type> GetTypesThatCouldHaveConfigs(Type originalType)
    {
        var baseType = originalType;
        while (baseType is not null && baseType != typeof(object))
        {
            yield return baseType;
            baseType = baseType.BaseType;
        }
        foreach (var strongType in originalType.GetInterfaces())
        {
            yield return strongType;
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
