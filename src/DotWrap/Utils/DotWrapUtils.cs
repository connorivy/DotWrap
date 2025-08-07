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
        var normalized = typeName.ToLowerInvariant().Replace("system.", "") switch
        {
            "int16" => "short",
            "uint16" => "ushort",
            "int32" => "int",
            "uint32" => "uint",
            "int64" => "long",
            "uint64" => "ulong",
            "string" => "string", // make sure string is lowercase
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

        return typeName;
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
        if (string.IsNullOrEmpty(assemblyQualifiedName))
            return assemblyQualifiedName;

        // Find the first comma that separates the type name from assembly info
        int assemblyStart = FindAssemblyStart(assemblyQualifiedName);
        string typePart =
            assemblyStart > 0
                ? assemblyQualifiedName.Substring(0, assemblyStart)
                : assemblyQualifiedName;

        return ParseGenericType(typePart);
    }

    private static int FindAssemblyStart(string assemblyQualifiedName)
    {
        int bracketDepth = 0;
        for (int i = 0; i < assemblyQualifiedName.Length; i++)
        {
            char c = assemblyQualifiedName[i];
            if (c == '[')
                bracketDepth++;
            else if (c == ']')
                bracketDepth--;
            else if (c == ',' && bracketDepth == 0)
                return i;
        }
        return -1;
    }

    private static string ParseGenericType(string typePart)
    {
        // Handle generic types with backtick notation (e.g., List`1, Dictionary`2)
        int backtickIndex = typePart.IndexOf('`');
        if (backtickIndex == -1)
        {
            // Not a generic type, return as-is
            return typePart;
        }

        // Extract the base type name
        string baseTypeName = typePart.Substring(0, backtickIndex);

        // Find the generic arguments enclosed in [[ ]]
        int genericStart = typePart.IndexOf("[[");
        if (genericStart == -1)
        {
            // No generic arguments found, just return base type
            return baseTypeName;
        }

        // Parse generic arguments
        var genericArgs = ParseGenericArguments(typePart.Substring(genericStart));

        // Construct the C# format with angle brackets
        return baseTypeName + "<" + string.Join(", ", genericArgs) + ">";
    }

    private static string[] ParseGenericArguments(string genericPart)
    {
        var args = new System.Collections.Generic.List<string>();

        // Remove the outer [[ ]]
        if (genericPart.StartsWith("[[") && genericPart.EndsWith("]]"))
        {
            genericPart = genericPart.Substring(2, genericPart.Length - 4);
        }

        // Split by "],["  to handle bracketed arguments
        // This handles the pattern [arg1],[arg2],[arg3]...
        string[] parts = genericPart.Split(new string[] { "],[" }, StringSplitOptions.None);

        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i];

            // Remove leading [ from first part and trailing ] from last part
            if (i == 0 && part.StartsWith("["))
                part = part.Substring(1);
            if (i == parts.Length - 1 && part.EndsWith("]"))
                part = part.Substring(0, part.Length - 1);

            args.Add(ProcessGenericArgument(part));
        }

        return args.ToArray();
    }

    private static string ProcessGenericArgument(string arg)
    {
        // Remove surrounding brackets if present
        if (arg.StartsWith("[") && arg.EndsWith("]"))
        {
            arg = arg.Substring(1, arg.Length - 2);
        }

        // Recursively process this argument as it might be a generic type itself
        return GetOriginalTypeString(arg);
    }
}
