using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// A keyframe
/// </summary>
public record Keyframe
{
    /// <summary>
    /// The frame at which this frame occurs
    /// </summary>
    public int Frame { get; set; }
    /// <summary>
    /// The value of the parameter at the given frame
    /// </summary>
    public float Value { get; set; }
    /// <summary>
    /// Interpolation tension for cubic/inout
    /// </summary>
    public float Tension { get; set; } = 0.5f;

    public void Serialize(JsonObject obj)
    {
        obj.Add("frame", Frame);
        obj.Add("value", Value);
        obj.Add("tension", Tension);
    }

    public void Deserialize(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("frame", out var value) && value != null)
        {
            Frame = value.GetValue<int>();
        }
        if (obj.TryGetPropertyValue("value", out value) && value != null)
        {
            Value = value.GetValue<int>();
        }
        if (obj.TryGetPropertyValue("tension", out value) && value != null)
        {
            Tension = value.GetValue<int>();
        }
    }
}
