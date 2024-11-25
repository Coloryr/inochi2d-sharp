using System.Numerics;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

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
    public void Serialize(JObject serializer)
    {
        serializer.Add("distance", Distance);
        serializer.Add("position", Position.ToToken());
        serializer.Add("old_position", OldPosition.ToToken());
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JObject data)
    {
        var temp = data["distance"];
        if (temp != null)
        {
            Distance = (float)temp;
        }

        temp = data["position"];
        if (temp != null)
        {
            Position = temp.ToVector2();
        }

        temp = data["old_position"];
        if (temp != null)
        {
            OldPosition = temp.ToVector2();
        }
    }
}
