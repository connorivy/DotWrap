using System;
using System.Linq;

namespace DotWrap.MSBuild.WrapperGenerators.Python;

public static class PythonUtils
{
    /// <summary>
    /// Converts c# class name to a Pythonic class name in a few different ways.
    /// - Changes List<int> to ListOfInt
    /// </summary>
    /// <param name="className"></param>
    /// <returns></returns>
    public static string PythonizeClassName(string className)
    {
        while (className.Contains('<'))
        {
            // split on first < and last >
            var startIndex = className.IndexOf('<');
            var endIndex = className.LastIndexOf('>');
            if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
            {
                break; // no valid generic type found
            }
            var genericPart = className.Substring(startIndex + 1, endIndex - startIndex - 1);
            className =
                className.Substring(0, startIndex)
                + $"Of{genericPart}"
                + className.Substring(endIndex + 1);
        }

        return className;
    }

    public static string? GetGenericBaseNameOrNull(string className)
    {
        // split on first < and last >
        var startIndex = className.IndexOf('<');
        var endIndex = className.LastIndexOf('>');
        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            return null; // no valid generic type found
        }
        var genericPart = className.Substring(startIndex + 1, endIndex - startIndex - 1);
        return className.Substring(0, startIndex);
    }

    public static string MapTypeToPython(string t)
    {
        return t switch
        {
            "sbyte"
            or "byte"
            or "short"
            or "ushort"
            or "int32"
            or "int"
            or "uint32"
            or "uint"
            or "int64"
            or "long"
            or "uint64"
            or "ulong" => "int",
            "float" or "double" => "float",
            "boolean" or "bool" => "bool",
            "void" => "None",
            "string" => "str",
            "int[]" => "list[int]",
            _ => $"\"{PythonUtils.PythonizeClassName(t.Split('.').Last())}\"",
        };
    }

    public static string MapTypeToC(string t)
    {
        t = t.ToLowerInvariant().Replace("system.", "");
        return t switch
        {
            "byte" => "uint8_t",
            "sbyte" => "int8_t",
            "short" => "int16_t",
            "ushort" => "uint16_t",
            "int32" or "int" => "int32_t",
            "uint32" or "uint" => "uint32_t",
            "int64" or "long" => "int64_t",
            "uint64" or "ulong" => "uint64_t",
            "char" => "char",
            "void" => "void",
            "half" => "float",
            "float" => "float",
            "double" => "double",
            "intptr" => "void*",
            "arrayinfo" => "ArrayInfo",
            _ => throw new NotSupportedException($"Unsupported type: {t}"),
        };
    }
}
