using DotWrap;
using Newtonsoft.Json;

[assembly: DotWrapExternalPropertyMeta(
    typeof(Newtonsoft.Json.JsonSerializer),
    nameof(Newtonsoft.Json.JsonSerializer.DateFormatString)
)]

namespace DotWrap.TestLib;

[DotWrapExpose]
public class ExternalReference
{
    public static JsonSerializer GetJsonSerializer(JsonSerializer jsonSerializer)
    {
        return jsonSerializer;
    }
}
