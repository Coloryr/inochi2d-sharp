

using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Nodes;

public record MaskBinding
{
    public uint MaskSrcUUID { get; set; }

    public MaskingMode Mode { get; set; }

    public Drawable MaskSrc { get; set; }

    public void Serialize(JsonObject obj)
    {
        obj.Add("maskSrcUUID", MaskSrcUUID);
        obj.Add("mode", Mode.ToString());
    }

    public void Deserialize(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("maskSrcUUID", out var temp) && temp != null)
        {
            MaskSrcUUID = temp.GetValue<uint>();
        }

        if (obj.TryGetPropertyValue("mode", out temp) && temp != null)
        {
            Mode = Enum.Parse<MaskingMode>(temp.GetValue<string>());
        }
    }
}
