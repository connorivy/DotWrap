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
    }
}
