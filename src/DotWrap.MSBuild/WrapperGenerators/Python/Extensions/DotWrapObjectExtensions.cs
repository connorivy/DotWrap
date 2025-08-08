using System;
using System.Collections.Generic;
using System.Linq;
using DotWrap.Configuration;
using DotWrap.Utils.Python;

namespace DotWrap.MSBuild.WrapperGenerators.Python.Extensions;

public static class DotWrapObjectExtensions
{
    extension(IHasOriginalAndExposedTypes typeInfo)
    {
        public string OriginalTypeSimple => typeInfo.OriginalTypeName.Split('.').Last();

        public string OriginalTypeWrapper => PythonNamingUtils.PythonizeClassName(typeInfo.OriginalTypeName);

        public string MapOriginalTypeToPython(IDictionary<string, string>? genericParamsToArgsDict)
        {
            var t = typeInfo.OriginalTypeName.ToLowerInvariant().Replace("system.", "");
            return t switch
            {
                "sbyte" or
                "byte" or
                "short" or
                "ushort" or
                "int32" or "int" or
                "uint32" or "uint" or
                "int64" or "long" or
                "uint64" or "ulong" => "int",
                "float" or "double" => "float",
                "boolean" or "bool" => "bool",
                "void" => "None",
                "string" => "str",
                _ => $"\"{PythonNamingUtils.PythonizeTypeName(typeInfo.OriginalTypeName, genericParamsToArgsDict)}\"",
            };
        }

        public string MapExposedTypeToPython()
        {
            var t = (typeInfo.ExposedTypeIfDifferent ?? typeInfo.OriginalTypeName)
                .ToLowerInvariant()
                .Replace("system.", "");
            return t switch
            {
                "int32" or "int" => "int",
                "float" or "double" => "float",
                "boolean" or "bool" => "bool",
                "void" => "None",
                "string" => "CString", // use CString wrapper for strings
                _ => $"\"{PythonNamingUtils.PythonizeTypeName(typeInfo.OriginalTypeName.Split('.').Last())}\"",
            };
        }

        public string MapExposedTypeToC()
        {
            var t = (typeInfo.ExposedTypeIfDifferent ?? typeInfo.OriginalTypeName)
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
                "arrayinfo" => "ArrayInfo",
                _ => throw new NotSupportedException($"Unsupported type: {t}"),
            };
        }
    }
}