using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Automations;

public class VerletNode
{
    public float Distance = 1f;
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
    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "distance" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Distance = item.Value.GetSingle();
            }

            else if (item.Name == "position" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Position = item.Value.ToVector2();
            }

            else if (item.Name == "old_position" && item.Value.ValueKind == JsonValueKind.Array)
            {
                OldPosition = item.Value.ToVector2();
            }
        }
    }
}
