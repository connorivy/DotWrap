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
}
