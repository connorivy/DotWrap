using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using static DotWrap.Internal.Constants;

namespace DotWrap.MSBuild;

public static class PythonWrapperCffiAbiGenerator
{
    public static void GeneratePythonWrapper(
        string libFullPath,
        IEnumerable<ExportedClassInfo> classes
    )
    {
        var initPyContent = new StringBuilder();
        var libName = Path.GetFileNameWithoutExtension(libFullPath);
        string projectName = libName.Replace(".", "_");

        var sb = new StringBuilder();
        sb.AppendLine("from cffi import FFI");
        sb.AppendLine("from typing import Any");
        sb.AppendLine("import sys");
        sb.AppendLine("import importlib.resources");
        sb.AppendLine();
        sb.AppendLine("ffi = FFI()");
        sb.AppendLine("ffi.cdef(\"\"\"");
        foreach (var cls in classes)
        {
            sb.AppendLine($"int {cls.EntryPrefix}{Create}();");
            sb.AppendLine($"void {cls.EntryPrefix}{Destroy}(int ptr);");
            foreach (var method in cls.Methods)
            {
                var paramList = string.Join(
                    ", ",
                    method.Parameters.Select(p => $"{MapTypeToC(p.OriginalType)} {p.Name}")
                );
                var cDef =
                    $"{MapTypeToC(method.OriginalType)} {cls.EntryPrefix}{method.Name}(int ptr{(paramList.Length > 0 ? ", " : "")}{paramList});";
                sb.AppendLine(cDef);
            }
            foreach (var prop in cls.Properties)
            {
                if (prop.HasGetter)
                {
                    var cDef =
                        $"{MapTypeToC(prop.OriginalType)} {cls.EntryPrefix}get_{prop.Name}(int ptr);";
                    sb.AppendLine(cDef);
                }
                if (prop.HasSetter)
                {
                    var cDef =
                        $"void {cls.EntryPrefix}set_{prop.Name}(int ptr, {MapTypeToC(prop.OriginalType)} value);";
                    sb.AppendLine(cDef);
                }
            }
        }
        sb.AppendLine("\"\"\")");
        sb.AppendLine();
        sb.AppendLine(
            $@"
# Determine the correct shared library extension for the platform
if sys.platform.startswith(""win""):
    lib_name = ""{libName}.dll""
elif sys.platform.startswith(""darwin""):
    lib_name = ""{libName}.dylib""
else:
    lib_name = ""{libName}.so""

with importlib.resources.path(""{projectName}"", lib_name) as lib_path:
    lib = ffi.dlopen(str(lib_path))
        "
        );
        // sb.AppendLine($"lib = ffi.dlopen(r\"{dllName}\")  # Path to your DLL");
        sb.AppendLine();
        foreach (var cls in classes)
        {
            initPyContent.AppendLine($"from .main import {cls.ClassName}");
            sb.AppendLine($"class {cls.ClassName}:");
            sb.AppendLine("    def __init__(self):");
            sb.AppendLine($"        self._ptr = lib.{cls.EntryPrefix}{Create}()");
            foreach (var method in cls.Methods)
            {
                var paramListWithHints = string.Join(
                    ", ",
                    method.Parameters.Select(p =>
                        $"{p.Name}: {MapTypeToPython(p.OriginalType) ?? "Any"}"
                    )
                );
                var paramNames = string.Join(", ", method.Parameters.Select(p => p.Name));
                var pyReturnType = MapTypeToPython(method.OriginalType);
                sb.AppendLine(
                    $"    def {method.Name}(self{(paramListWithHints.Length > 0 ? ", " : "")}{paramListWithHints}){(pyReturnType != null ? $" -> {pyReturnType}" : "")}:"
                );
                var callArgs = paramNames.Length > 0 ? ", " + paramNames : "";
                sb.AppendLine(
                    $"        return lib.{cls.EntryPrefix}{method.Name}(self._ptr{callArgs})"
                );
            }
            foreach (var prop in cls.Properties)
            {
                if (prop.HasGetter)
                {
                    var pyPropType = MapTypeToPython(prop.OriginalType);
                    sb.AppendLine($"    @property");
                    sb.AppendLine(
                        $"    def {prop.Name}(self){(pyPropType != null ? $" -> {pyPropType}" : "")}:"
                    );
                    sb.AppendLine(
                        $"        return lib.{cls.EntryPrefix}get_{prop.Name}(self._ptr)"
                    );
                }
                if (prop.HasSetter)
                {
                    sb.AppendLine($"    @{prop.Name}.setter");
                    sb.AppendLine(
                        $"    def {prop.Name}(self, value: {MapTypeToPython(prop.OriginalType) ?? "Any"}):"
                    );
                    sb.AppendLine(
                        $"        lib.{cls.EntryPrefix}set_{prop.Name}(self._ptr, value)"
                    );
                }
            }
            sb.AppendLine("    def __del__(self):");
            sb.AppendLine($"        lib.{cls.EntryPrefix}{Destroy}(self._ptr)");
            sb.AppendLine();
        }

        // get directory before the /bin dir
        var projectDir = libFullPath.Split(["bin"], StringSplitOptions.None)[0];
        Directory.CreateDirectory(Path.Combine(projectDir, "python"));
        Directory.CreateDirectory(Path.Combine(projectDir, "python", projectName));
        File.WriteAllText(
            Path.Combine(projectDir, "python", projectName, "main.py"),
            sb.ToString()
        );
        File.WriteAllText(
            Path.Combine(projectDir, "python", projectName, "__init__.py"),
            initPyContent.ToString()
        );
        File.Copy(
            libFullPath,
            Path.Combine(projectDir, "python", projectName, Path.GetFileName(libFullPath)),
            true
        );

        // Generate setup.py for the Python package
        var setupPyContent =
            @$"
from setuptools import setup, find_packages

setup(
    name='{projectName}',
    version='0.1.0',
    packages=find_packages(),
    install_requires=['cffi'],
    author='DotWrap',
    description='Auto-generated Python bindings for DotWrap C# library',
    include_package_data=True,
    zip_safe=False,
    package_data={{'{projectName}': ['*.dll', '*.so', '*.dylib']}},
)
";
        File.WriteAllText(Path.Combine(projectDir, "python", "setup.py"), setupPyContent);
    }

    private static string MapTypeToC(string type)
    {
        var t = type.ToLowerInvariant().Replace("system.", "");
        return t switch
        {
            "int32" or "int" => "int",
            "void" => "void",
            "float" => "float",
            "double" => "double",
            "boolean" or "bool" => "bool",
            "intptr" => "void*",
            _ => "void*", // fallback for unsupported types
        };
    }

    private static string? MapTypeToPython(string type)
    {
        var t = type.ToLowerInvariant().Replace("system.", "");
        return t switch
        {
            "int32" or "int" => "int",
            "float" or "double" => "float",
            "boolean" or "bool" => "bool",
            "void" => "None",
            // _ => throw new NotSupportedException($"Unsupported type: {type}"),
            _ => null, // return null for unsupported types
        };
    }

    public static void CreatePyPackageDirStructure(string outputPath)
    {
        var dir = Path.GetDirectoryName(outputPath);
        if (dir == null)
        {
            throw new ArgumentException("Invalid output path.");
        }

        // Create the directory if it doesn't exist
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Create an empty __init__.py file to make it a package
        var initFilePath = Path.Combine(dir, "__init__.py");
        if (!File.Exists(initFilePath))
        {
            File.WriteAllText(initFilePath, string.Empty);
        }
    }
}
