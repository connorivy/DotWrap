using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.MSBuild.WrapperGenerators.Python.Extensions;
using static DotWrap.Internal.Constants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

public class CffiApiInteropBuilder(PythonProjectInfo pythonProjectInfo)
{
    public string CreateSetupPy()
    {
        var projectName = pythonProjectInfo.ProjectName;

        return @$"
from setuptools import setup, find_packages
from setuptools.command.build_py import build_py as build
import subprocess
import sys
import os


class CustomBuild(build):
    def run(self):
        # Run lib_build.py before building
        lib_build_path = os.path.join(
            os.path.dirname(__file__), ""{projectName}"", ""{PythonProjectInfo.DotWrapGeneratedDir}"", ""lib_build.py""
        )
        if os.path.exists(lib_build_path):
            subprocess.check_call([sys.executable, lib_build_path])
        else:
            print(f""lib_build.py not found at {{lib_build_path}}, skipping."")
        super().run()


setup(
    name=""{projectName}"",
    version=""0.1.0"",
    packages=find_packages(),
    install_requires=[""cffi""],
    author=""DotWrap"",
    description=""Auto-generated Python bindings for DotWrap C# library"",
    include_package_data=True,
    zip_safe=False,
    setup_requires=[""cffi""],
    package_data={{
        ""{projectName}.{PythonProjectInfo.DotWrapGeneratedDir}"": [
            ""*.dll"",
            ""*.so"",
            ""*.dylib"",
            ""*.pyd"",
            ""*.lib"",
            ""*.exp"",
            ""Release/*"",
        ]
    }},
    cmdclass={{
        ""build_py"": CustomBuild,
    }},
)

";
    }

    private const string CSelfPtrType = "void*";

    public (StringBuilder, StringBuilder) CreateBuildPyAndHeader(IList<ExportedClassInfo> classes)
    {
        var libName = pythonProjectInfo.CSharpProjectInfo.LibName;
        var projectName = pythonProjectInfo.ProjectName;

        StringBuilder headerContent = new();
        StringBuilder build = new();
        headerContent.AppendLine($"#ifndef DOTWRAP_{projectName}_H");
        headerContent.AppendLine($"#define DOTWRAP_{projectName}_H");

        build.AppendLine("from cffi import FFI");
        build.AppendLine("from typing import Any");
        build.AppendLine("import os");
        build.AppendLine();
        build.AppendLine("ffibuilder = FFI()");
        build.AppendLine("ffibuilder.cdef(\"\"\"");

        var freeString = "void DotWrap_BuiltIn_CString_Free(void* ptr);";
        build.AppendLine(freeString);
        headerContent.AppendLine(freeString);

        var arrayType =
            @"
typedef struct {
    void* Ptr;
    int Length;
} ArrayInfo;";
        build.AppendLine(arrayType);
        headerContent.AppendLine(arrayType);
        foreach (var cls in classes)
        {
            if (!cls.IsStatic)
            {
                var destroyMethod = $"void {cls.EntryPrefix}{Destroy}({CSelfPtrType} ptr);";
                build.AppendLine(destroyMethod);
                headerContent.AppendLine(destroyMethod);
            }
            foreach (var method in cls.Methods)
            {
                var parameters = method.Parameters.Select(p => $"{p.MapExposedTypeToC()} {p.Name}");
                if (!method.IsStatic)
                {
                    parameters = parameters.Prepend($"{CSelfPtrType} ptr");
                }
                var paramList = string.Join(", ", parameters);

                string entryName;
                if (method.OriginalName.StartsWith(InternalPrefix))
                {
                    entryName = $"{cls.EntryPrefix}{method.OriginalName}";
                }
                else
                {
                    entryName = $"{cls.EntryPrefix}{method.StampedName}";
                }
                var cDef = $"{method.MapExposedTypeToC()} {entryName}({paramList});";
                build.AppendLine(cDef);
                headerContent.AppendLine(cDef);
            }
            foreach (var prop in cls.Properties)
            {
                if (prop.HasGetter)
                {
                    var cDef =
                        $@"{prop.MapExposedTypeToC()} {cls.EntryPrefix}get_{prop.Name}({CSelfPtrType} ptr);";
                    build.AppendLine(cDef);
                    headerContent.AppendLine(cDef);
                }
                if (prop.HasSetter)
                {
                    var cDef =
                        $@"void {cls.EntryPrefix}set_{prop.Name}({CSelfPtrType} ptr, {prop.MapExposedTypeToC()} value);";
                    build.AppendLine(cDef);
                    headerContent.AppendLine(cDef);
                }
            }
        }
        build.Append("\"\"\")");
        build.AppendLine();

        build.AppendLine(
            $@"
current_dir = os.path.dirname(os.path.abspath(__file__))
ffibuilder.set_source(
    ""_{projectName}"",
    """"""
    #include ""{libName}.h""
    """""",
    libraries=[""{libName}""],
    library_dirs=[current_dir],
    include_dirs=[current_dir],
)

if __name__ == '__main__':
    ffibuilder.compile(verbose=True, tmpdir=current_dir)
"
        );
        headerContent.AppendLine($"#endif // DOTWRAP_{projectName}_H");
        return (build, headerContent);
    }
}
