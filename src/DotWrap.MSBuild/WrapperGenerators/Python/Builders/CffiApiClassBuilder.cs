using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using static DotWrap.Internal.Constants;
using static DotWrap.MSBuild.WrapperGenerators.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiClassBuilder(
    PythonProjectInfo pythonProjectInfo,
    StringBuilder mainPy,
    StringBuilder initPy
)
{
    private HashSet<string> classNames = new();

    public void AddClassesToMainAndInitPy(IEnumerable<ExportedClassInfo> classes)
    {
        foreach (var cls in classes)
        {
            string? genericClassName = PythonUtils.GetGenericBaseNameOrNull(cls.ClassName);
            if (genericClassName is not null && this.classNames.Add(genericClassName))
            {
                this.AddInheritedClassToMainAndInit(cls);
            }
            AddClassToMainAndInitPy(cls);
        }
    }

    private void AddInheritedClassToMainAndInit(ExportedClassInfo cls)
    {
        string genericClassName =
            PythonUtils.GetGenericBaseNameOrNull(cls.ClassName)
            ?? throw new ArgumentException("Class name must be a generic type with '<' and '>'");
        string className = PythonUtils.PythonizeClassName(cls.ClassName);
        var genericParams = cls.GenericTypeParametersToArguments.Select(kvp => kvp.Key).ToList();

        foreach (var kvp in cls.GenericTypeParametersToArguments)
        {
            mainPy.AppendLine($"{kvp.Key} = TypeVar('{kvp.Key}')");
        }
        mainPy.AppendLine(
            $"class {genericClassName}(Generic[{string.Join(", ", genericParams)}]):"
        );
        if (!string.IsNullOrWhiteSpace(cls.SummaryComment))
        {
            mainPy.AppendLine(
                @$"    
    """"""
    {cls.SummaryComment}
    """""""
            );
        }

        var classContext = new ClassBuilderContext(pythonProjectInfo, cls);
        StringBuilder dummy = new();
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
            var pyReturnType = context.MethodInfo.GenericTypeName ?? context.GetReturnType();
            var methodName = context.GetMethodName(methodNames);
            var paramListWithHints = context.PythonMethodGenericParamListWithHints();

            mainPy.AppendLine(
                @$"    
    @abstractmethod
    def {methodName}({paramListWithHints}){$" -> {pyReturnType}"}:
        pass
    "
            );
        }

        if (cls.TryGetICollectionType(out var genericType))
        {
            var genericArg = PythonUtils.MapTypeToPython(genericType);
            var genericParam =
                cls.GenericTypeParametersToArguments.FirstOrDefault(kvp =>
                    kvp.Value == genericType
                ).Key
                ?? throw new InvalidOperationException(
                    "Generic type parameter not found in class generic type parameters"
                );
            mainPy.AppendLine(
                @$"
    def to_list(self) -> list[{genericParam}]:
        pass
        "
            );
        }
    }

    public void AddClassToMainAndInitPy(ExportedClassInfo classInfo)
    {
        var baseClassName = PythonUtils.GetGenericBaseNameOrNull(classInfo.ClassName);
        string className = PythonUtils.PythonizeClassName(classInfo.ClassName);

        initPy.AppendLine($"from .main import {className}");

        var genericDef = string.Join(
            ", ",
            classInfo.GenericTypeParametersToArguments.Select(kvp =>
                PythonUtils.MapTypeToPython(kvp.Value)
            )
        );
        if (!string.IsNullOrEmpty(genericDef))
        {
            genericDef = $"({baseClassName}[{genericDef}])";
        }

        mainPy.AppendLine($"class {className}{genericDef}:");

        if (!string.IsNullOrWhiteSpace(classInfo.SummaryComment))
        {
            mainPy.AppendLine(
                @$"    
    """"""
    {classInfo.SummaryComment}
    """""""
            );
        }

        var classContext = new ClassBuilderContext(pythonProjectInfo, classInfo);
        var methodBuilder = new CffiApiMethodBuilder(classContext, mainPy);
        foreach (var method in classInfo.Methods)
        {
            if (method.OriginalName.StartsWith(InternalPrefix))
            {
                continue;
            }
            methodBuilder.AddClassToMainAndInitPy(method);
        }

        if (!classInfo.IsStatic)
        {
            mainPy.AppendLine($"    @classmethod");
            mainPy.AppendLine($"    def {FromPtr}(cls, ptr: int):");
            mainPy.AppendLine($"        instance = object.__new__(cls)");
            mainPy.AppendLine($"        instance.{Ptr} = ptr");
            mainPy.AppendLine($"        return instance");
            mainPy.AppendLine();
            mainPy.AppendLine("    def __del__(self):");
            mainPy.AppendLine($"        {Lib}.{classInfo.EntryPrefix}{Destroy}(self.{Ptr})");
            mainPy.AppendLine();
        }

        if (classInfo.TryGetICollectionType(out var genericType))
        {
            var genericArg = PythonUtils.MapTypeToPython(genericType);
            mainPy.AppendLine(
                @$"
    def to_list(self) -> list[{genericArg}]:
        """"""
        Converts the array data to a list of the specified dtype.
        """"""
        length = {Lib}.{classInfo.EntryPrefix}{GetCount}_0DE6EC57(self.{Ptr})
        arr = np.empty(length, dtype={PythonUtils.MapTypeToNumpy(genericType)})

        # get stable pointer to the array data
        arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
        {Lib}.{classInfo.EntryPrefix}{PyFillArr}_76C67BC3(self.{Ptr}, arr_ptr, length)

        return arr.tolist()
        "
            );
        }
    }
}
