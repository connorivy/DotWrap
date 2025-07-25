using System;
using System.Linq;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Extensions;

public static class DotWrapObjectExtensions
{
    extension(IHasOriginalAndExposedTypes typeInfo)
    {
        public string OriginalTypeSimple => typeInfo.OriginalType.Split('.').Last();

        public string MapOriginalTypeToPython()
        {
            var t = typeInfo.OriginalType.ToLowerInvariant().Replace("system.", "");
            return t switch
            {
                "int32" or "int" or "int64" or "long" => "int",
                "float" or "double" => "float",
                "boolean" or "bool" => "bool",
                "void" => "None",
                "string" => "str",
                _ => $"\"{typeInfo.OriginalType.Split('.').Last()}\"",
            };
        }

        public string MapExposedTypeToPython()
        {
            var t = (typeInfo.ExposedTypeIfDifferent ?? typeInfo.OriginalType)
                .ToLowerInvariant()
                .Replace("system.", "");
            return t switch
            {
                "int32" or "int" => "int",
                "float" or "double" => "float",
                "boolean" or "bool" => "bool",
                "void" => "None",
                "string" => "CString", // use CString wrapper for strings
                _ => $"\"{typeInfo.OriginalType.Split('.').Last()}\"",
            };
        }

        public string MapExposedTypeToC()
        {
            var t = (typeInfo.ExposedTypeIfDifferent ?? typeInfo.OriginalType)
                .ToLowerInvariant()
                .Replace("system.", "");

            return t switch
            {
                "int32" or "int" => "int",
                "void" => "void",
                "float" => "float",
                "double" => "double",
                "boolean" or "bool" => "bool",
                "intptr" => "void*",
                "long" => "int64_t",
                // _ => "void*", // fallback for unsupported types
                _ => throw new NotSupportedException($"Unsupported type: {t}"),
            };
        }
    }
}
