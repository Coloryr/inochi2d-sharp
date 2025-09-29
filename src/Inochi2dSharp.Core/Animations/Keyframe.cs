using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// A keyframe
/// </summary>
public record Keyframe
{
    /// <summary>
    /// The frame at which this frame occurs
    /// </summary>
    public int Frame;
    /// <summary>
    /// The value of the parameter at the given frame
    /// </summary>
    public float Value;
    /// <summary>
    /// Interpolation tension for cubic/inout
    /// </summary>
    public float Tension = 0.5f;

    public void Serialize(JsonObject obj)
    {
        obj.Add("frame", Frame);
        obj.Add("value", Value);
        obj.Add("tension", Tension);
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "frame" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Frame = item.Value.GetInt32();
            }
            else if (item.Name == "value" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Value = item.Value.GetSingle();
            }
            else if (item.Name == "tension" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Tension = item.Value.GetSingle();
            }
        }
    }
}
