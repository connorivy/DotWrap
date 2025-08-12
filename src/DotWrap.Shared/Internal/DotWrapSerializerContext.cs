using System.Text.Json;
using System.Text.Json.Serialization;
using DotWrap.Configuration;

namespace DotWrap;

[JsonSerializable(typeof(ExportedTypeDefinition))]
// [JsonSerializable(typeof(ExportedTypeDefinition))]
[JsonSerializable(typeof(ExportedMethodInfo))]
[JsonSerializable(typeof(ExportedParameterInfo))]
[JsonSerializable(typeof(ExportedEnumInfo))]
internal partial class DotWrapSerializerContext : JsonSerializerContext { }

public static class DotWrapSerializerOptions
{
    static DotWrapSerializerOptions()
    {
        Default = new JsonSerializerOptions { TypeInfoResolver = DotWrapSerializerContext.Default };
    }

    internal static JsonSerializerOptions Default { get; }
}
