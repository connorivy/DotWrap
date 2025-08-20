using System;
using DotWrap;

[assembly: DotWrapExternalPropertyMeta(
    typeof(Newtonsoft.Json.JsonSerializer),
    nameof(Newtonsoft.Json.JsonSerializer.DateFormatString)
)]

namespace DotWrap.TestLib;

[DotWrapExpose]
public class ExternalReference
{
    public static Newtonsoft.Json.JsonSerializer GetJsonSerializer()
    {
        return new Newtonsoft.Json.JsonSerializer();
    }
}
