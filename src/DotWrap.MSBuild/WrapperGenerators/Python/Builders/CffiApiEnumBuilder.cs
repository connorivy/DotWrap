using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using DotWrap.Configuration;
using DotWrap.Utils;
using static DotWrap.Internal.Constants;
using static DotWrap.Utils.PythonConstants;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Builders;

internal class CffiApiEnumBuilder(
    PythonProjectInfo pythonProjectInfo,
    IndentedStringBuilder mainPy,
    IndentedStringBuilder initPy
)
{
    public void AddClassesToMainAndInitPy(IEnumerable<ExportedEnumInfo> enums)
    {
        foreach (var enumInfo in enums)
        {
            AddClassToMainAndInitPy(enumInfo);
        }
    }

    public void AddClassToMainAndInitPy(ExportedEnumInfo cls)
    {
        string className = PythonUtils.PythonizeClassName(cls.Name);
        initPy.AppendLine($"from .main import {className}");

        mainPy.AppendLine($"class {className}(Enum):");
        using var indent = mainPy.IndentUntilDispose();

        foreach (var kvp in cls.Options)
        {
            mainPy.AppendLine($"{PythonUtils.ToSnakeCase(kvp.Key)} = {kvp.Value}");
        }
    }
}
