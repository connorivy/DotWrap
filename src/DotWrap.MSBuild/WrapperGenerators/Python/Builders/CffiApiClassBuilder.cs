using System;
using System.Collections.Generic;
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
            if (genericClassName is not null)
            {
                if (this.classNames.Add(genericClassName))
                {
                    AddClassToMainAndInitPy(cls);
                }
                AddInheritedClassToMainAndInit(cls);
            }
            else
            {
                AddClassToMainAndInitPy(cls);
            }
        }
    }

    private void AddInheritedClassToMainAndInit(ExportedClassInfo cls)
    {
        string genericClassName =
            PythonUtils.GetGenericBaseNameOrNull(cls.ClassName)
            ?? throw new ArgumentException("Class name must be a generic type with '<' and '>'");
        string className = PythonUtils.PythonizeClassName(cls.ClassName);
        string genericArguments = PythonUtils.MapTypeToPython(
            cls.ClassName.Substring(
                cls.ClassName.IndexOf('<') + 1,
                cls.ClassName.LastIndexOf('>') - cls.ClassName.IndexOf('<') - 1
            )
        );

        mainPy.AppendLine($"class {className}({genericClassName}[{genericArguments}]):");
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
                .Append($"{Lib}.{classContext.ClassInfo.EntryPrefix}{Destroy}");
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
                methods = methods.Prepend($"{Ptr}").Append($"{PyDestroy}");
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

        //     if (classInfo.SpecialCaseFlags.HasFlag(ClassSpecialCaseFlags.ICollection))
        //     {
        //         mainPy.AppendLine(
        //             @$"
        // def to_list(self, dtype): -> list
        //     """"""
        //     Converts the array data to a list of the specified dtype.
        //     """"""
        //     length = {Lib}.self.{Ptr}ArrayInfo.Length
        //     arr = np.empty(length, dtype=np.int32)

        //     # get stable pointer to the array data
        //     arr_ptr = _dotwrap_ffi.cast(""int*"", _dotwrap_ffi.from_buffer(arr))
        //     _dotwrap_lib.DotWrap_TestLib_Hello_CopyArrayInfoToNumpyArray_27FFF55C(
        //         self.ArrayInfo, arr_ptr
        //     )
        //     return arr
        //     "
        //         );
        //     }
    }
}
