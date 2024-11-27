using System.Text.Json;
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
