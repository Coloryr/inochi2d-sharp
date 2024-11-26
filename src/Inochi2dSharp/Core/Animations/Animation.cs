using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Animations;

public class Animation
{
    /// <summary>
    /// The timestep of each frame
    /// </summary>
    public float Timestep { get; private set; } = 0.0166f;

    /// <summary>
    /// Whether the animation is additive.
    /// 
    /// Additive animations will not replace main animations, but add their data
    /// on top of the running main animation
    /// </summary>
    private bool _additive;

    /// <summary>
    /// The weight of the animation
    /// 
    /// This is only relevant for additive animations
    /// </summary>
    private float _animationWeight;

    /// <summary>
    /// All of the animation lanes in this animation
    /// </summary>
    public List<AnimationLane> Lanes { get; init; } = [];

    /// <summary>
    /// Length in frames
    /// </summary>
    public int Length { get; private set; }

    /// <summary>
    /// Time where the lead-in ends
    /// </summary>
    public int LeadIn { get; private set; } = -1;

    /// <summary>
    /// Time where the lead-out starts
    /// </summary>
    public int LeadOut { get; private set; } = -1;

    public void Reconstruct(Puppet puppet)
    {
        foreach (var lane in Lanes.ToArray()) lane.Reconstruct(puppet);
    }

    /// <summary>
    /// Finalizes the animation
    /// </summary>
    /// <param name="puppet"></param>
    public void JsonLoadDone(Puppet puppet)
    {
        foreach (var lane in Lanes) lane.JsonLoadDone(puppet);
    }

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("timestep", Timestep);
        serializer.Add("additive", _additive);
        serializer.Add("length", Length);
        serializer.Add("leadIn", LeadIn);
        serializer.Add("leadOut", LeadOut);
        serializer.Add("animationWeight", _animationWeight);

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
    public void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("timestep", out var temp) && temp != null)
        {
            Timestep = temp.GetValue<float>();
        }

        if (data.TryGetPropertyValue("additive", out temp) && temp != null)
        {
            _additive = temp.GetValue<bool>();
        }

        if (data.TryGetPropertyValue("animationWeight", out temp) && temp != null)
        {
            _animationWeight = temp.GetValue<float>();
        }

        if (data.TryGetPropertyValue("length", out temp) && temp != null)
        {
            Length = temp.GetValue<int>();
        }

        if (data.TryGetPropertyValue("leadIn", out temp) && temp != null)
        {
            LeadIn = temp.GetValue<int>();
        }

        if (data.TryGetPropertyValue("leadOut", out temp) && temp != null)
        {
            LeadOut = temp.GetValue<int>();
        }

        if (data.TryGetPropertyValue("lanes", out temp) && temp is JsonArray array)
        {
            foreach (JsonObject item in array.Cast<JsonObject>())
            {
                var land = new AnimationLane();
                land.Deserialize(item);
                Lanes.Add(land);
            }
        }
    }
}
