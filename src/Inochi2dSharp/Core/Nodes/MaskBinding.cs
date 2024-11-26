

using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Nodes;

public record MaskBinding
{
    public uint MaskSrcUUID;

    public MaskingMode Mode;

    public Drawable MaskSrc;

    public void Serialize(JsonObject obj)
    {
        obj.Add("source", MaskSrcUUID);
        obj.Add("mode", Mode.ToString());
    }

    public void Deserialize(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("source", out var temp) && temp != null)
        {
            MaskSrcUUID = temp.GetValue<uint>();
        }

        if (obj.TryGetPropertyValue("mode", out temp) && temp != null)
        {
            Mode = Enum.Parse<MaskingMode>(temp.GetValue<string>());
        }
    }
}
