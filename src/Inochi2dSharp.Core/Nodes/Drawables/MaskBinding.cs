using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Drawables;

public record MaskBinding
{
    public Guid MaskSrcGUID;
    public MaskingMode Mode;
    public Drawable MaskSrc;

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="recursive"></param>
    public void Serialize(JsonObject obj, bool recursive = true)
    {
        obj["source"] = MaskSrcGUID.ToString();
        obj["mode"] = Mode.ToString();
    }

    /// <summary>
    /// Deserialization function
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        MaskSrcGUID = data.GetGuid("source", "source");
        if (data.TryGetProperty("mode", out var item))
        {
            Mode = item.GetString()!.ToMaskingMode();
        }
    }
}
