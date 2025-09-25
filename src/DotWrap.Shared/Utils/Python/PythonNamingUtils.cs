using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DotWrap.Configuration;

namespace DotWrap.Utils.Python;

public static class PythonNamingUtils
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

        var topLevelSplit = SplitOnPeriodTopLevel(fullTypeName).ToList();

        var innerGenerics = topLevelSplit
            .SelectMany(GetTopLevelGenerics)
            .Concat(GetTopLevelGenericsWithBrackets(topLevelSplit.Last()))
            .Select(DotWrapUtils.NormalizeCsTypeName)
            .Select(PythonizeClassName)
            .ToList();

        var typeName = topLevelSplit.Last();
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
        IDictionary<string, string>? genericArgsToParamsDict = null,
        bool useGenericParams = false
    )
    {
        fullTypeName = DotWrapUtils.ReplaceArraySymbols(fullTypeName);

        var topLevelSplit = SplitOnPeriodTopLevel(fullTypeName).ToList();

        var innerGenerics = topLevelSplit
            .SelectMany(GetTopLevelGenerics)
            .Concat(GetTopLevelGenericsWithBrackets(topLevelSplit.Last()))
            .Select(DotWrapUtils.NormalizeCsTypeName)
            .Select(g => MapTypeToPython(g, genericArgsToParamsDict, useGenericParams))
            .ToList();

        var typeName = topLevelSplit.Last();
        var nonGenericTypeName = GetGenericBaseNameOrNull(typeName) ?? typeName;

        return nonGenericTypeName
            + (innerGenerics.Count > 0 ? $"[{string.Join(", ", innerGenerics)}]" : "");
    }

    /// <summary>
    /// Splits a string on periods, but only at the top level meaning that periods within generic type arguments are ignored.
    /// For example:
    ///   "System.Collections.Generic.List<System.Int32>" -> ["System", "Collections", "Generic", "List<System.Int32>"]
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    private static IEnumerable<string> SplitOnPeriodTopLevel(string input)
    {
        int splitStartIndex = 0;
        int numOpenBrackets = 0;
        var doubleOpenBracketIndex = input.IndexOf("[[");
        for (int i = 0; i < input.Length; i++)
        {
            if (doubleOpenBracketIndex > -1 && i >= doubleOpenBracketIndex)
            {
                break;
            }
            if (input[i] == '<')
            {
                numOpenBrackets++;
            }
            else if (input[i] == '>')
            {
                numOpenBrackets--;
            }
            else if (numOpenBrackets == 0 && input[i] == '.')
            {
                yield return input.Substring(splitStartIndex, i - splitStartIndex);
                splitStartIndex = i + 1;
            }
        }
        yield return input.Substring(splitStartIndex);
    }

    private static IEnumerable<string> GetTopLevelGenerics(string input)
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

    private static IEnumerable<string> GetTopLevelGenericsWithBrackets(string input)
    {
        var startIndex = input.IndexOf("[[");
        var endIndex = input.LastIndexOf("]]");
        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            yield break;
        }

        var genericString = input.Substring(startIndex + 2, endIndex - startIndex - 2);
        foreach (var genericType in SplitGenericStringWithBracketsIntoTopLevelList(genericString))
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
        var doubleOpenBracketIndex = input.IndexOf("[[");
        for (int i = 0; i < input.Length; i++)
        {
            if (doubleOpenBracketIndex > -1 && i >= doubleOpenBracketIndex)
            {
                break;
            }
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

    /// <summary>
    /// Splits a generic argument string into its top-level arguments, handling nested generics.
    /// For example:
    ///   "int, string"                => ["int", "string"]
    ///   "List[[System.Int32], [System.String]]"          => ["List[[System.Int32]]", "string"]
    ///   "Dictionary[[System.String, System.Int32]]"    => ["Dictionary[[System.String], System.Int32]]"]
    ///   "List[[Dictionary[[int, str]]]]" => ["List[[Dictionary[[int],[str]]]]"]
    ///   "string, List[[int]], Tuple[[int],[List[[string]]]]" => ["string", "List[[int]]", "Tuple[[int],[List[[string]]]]"]
    /// </summary>
    /// <param name="input">The generic argument string (e.g., "int, List<string>")</param>
    /// <returns>List of top-level argument strings</returns>
    static IEnumerable<string> SplitGenericStringWithBracketsIntoTopLevelList(string input)
    {
        int depth = 0;
        int lastPos = 0;
        for (int i = 0; i < input.Length - 2; i++)
        {
            var nextTwoChars = input.AsSpan(i, 2);
            if (nextTwoChars == "[[")
            {
                depth++;
                continue;
            }
            else if (nextTwoChars == "]]")
            {
                depth--;
                continue;
            }
            var nextThreeChars = input.AsSpan(i, 3);
            if (depth == 0 && nextThreeChars.SequenceEqual("],["))
            {
                yield return input.Substring(lastPos, i - lastPos).Trim();
                lastPos = i + 3;
            }
        }
        if (lastPos < input.Length)
        {
            yield return input.Substring(lastPos).Trim();
        }
    }

    public static string? GetGenericBaseNameOrNull(string className)
    {
        className = SplitOnPeriodTopLevel(className).LastOrDefault() ?? className;
        // split on first < and last >
        var startIndex = className.IndexOf('<');
        var endIndex = className.LastIndexOf('>');
        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            startIndex = className.IndexOf("[[");
            endIndex = className.LastIndexOf("]]");
        }
        if (startIndex < 0 || endIndex < 0 || startIndex >= endIndex)
        {
            return null; // no valid generic type found
        }
        return className.Substring(0, startIndex);
    }

    public static string MapTypeToPython(
        string t,
        IDictionary<string, string>? genericArgsToParamsDict = null,
        bool useGenericParams = false
    )
    {
        if (useGenericParams)
        {
            if (genericArgsToParamsDict?.TryGetValue(t, out var mappedType) == true)
            {
                return mappedType;
            }
        }
        else
        {
            if (
                genericArgsToParamsDict?.FirstOrDefault(kvp => kvp.Value == t) is var mappedType
                && mappedType.HasValue
                && mappedType.Value.Key is not null
            )
            {
                t = mappedType.Value.Key;
            }
        }

        return t.ToLowerInvariant().Replace("system.", "") switch
        {
            "sbyte"
            or "byte"
            or "short"
            or "ushort"
            or "int16"
            or "uint16"
            or "int32"
            or "int"
            or "uint32"
            or "uint"
            or "int64"
            or "long"
            or "uint64"
            or "ulong" => "int",
            "half" or "float" or "double" => "float",
            "guid" => "uuid.UUID",
            "boolean" or "bool" => "bool",
            "void" => "None",
            "string" or "char" => "str",
            _ => $"\"{PythonizeTypeName(t, genericArgsToParamsDict, useGenericParams)}\"",
        };
    }

    public static string MapTypeToNumpy(ExportedType t)
    {
        return t switch
        {
            ExportedType.Byte => "np.uint8",
            ExportedType.SByte => "np.int8",
            ExportedType.Int16 => "np.int16",
            ExportedType.UInt16 => "np.uint16",
            ExportedType.Int32 => "np.int32",
            ExportedType.UInt32 => "np.uint32",
            ExportedType.Int64 => "np.int64",
            ExportedType.UInt64 => "np.uint64",
            ExportedType.Float => "np.float32",
            ExportedType.Double => "np.float64",
            ExportedType.IntPtr => "np.intp",
            ExportedType.Void => "np.void",
            _ => "np.intp",
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

    // public static string MapTypeToC(string t)
    // {
    //     t = t.ToLowerInvariant().Replace("system.", "");
    //     return t switch
    //     {
    //         "byte" => "uint8_t",
    //         "sbyte" => "int8_t",
    //         "short" => "int16_t",
    //         "ushort" => "uint16_t",
    //         "int32" or "int" => "int32_t",
    //         "uint32" or "uint" => "uint32_t",
    //         "int64" or "long" => "int64_t",
    //         "uint64" or "ulong" => "uint64_t",
    //         "char" => "char",
    //         "void" => "void",
    //         "half" => "float",
    //         "float" => "float",
    //         "double" => "double",
    //         "intptr" => "void*",
    //         _ => throw new NotSupportedException($"Unsupported type: {t}"),
    //     };
    // }

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
