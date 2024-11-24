using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Inochi2dSharp.Core.Nodes;

public record MaskBinding
{
    [JsonProperty("maskSrcUUID")]
    public uint MaskSrcUUID;

    [JsonProperty("mode")]
    [JsonConverter(typeof(StringEnumConverter))]
    public MaskingMode Mode;

    [JsonIgnore]
    public Drawable maskSrc;
}
