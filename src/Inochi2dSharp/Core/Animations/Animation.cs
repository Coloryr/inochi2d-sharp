using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Animations;

public class Animation
{
    /// <summary>
    /// The timestep of each frame
    /// </summary>
    public float Timestep = 0.0166f;

    /// <summary>
    /// Whether the animation is additive.
    /// 
    /// Additive animations will not replace main animations, but add their data
    /// on top of the running main animation
    /// </summary>
    public bool Additive;

    /// <summary>
    /// The weight of the animation
    /// 
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

    public void reconstruct(Puppet puppet)
    {
        foreach (var lane in Lanes.ToArray()) lane.reconstruct(puppet);
    }

    /// <summary>
    /// Finalizes the animation
    /// </summary>
    /// <param name="puppet"></param>
    public void finalize(Puppet puppet)
    {
        foreach (var lane in Lanes) lane.finalize(puppet);
    }

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializer.Add("timestep", Timestep);
        serializer.Add("additive", Additive);
        serializer.Add("length", Length);
        serializer.Add("leadIn", LeadIn);
        serializer.Add("leadOut", LeadOut);
        serializer.Add("animationWeight", AnimationWeight);

        var list = new JArray();
        foreach (var lane in Lanes)
        {
            if (lane.paramRef?.targetParam != null)
            {
                var obj = new JObject();
                lane.serialize(obj);
                list.Add(obj);
            }
        }
        serializer.Add("lanes", list);
    }

    /// <summary>
    /// Deserialization function
    /// </summary>
    /// <param name="data"></param>
    public void deserialize(JObject data)
    {
        var temp = data["timestep"];
        if (temp != null)
        {
            Timestep = (float)temp;
        }

        temp = data["additive"];
        if (temp != null)
        {
            Additive = (bool)temp;
        }

        temp = data["animationWeight"];
        if (temp != null)
        {
            AnimationWeight = (float)temp;
        }

        temp = data["length"];
        if (temp != null)
        {
            Length = (int)temp;
        }

        temp = data["leadIn"];
        if (temp != null)
        {
            LeadIn = (int)temp;
        }

        temp = data["leadOut"];
        if (temp != null)
        {
            LeadOut = (int)temp;
        }

        temp = data["lanes"];
        if (temp is JArray array)
        {
            foreach (JObject item in array.Cast<JObject>())
            {
                var land = new AnimationLane();
                land.deserialize(item);
                Lanes.Add(land);
            }
        }
    }
}
