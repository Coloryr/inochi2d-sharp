using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// An animation
/// </summary>
public class Animation
{
    /// <summary>
    /// The timestep of each frame
    /// </summary>
    public float Timestep = 0.0166f;

    /// <summary>
    /// Whether the animation is additive.
    /// <br/>
    /// Additive animations will not replace main animations, but add their data
    /// on top of the running main animation
    /// </summary>
    public bool Additive;

    /// <summary>
    /// The weight of the animation
    /// <br/>
    /// This is only relevant for additive animations
    /// </summary>
    public float AnimationWeight;

    /// <summary>
    /// All of the animation lanes in this animation
    /// </summary>
    public List<AnimationLane> Lanes = [];

    /// <summary>
    /// Length in frames
    /// </summary>
    public int Length;

    /// <summary>
    /// Time where the lead-in ends
    /// </summary>
    public int LeadIn = -1;

    /// <summary>
    /// Time where the lead-out starts
    /// </summary>
    public int LeadOut = -1;

    public void Reconstruct(Puppet puppet)
    {
        foreach (var lane in Lanes.ToArray()) lane.Reconstruct(puppet);
    }

    /// <summary>
    /// Finalizes the animation
    /// </summary>
    /// <param name="puppet"></param>
    public void Finalized(Puppet puppet)
    {
        foreach (var lane in Lanes)
        {
            lane.Finalized(puppet);
        }
    }

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("timestep", Timestep);
        serializer.Add("additive", Additive);
        serializer.Add("length", Length);
        serializer.Add("leadIn", LeadIn);
        serializer.Add("leadOut", LeadOut);
        serializer.Add("animationWeight", AnimationWeight);

        var list = new JsonArray();
        foreach (var lane in Lanes)
        {
            var obj = new JsonObject();
            lane.Serialize(obj);
            list.Add(obj);
        }
        serializer.Add("lanes", list);
    }

    /// <summary>
    /// Deserialization function
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "timestep" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Timestep = item.Value.GetSingle();
            }
            else if (item.Name == "additive" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Additive = item.Value.GetBoolean();
            }
            else if (item.Name == "animationWeight" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AnimationWeight = item.Value.GetSingle();
            }
            else if (item.Name == "length" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Length = item.Value.GetInt32();
            }
            else if (item.Name == "leadIn" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LeadIn = item.Value.GetInt32();
            }
            else if (item.Name == "leadOut" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LeadOut = item.Value.GetInt32();
            }
            else if (item.Name == "lanes" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    var land = new AnimationLane();
                    land.Deserialize(item1);
                    Lanes.Add(land);
                }
            }
        }
    }
}
