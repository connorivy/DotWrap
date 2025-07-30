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
            var genericPart = className
                .Substring(startIndex + 1, endIndex - startIndex - 1)
                .Replace(", ", "And")
                .Replace(",", "And");
            className =
                className.Substring(0, startIndex)
                + $"Of{genericPart}"
                + className.Substring(endIndex + 1);
        }

        if (className.Length > 0 && char.IsLower(className[0]))
        {
            className = char.ToUpperInvariant(className[0]) + className.Substring(1);
        }

        return className;
    }

    public static string PythonizeTypeName(string typeName)
    {
        // split on first < and last >
        var startIndex = typeName.IndexOf('<');
        var endIndex = typeName.LastIndexOf('>');
        if (startIndex >= 0 && endIndex >= 0 && startIndex < endIndex)
        {
            var genericPart = MapTypeToPython(
                typeName.Substring(startIndex + 1, endIndex - startIndex - 1)
            );
            typeName =
                typeName.Substring(0, startIndex)
                + $"[{MapTypeToPython(genericPart)}]"
                + typeName.Substring(endIndex + 1);
        }

        var parts = typeName.Split(',').Select(p => p.Trim()).ToList();
        if (parts.Count > 1)
        {
            return string.Join(", ", parts.Select(MapTypeToPython));
        }

        return typeName;
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
            _ => PythonizeTypeName(t.Split('.').Last()),
        };
    }

    public static string MapTypeToNumpy(string t)
    {
        t = t.ToLowerInvariant().Replace("system.", "");
        return t switch
        {
            "byte" => "np.uint8",
            "sbyte" => "np.int8",
            "short" => "np.int16",
            "ushort" => "np.uint16",
            "int32" or "int" => "np.int32",
            "uint32" or "uint" => "np.uint32",
            "int64" or "long" => "np.int64",
            "uint64" or "ulong" => "np.uint64",
            "half" => "np.float16",
            "float" => "np.float32",
            "double" => "np.float64",
            _ => "np.object_",
            // _ => throw new NotSupportedException($"Unsupported type: {t}"),
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

    /// <summary>
    /// Converts a PascalCase or camelCase string to snake_case.
    /// </summary>
    /// <param name="input">The PascalCase or camelCase string.</param>
    /// <returns>The string in snake_case format.</returns>
    public static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        var result = string.Concat(
            input.Select(
                (c, i) =>
                    i > 0
                    && char.IsUpper(c)
                    && (
                        char.IsLower(input[i - 1])
                        || (i + 1 < input.Length && char.IsLower(input[i + 1]))
                    )
                    && input[i - 1] != '_'
                        ? "_" + char.ToLowerInvariant(c)
                        : char.ToLowerInvariant(c).ToString()
            )
        );
        return result;
    }
}
