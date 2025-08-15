using DotWrap.Configuration;
using DotWrap.Configuration.Python;
using DotWrap.MSBuild.WrapperGenerators.Python.Configs;
using DotWrap.Utils;
using static DotWrap.Utils.Python.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiWrapperBuilder(GlobalContext globalContext, CSharpProjectInfo projectInfo)
{
    public void BuildWrapper(
        IList<ExportedTypeDefinition> classes,
        IReadOnlyList<ExportedEnumInfo> enums
    )
    {
        var configTypes = globalContext
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DotWrapPythonTypeConfig)))
            .Select(t => (DotWrapPythonTypeConfig)Activator.CreateInstance(t)!)
            .Concat(DefaultConfigs.GetDefaultConfigs())
            .ToDictionary(t => t.TypeToConfigure);

        Dictionary<Type, DotWrapPythonTypeConfig> configTypesDict = new();
        foreach (var config in configTypes)
        {
            configTypesDict.TryAdd(config.Key, config.Value);
        }

        var allPythonGlobalConfigs = globalContext
            .Assembly.GetTypes()
            .Where(t => t.IsSubclassOf(typeof(DotWrapPythonGlobalConfig)))
            .Select(t => (DotWrapPythonGlobalConfig)Activator.CreateInstance(t)!)
            .ToList();

        if (allPythonGlobalConfigs.Count > 1)
        {
            throw new InvalidOperationException(
                $"Expected one or zero global Python configuration classes, but found {allPythonGlobalConfigs.Count}. Configuration classes found: {string.Join(", ", allPythonGlobalConfigs.Select(c => c.GetType().Name))}"
            );
        }
        var pythonGlobalConfig = allPythonGlobalConfigs.SingleOrDefault();

        PythonProjectInfo pythonProjectInfo = new(projectInfo, pythonGlobalConfig);
        PythonContext pythonContext = new(
            globalContext,
            pythonProjectInfo,
            new ModuleBuilder(pythonProjectInfo),
            configTypesDict,
            pythonGlobalConfig
        );

        CffiApiInteropBuilder interopBuilder = new(pythonProjectInfo);
        var pythonPackageRoot = pythonProjectInfo.PythonPackageRoot;
        var pythonProjectRoot = pythonProjectInfo.PythonProjectRoot;

        if (pythonGlobalConfig?.CreateSetupPy != false)
        {
            var setupPyContent = interopBuilder.CreateSetupPy();
            File.WriteAllText(Path.Combine(pythonProjectRoot, "setup.py"), setupPyContent);
        }

        // Create the __init__.py file if it doesn't exist
        if (!File.Exists(Path.Combine(pythonPackageRoot, "__init__.py")))
        {
            File.WriteAllText(
                Path.Combine(pythonPackageRoot, "__init__.py"),
                $"from .{PythonProjectInfo.DotWrapGeneratedDir}.{PythonProjectInfo.DotWrapExports} import *"
            );
        }

        var dotWrapRoot = pythonProjectInfo.DotWrapGeneratedRoot;
        var libName = pythonProjectInfo.CSharpProjectInfo.LibName;

        var (buildPyContent, headerContent) = interopBuilder.CreateBuildPyAndHeader(classes);
        File.WriteAllText(Path.Combine(dotWrapRoot, $"{libName}.h"), headerContent.ToString());
        File.WriteAllText(Path.Combine(dotWrapRoot, "lib_build.py"), buildPyContent.ToString());

        var mainPy = CreateMainPy(pythonContext);
        CffiApiClassBuilder classBuilder = new(pythonContext, pythonProjectInfo, mainPy);
        classBuilder.AddClassesToMainAndInitPy(classes);
        CffiApiEnumBuilder enumBuilder = new(pythonContext, mainPy);
        enumBuilder.AddClassesToMainAndInitPy(enums);

        foreach (var outParam in globalContext.OutParams)
        {
            // todo: less opinionated
            var initFileBuilder = pythonContext.ModuleBuilder.GetImportFile("OutTypes");
            OutParamWrapperBuilder.CreateOutParamWrapper(outParam, mainPy, initFileBuilder);
        }

        File.WriteAllText(Path.Combine(dotWrapRoot, "main.py"), mainPy.ToString());

        var initPy = pythonContext.ModuleBuilder.RootImportFile;
        File.WriteAllText(
            Path.Combine(dotWrapRoot, $"{PythonProjectInfo.DotWrapExports}.py"),
            initPy.ToString()
        );
        File.WriteAllText(Path.Combine(dotWrapRoot, $"__init__.py"), string.Empty);

        var modulesPath = Path.Combine(dotWrapRoot, "modules");
        Directory.CreateDirectory(modulesPath);
        File.WriteAllText(Path.Combine(modulesPath, "__init__.py"), string.Empty);
        foreach (var module in pythonContext.ModuleBuilder.Modules)
        {
            File.WriteAllText(
                Path.Combine(modulesPath, module.Key + ".py"),
                module.Value.ToString()
            );
        }
    }

    private IndentedPythonStringBuilder CreateMainPy(PythonContext pythonContext)
    {
        var pythonProjectInfo = pythonContext.ProjectInfo;
        var initPy = pythonContext.ModuleBuilder.RootImportFile;

        var projectName = pythonProjectInfo.ProjectName;
        var mainPy = new IndentedPythonStringBuilder();
        mainPy.AppendLine($"# This file is auto-generated by DotWrap. Do not edit manually.");
        mainPy.AppendLine($"from abc import abstractmethod");
        mainPy.AppendLine($"from enum import Enum");
        mainPy.AppendLine($"from typing import Any, Generic, TypeVar, Iterator");
        mainPy.AppendLine($"import asyncio");
        mainPy.AppendLine("import numpy as np");
        mainPy.AppendLine($"from ._{projectName} import lib as {Lib}");
        mainPy.AppendLine($"from ._{projectName} import ffi as {Ffi}");

        foreach (var config in pythonContext.Configs.Values)
        {
            config.ConfigureImports(mainPy);
        }

        var exceptionClass = pythonProjectInfo.CSharpProjectInfo.LibName.Replace(".", "") + "Error";
        initPy.AddTypeImport(exceptionClass);
        mainPy.AppendLine(
            @$"
class CString:
    """"""
    CString is a thin wrapper around a string pointer returned by the C# library.
    When this class is disposed, it will free the underlying C string memory.
    """"""
    def __init__(self, ptr):
        self.{Ptr} = {Ffi}.cast(""char *"", ptr)

    def __str__(self):
        return {Ffi}.string(self.{Ptr}).decode(""utf-8"")

    def __del__(self):
        {Lib}.DotWrap_BuiltIn_CString_Free(self.{Ptr})

class {exceptionClass}(Exception):
    def __init__(
        self,
        message: str,
        stack_trace: str = None,
        inner_exception_message: str = None,
        inner_exception_stack_trace: str = None,
    ):
        super().__init__(message)
        self.message = message
        self.stack_trace = stack_trace
        self.inner_exception_message = inner_exception_message
        self.inner_exception_stack_trace = inner_exception_stack_trace

    def __str__(self):
        return f""""""
An exception occurred in {pythonProjectInfo.ProjectName}:
Message: {{self.message}}
Stack Trace: {{self.stack_trace}}
Inner Exception Message: {{self.inner_exception_message}}
Inner Exception Stack Trace: {{self.inner_exception_stack_trace}}
        """"""

# create a method to raise exceptions from ExceptionInfo objects
def _raise_exception(exception_info):
    if exception_info.Message == _dotwrap_ffi.NULL:
        return

    message = str(CString(exception_info.Message))
    stack_trace = str(CString(exception_info.StackTrace))
    inner_exception_message = str(CString(exception_info.InnerExceptionMessage)) if exception_info.InnerExceptionMessage != _dotwrap_ffi.NULL else None
    inner_exception_stack_trace = str(CString(exception_info.InnerExceptionStackTrace)) if exception_info.InnerExceptionStackTrace != _dotwrap_ffi.NULL else None
    raise {exceptionClass}(message, stack_trace, inner_exception_message, inner_exception_stack_trace)
"
        );
        return mainPy;
    }
}
