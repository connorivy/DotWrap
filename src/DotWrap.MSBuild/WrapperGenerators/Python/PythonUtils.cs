using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.Internal;

namespace DotWrap.MSBuild.WrapperGenerators.Python;

public static class PythonUtils
{
    /// <summary>
    /// Cases to handle:
    /// - System.Collections.Generic.List<int> -> ListOfint
    /// - System.Collections.Generic.List<List<int>> -> ListOfListOfint
    /// - System.Collections.Generic.Dictionary<string, int>.KeyCollection -> KeyCollectionOfstrAndint
    /// - System.Collections.Generic.List<(string, List<(string, int)>)> -> ListOfTupleOfstrAndListOfTupleOfstrAndint
    /// </summary>
    /// <param name="fullTypeName"></param>
    /// <returns></returns>
    public static string PythonizeClassName(string fullTypeName)
    {
        fullTypeName = DotWrapUtils.ReplaceArraySymbols(fullTypeName);
        // Handle generic types in the original type
        // e.g., typeName = "System.Collections.Generic.Dictionary<string, int>.KeyCollection"
        var innerGenerics = fullTypeName
            .Split('.')
            .SelectMany(GetTopLevelGenerics)
            .Select(PythonizeClassName)
            .ToList();

        var typeName = fullTypeName.Split('.').Last();
        var nonGenericTypeName = GetGenericBaseNameOrNull(typeName) ?? typeName;

        return nonGenericTypeName
            + (innerGenerics.Count > 0 ? $"Of{string.Join("And", innerGenerics)}" : "");
    }

    /// <summary>
    /// Cases to handle:
    /// - System.Collections.Generic.List<int> -> List[int]
    /// - System.Collections.Generic.List<List<int>> -> List[List[int]]
    /// - System.Collections.Generic.Dictionary<string, int>.KeyCollection -> KeyCollection[str, int]
    /// - System.Collections.Generic.List<(string, List<(string, int)>)> -> List[Tuple[str, List[Tuple[str, int]]]]
    /// </summary>
    /// <param name="fullTypeName"></param>
    /// <returns></returns>
    public static string PythonizeTypeName(
        string fullTypeName,
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        fullTypeName = DotWrapUtils.ReplaceArraySymbols(fullTypeName);

        // Handle generic types in the original type
        // e.g., typeName = "System.Collections.Generic.Dictionary<string, int>.KeyCollection"
        var innerGenerics = fullTypeName
            .Split('.')
            .SelectMany(GetTopLevelGenerics)
            .Select(g => MapTypeToPython(g, genericParamsToArgsDict))
            .ToList();

        var typeName = fullTypeName.Split('.').Last();
        var nonGenericTypeName = GetGenericBaseNameOrNull(typeName) ?? typeName;

        return nonGenericTypeName
            + (innerGenerics.Count > 0 ? $"[{string.Join(", ", innerGenerics)}]" : "");
    }

    public static IEnumerable<string> GetTopLevelGenerics(string input)
    {
        var startIndex = input.IndexOf('<');
        var endIndex = input.LastIndexOf('>');
        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            yield break;
        }

        var genericString = input.Substring(startIndex + 1, endIndex - startIndex - 1);
        foreach (var genericType in SplitGenericStringIntoTopLevelList(genericString))
        {
            yield return genericType;
        }
    }

    /// <summary>
    /// Splits a generic argument string into its top-level arguments, handling nested generics.
    /// For example:
    ///   "int, string"                => ["int", "string"]
    ///   "List<int>, string"          => ["List<int>", "string"]
    ///   "Dictionary<string, int>"    => ["Dictionary<string, int>"]
    ///   "List<Dictionary<int, str>>" => ["List<Dictionary<int, str>>"]
    ///   "string, List<int>, Tuple<int, List<string>>"
    ///                                => ["string", "List<int>", "Tuple<int, List<string>>"]
    /// </summary>
    /// <param name="input">The generic argument string (e.g., "int, List<string>")</param>
    /// <returns>List of top-level argument strings</returns>
    static IEnumerable<string> SplitGenericStringIntoTopLevelList(string input)
    {
        int depth = 0;
        int lastPos = 0;
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '<')
                depth++;
            else if (input[i] == '>')
                depth--;
            else if (input[i] == ',' && depth == 0)
            {
                yield return input.Substring(lastPos, i - lastPos).Trim();
                lastPos = i + 1;
            }
        }
        if (lastPos < input.Length)
            yield return input.Substring(lastPos).Trim();
    }

    public static IEnumerable<string> GetPythonGenericTypes(string typeName)
    {
        var startIndex = typeName.IndexOf('<');
        var endIndex = typeName.LastIndexOf('>');
        while (startIndex >= 0 && endIndex >= 0 && startIndex < endIndex)
        {
            var genericString = typeName.Substring(startIndex + 1, endIndex - startIndex - 1);
            foreach (var genericType in genericString.Split(',').Select(t => t.Trim()))
            {
                yield return MapTypeToPython(genericType);
            }
        }
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

    public static string MapTypeToPython(
        string t,
        IDictionary<string, string>? genericParamsToArgsDict = null
    )
    {
        if (genericParamsToArgsDict?.TryGetValue(t, out var mappedType) == true)
        {
            return mappedType;
        }
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
            _ => PythonizeTypeName(t, genericParamsToArgsDict),
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
            _ => "np.intp",
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

        var result = new StringBuilder();
        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (i > 0)
            {
                // Underscore before uppercase (existing logic)
                if (
                    char.IsUpper(c)
                    && (
                        char.IsLower(input[i - 1])
                        || (i + 1 < input.Length && char.IsLower(input[i + 1]))
                    )
                    && input[i - 1] != '_'
                )
                {
                    result.Append('_');
                }
                // Underscore before digit if previous is a letter and not already underscored
                else if (char.IsDigit(c) && char.IsLetter(input[i - 1]) && input[i - 1] != '_')
                {
                    result.Append('_');
                }
            }

            result.Append(char.ToLowerInvariant(c));
        }
        return result.ToString();
    }
}
