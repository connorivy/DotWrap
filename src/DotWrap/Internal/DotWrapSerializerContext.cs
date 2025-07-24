using System.Text.Json;
using System.Text.Json.Serialization;
using DotWrap.MSBuild;

namespace DotWrap;

[JsonSerializable(typeof(ExportedClassInfo))]
[JsonSerializable(typeof(ExportedMethodInfo))]
[JsonSerializable(typeof(ExportedParameterInfo))]
internal partial class DotWrapSerializerContext : JsonSerializerContext { }

public static class DotWrapSerializerOptions
{
    internal static JsonSerializerOptions Default
    {
        get
        {
            field ??= new() { TypeInfoResolver = DotWrapSerializerContext.Default };
            return field;
        }
    }
}
