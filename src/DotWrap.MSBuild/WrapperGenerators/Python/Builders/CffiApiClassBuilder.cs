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
            // string? genericClassName = PythonUtils.GetGenericBaseNameOrNull(cls.ClassName);
            // if (genericClassName is not null)
            // {
            //     if (this.classNames.Add(genericClassName))
            //     {
            //         AddClassToMainAndInitPy(cls);
            //     }
            //     AddInheritedClassToMainAndInit(cls);
            // }
            // else
            // {
            //     AddClassToMainAndInitPy(cls);
            // }
            AddClassToMainAndInitPy(cls);
        }
    }

    private void AddInheritedClassToMainAndInit(ExportedClassInfo cls)
    {
        string genericClassName =
            PythonUtils.GetGenericBaseNameOrNull(cls.ClassName)
            ?? throw new ArgumentException("Class name must be a generic type with '<' and '>'");
        string className = PythonUtils.PythonizeClassName(cls.ClassName);
        var genericArguments = cls
            .GenericTypeParametersToArguments.Select(kvp => PythonUtils.MapTypeToPython(kvp.Value))
            .ToList();

        mainPy.AppendLine(
            $"class {className}({genericClassName}[{string.Join(", ", genericArguments)}]):"
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
        var methodBuilder = new CffiApiMethodBuilder(classContext, dummy);
        foreach (var method in cls.Methods)
        {
            methodBuilder.AddClassToMainAndInitPy(method);
        }

        var methodNames = cls.Methods.Select(m =>
            Lib
            + "."
            + classContext.ClassInfo.EntryPrefix
            + new MethodBuilderContext(classContext, m).MethodInfo.StampedName
        );

        if (!cls.IsStatic)
        {
            methodNames = methodNames
                .Prepend($"{Ptr}")
                .Append($"{Lib}.{classContext.ClassInfo.EntryPrefix}{GetCount}")
                .Append($"{Lib}.{classContext.ClassInfo.EntryPrefix}{FillArr}")
                .Append($"{Lib}.{classContext.ClassInfo.EntryPrefix}{Destroy}");
        }

        foreach (var genericArg in cls.GenericTypeParametersToArguments)
        {
            mainPy.AppendLine(
                @$"    
    def {GenericTypeNpType}_{genericArg.Key}():
        return {PythonUtils.MapTypeToNumpy(genericArg.Value)} 
            "
            );
        }

        mainPy.AppendLine($"    def __init__(self, {Ptr}):");
        mainPy.AppendLine($"        super().__init__(");
        foreach (var method in methodNames)
        {
            mainPy.AppendLine($"            {method},");
        }
        mainPy.AppendLine($"        )");
        mainPy.AppendLine();
        mainPy.AppendLine($"    @classmethod");
        mainPy.AppendLine($"    def {FromPtr}(cls, ptr: int):");
        mainPy.AppendLine($"        return cls(ptr)");
        mainPy.AppendLine();
    }

    public void AddClassToMainAndInitPy(ExportedClassInfo classInfo)
    {
        string className =
            PythonUtils.GetGenericBaseNameOrNull(classInfo.ClassName)
            ?? PythonUtils.PythonizeClassName(classInfo.ClassName);

        initPy.AppendLine($"from .main import {className}");

        var isGeneric = classInfo.ClassName.Contains('<');
        foreach (var kvp in classInfo.GenericTypeParametersToArguments)
        {
            mainPy.AppendLine($"{kvp.Key} = TypeVar('{kvp.Key}')");
        }
        var genericDef = string.Join(
            ", ",
            classInfo.GenericTypeParametersToArguments.Select(kvp => kvp.Key)
        );
        if (!string.IsNullOrEmpty(genericDef))
        {
            genericDef = $"(Generic[{genericDef}])";
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
            methodBuilder.AddClassToMainAndInitPy(method);
        }

        if (classContext.IsGeneric)
        {
            var methods = methodBuilder.MethodNames;
            if (!classInfo.IsStatic)
            {
                methods = methods.Prepend($"{Ptr}").Append($"{PyFillArr}").Append($"{PyDestroy}");
            }
            var ctorArgs = methods.Prepend("self");
            mainPy.AppendLine($"    def __init__({string.Join(", ", ctorArgs)}):");
            foreach (var methodName in methods)
            {
                var methodNameToAdd = methodName.StartsWith(InternalPythonPrefix)
                    ? methodName
                    : $"{InternalPythonPrefix}{methodName}";

                mainPy.AppendLine($"        self.{methodNameToAdd} = {methodName}");
            }
            mainPy.AppendLine();

            foreach (var genericArg in classInfo.GenericTypeParametersToArguments.Keys)
            {
                mainPy.AppendLine(
                    @$"    
    @abstractmethod
    def {GenericTypeNpType}_{genericArg}():
        pass 
            "
                );
            }
        }

        if (!classInfo.IsStatic)
        {
            if (classContext.IsGeneric)
            {
                mainPy.AppendLine("    def __del__(self):");
                mainPy.AppendLine($"        self.{PyDestroy}(self.{Ptr})");
                mainPy.AppendLine();
            }
            else
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
        }

        if (
            classInfo
                .Interfaces.FindAll(i => i.StartsWith("System.Collections.Generic.ICollection"))
                .FirstOrDefault()
            is string iCollection
        )
        {
            string genericType = PythonUtils.PythonizeTypeName(
                iCollection.Substring(
                    iCollection.IndexOf('<') + 1,
                    iCollection.LastIndexOf('>') - iCollection.IndexOf('<') - 1
                )
            );
            var genericArg = classInfo
                .GenericTypeParametersToArguments.FirstOrDefault(kvp => kvp.Value == genericType)
                .Key;
            mainPy.AppendLine(
                @$"
    def to_list(self) -> list[{genericArg}]:
        """"""
        Converts the array data to a list of the specified dtype.
        """"""
        length = {Lib}.{classInfo.EntryPrefix}{GetCount}()
        np_type = self.{GenericTypeNpType}_{genericArg}()
        arr = np.empty(length, dtype=np_type)

        # get stable pointer to the array data
        arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
        self.{PyFillArr}(self.{Ptr}, arr_ptr, length)

        return arr.tolist()
        "
            );
        }
    }
}
