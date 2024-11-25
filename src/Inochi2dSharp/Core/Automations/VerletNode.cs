using System.Numerics;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Automations;

public class VerletNode
{
    public float Distance { get; set; } = 1f;
    public Vector2 Position;
    public Vector2 OldPosition;

    public VerletNode()
    {
        Position = new();
        OldPosition = new();
    }

    public VerletNode(Vector2 pos)
    {
        Position = pos;
        OldPosition = pos;
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("distance", Distance);
        serializer.Add("position", Position.ToToken());
        serializer.Add("old_position", OldPosition.ToToken());
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("distance", out var temp) && temp != null)
        {
            Distance = temp.GetValue<float>();
        }

        if (data.TryGetPropertyValue("position", out temp) && temp is JsonArray array)
        {
            Position = array.ToVector2();
        }

        if (data.TryGetPropertyValue("old_position", out temp) && temp is JsonArray array1)
        {
            OldPosition = array1.ToVector2();
        }
    }
}
