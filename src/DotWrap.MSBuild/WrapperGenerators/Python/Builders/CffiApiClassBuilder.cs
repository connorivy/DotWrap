using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using DotWrap.Internal;
using static DotWrap.Internal.Constants;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

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
            string? genericClassName = PythonUtils.GetGenericBaseNameOrNull(cls.TypeName);
            if (genericClassName is not null && this.classNames.Add(genericClassName))
            {
                this.AddInheritedClassToMainAndInit(cls);
            }
            AddClassToMainAndInitPy(cls);
        }
    }

    private void AddInheritedClassToMainAndInit(ExportedTypeDefinitionInfo cls)
    {
        string genericClassName =
            PythonUtils.GetGenericBaseNameOrNull(cls.TypeName)
            ?? throw new ArgumentException("Class name must be a generic type with '<' and '>'");
        string className = PythonUtils.PythonizeClassName(cls.TypeName);
        var genericParams = cls.GenericTypeArgumentsToParameters.Select(kvp => kvp.Value).ToList();

        foreach (var param in genericParams)
        {
            mainPy.AppendLine($"{param} = TypeVar('{param}')");
        }
        mainPy.AppendLine(
            $"class {genericClassName}(Generic[{string.Join(", ", genericParams)}]):"
        );
        using var _ = mainPy.IndentUntilDispose();

        if (!string.IsNullOrWhiteSpace(cls.SummaryComment))
        {
            mainPy.AppendLine(
                @$"    
""""""
{cls.SummaryComment}
"""""""
            );
        }

        var classContext = new ClassBuilderContext(globalContext, pythonProjectInfo, cls);
        IndentedCSharpStringBuilder dummy = new();
        var methodNames = new HashSet<string>();
        var methodBuilder = new CffiApiMethodBuilder(classContext, dummy);
        foreach (var method in cls.Methods)
        {
            if (method.OriginalName.StartsWith(InternalPrefix))
            {
                continue;
            }

            var context = new MethodBuilderContext(classContext, method);
            methodBuilder.AddClassToMainAndInitPy(method);
            var pyReturnType =
                context.MethodInfo.GenericTypeName
                ?? context.GetReturnType(cls.GenericTypeArgumentsToParameters);
            var methodName = context.GetMethodName(methodNames);
            var paramListWithHints = context.PythonMethodGenericParamListWithHints();
            if (method.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertyGetter))
            {
                methodName = methodName["get_".Length..];
                mainPy.AppendLine($"@property");
            }
            else if (method.SpecialCaseFlags.HasFlag(MethodSpecialCaseFlags.PropertySetter))
            {
                methodName = methodName["set_".Length..];
                mainPy.AppendLine($"@{methodName}.setter");
            }

            mainPy.AppendLine(
                @$"    
@abstractmethod
def {methodName}({paramListWithHints}){$" -> {pyReturnType}"}:
    pass
    "
            );
        }

        if (
            cls.TryGetICollectionType(out var genericType)
            || cls.TryGetIReadonlyCollectionType(out genericType)
        )
        {
            var genericParam = PythonUtils.MapTypeToPython(
                genericType,
                cls.GenericTypeArgumentsToParameters
            );
            mainPy.AppendLine(
                @$"
def to_list(self) -> list[""{genericParam}""]:
    pass
        "
            );
        }

        mainPy.AppendLine(
            @$"    
@abstractmethod
def __del__(self) -> None:
    pass
    "
        );
    }

    public void AddClassToMainAndInitPy(ExportedTypeDefinitionInfo classInfo)
    {
        var baseClassName = PythonUtils.GetGenericBaseNameOrNull(classInfo.TypeName);
        string className = PythonUtils.PythonizeClassName(classInfo.TypeName);

        initPy.AppendLine($"from .main import {className}");

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

        mainPy.AppendLine($"class {className}{genericDef}:");
        using var _ = mainPy.IndentUntilDispose();

        if (!string.IsNullOrWhiteSpace(classInfo.SummaryComment))
        {
            mainPy.AppendLine(
                @$"    
""""""
{classInfo.SummaryComment}
"""""""
            );
        }

        var classContext = new ClassBuilderContext(globalContext, pythonProjectInfo, classInfo);
        var methodBuilder = new CffiApiMethodBuilder(classContext, mainPy);
        foreach (var method in classInfo.Methods)
        {
            if (method.OriginalName.StartsWith(InternalPrefix))
            {
                continue;
            }
            methodBuilder.AddClassToMainAndInitPy(method);
        }

        if (!classInfo.SpecialCaseFlags.HasFlag(TypeSpecialCaseFlags.Static))
        {
            mainPy.AppendLine(
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
            mainPy.AppendLine($"def to_list(self) -> list[\"{genericArg}\"]:");
            using var indent1 = mainPy.IndentUntilDispose();
            mainPy.AppendLine(
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
                mainPy.AppendLine("return arr.tolist()");
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
                mainPy.AppendLine("final_list = []");
                using (var forBlock = mainPy.AppendLineWithNewBlock("for i in range(length):"))
                {
                    if (numpyType == "np.intp")
                    {
                        mainPy.AppendLine($"val = {Ffi}.cast('void *', arr[i])");
                    }
                    else
                    {
                        mainPy.AppendLine($"val = arr[i]");
                    }
                    mainPy.AppendLine($"final_list.append({prefix}val{suffix})");
                }
                mainPy.AppendLine("return final_list");
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
