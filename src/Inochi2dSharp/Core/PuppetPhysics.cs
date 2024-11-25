using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet physics settings
/// </summary>
public record PuppetPhysics
{
    public float PixelsPerMeter { get; set; } = 1000;

    public float Gravity { get; set; } = 9.8f;

    public void Serialize(JsonObject serializer)
    {
        serializer.Add("pixelsPerMeter", PixelsPerMeter);
        serializer.Add("gravity", Gravity);
    }

    public void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("pixelsPerMeter", out var temp) && temp != null)
        {
            PixelsPerMeter = temp.GetValue<float>();
        }

        if (data.TryGetPropertyValue("gravity", out temp) && temp != null)
        {
            Gravity = temp.GetValue<float>();
        }
    }
}
