using System;
using System.Runtime.InteropServices;

namespace DotWrap.Internal;

internal static class DotWrapUtils
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

        return typeName;
    }
}
