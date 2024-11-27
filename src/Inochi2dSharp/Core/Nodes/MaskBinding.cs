

using System.Text.Json;
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

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "source" && item.Value.ValueKind != JsonValueKind.Null)
            {
                MaskSrcUUID = item.Value.GetUInt32();
            }
            else if (item.Name == "mode" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Mode = Enum.Parse<MaskingMode>(item.Value.GetString()!);
            }
        }
    }
}
