using System;

namespace DotWrap.Utils;

public static class DotWrapUtils
{
    public static string GetStamp(string t)
    {
        unchecked
        {
            const uint seed = 0x811C9DC;
            const uint prime = 16777619;
            uint hash = seed;

            foreach (char c in t)
                hash = (hash ^ c) * prime;

            return hash.ToString("X8");
        }
    }

    public static string GetExposedTypeFromCsType(string type, out bool isOriginalType)
    {
        isOriginalType = true;

        var originalType = type.ToLowerInvariant() switch
        {
            "sbyte" => "sbyte",
            "byte" => "byte",
            "short" => "short",
            "ushort" => "ushort",
            "int" => "int",
            "uint" => "uint",
            "long" => "long",
            "ulong" => "ulong",
            "float" => "float",
            "double" => "double",
            "intptr" => "IntPtr",
            "void" => "void",
            "half" => "float",
            _ => null,
        };
        if (originalType != null)
        {
            return originalType;
        }
        isOriginalType = false;
        return type switch
        {
            "boolean" or "bool" => "int",
            "string" => "IntPtr",
            _ => "IntPtr", // Default to IntPtr for unsupported types
        };
    }

    public static string NormalizeCsTypeName(string typeName)
    {
        // Normalize the C# type name to a more Pythonic format
        // it may look like some of these assignments don't do anything,
        // but actually they are enforcing lowercase
        var normalized = typeName.ToLowerInvariant().Replace("system.", "") switch
        {
            "int16" => "short",
            "uint16" => "ushort",
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            "single" or "float" => "float",
            "double" => "double",
            "string" => "string",
            "object" => "CObject", // object is a reserved word in python
            _ => null,
        };
        return normalized ?? typeName;
    }

    /// <summary>
    /// int[] -> intArray
    /// int[][] -> intArrayArray
    /// int[,] -> intArray2D
    /// int[,,] -> intArray3D
    /// int[,,][] -> intArray3DArray
    /// int[][,,] -> intArrayArray2D
    /// hello -> hello
    /// List<int[]>[] -> List<intArray>Array
    /// </summary>
    /// <param name="typeName"></param>
    /// <returns></returns>
    public static string ReplaceArraySymbols(string typeName)
    {
        // Replace [] with Array
        typeName = typeName.Replace("[]", "Array");

        // Replace multidimensional arrays [,,] with ArrayND (N = number of commas + 1)
        // Regex: \[(,+)\]
        var regex = new System.Text.RegularExpressions.Regex(@"\[(,+)\]");
        typeName = regex.Replace(
            typeName,
            m =>
            {
                int n = m.Groups[1].Value.Length + 1;
                return $"Array{n}d";
            }
        );

        return string.Join(
            "Array",
            typeName.Split(new[] { "Array" }, StringSplitOptions.None).Select(NormalizeCsTypeName)
        );
    }

    /// <summary>
    /// takes an assembly qualified name and returns the original type string
    /// e.g. System.Collections.Generic.ICollection`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e],[System.Int64, System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]], System.Private.CoreLib, Version=10.0.0.0, Culture=neutral, PublicKeyToken=7cec85d7bea7798e]]
    /// System.Collections.Generic.ICollection<System.Collections.Generic.KeyValuePair<System.Int32, System.Int64>>
    /// </summary>
    /// <param name="assemblyQualifiedName"></param>
    /// <returns></returns>
    public static string GetOriginalTypeString(string assemblyQualifiedName)
    {
        var simplifiedAssemblyName = AssemblyNameUtils.GetSimplifiedAssemblyName(
            assemblyQualifiedName
        );

        // I have a simplified assembly name, now I need to convert it to the original type string
        // "System.Collections.Generic.KeyValuePair`2[[System.Collections.Generic.List`1[[System.Collections.Generic.List`1[[System.Collections.Generic.KeyValuePair`2[[System.Int32],[System.Int64]]]]]]],[System.Int32]]";
        // create a regex with the following rules
        // Replace `(anyNumber)[[ with "<" (i.e. "`2[[" -> "<")
        // Replace "]]" with ">" (i.e. `]]` -> `>`)
        // Replace "],[" with ", "

        var result = simplifiedAssemblyName;

        // Replace `(anyNumber)[[ with "<"
        result = System.Text.RegularExpressions.Regex.Replace(result, @"`\d+\[\[", "<");

        // Replace "]]" with ">"
        result = result.Replace("]]", ">");

        // Replace "],[" with ", "
        result = result.Replace("],[", ", ");

        return result;
    }
}
