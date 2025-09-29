using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet physics settings
/// </summary>
public record PuppetPhysics
{
    /// <summary>
    /// Pixels-per-meter for the physics system
    /// </summary>
    public float PixelsPerMeter { get; set; } = 1000;
    /// <summary>
    /// Gravity for the physics system
    /// </summary>
    public float Gravity { get; set; } = 9.8f;
    /// <summary>
    /// Serializes the type.
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("pixelsPerMeter", PixelsPerMeter);
        serializer.Add("gravity", Gravity);
    }
    /// <summary>
    /// Deserializes the type.
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "pixelsPerMeter" && item.Value.ValueKind != JsonValueKind.Null)
            {
                PixelsPerMeter = item.Value.GetSingle();
            }
            else if (item.Name == "gravity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Gravity = item.Value.GetSingle();
            }
        }
    }
}
