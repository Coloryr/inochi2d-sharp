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
    public float timestep = 0.0166f;

    /// <summary>
    /// Whether the animation is additive.
    /// 
    /// Additive animations will not replace main animations, but add their data
    /// on top of the running main animation
    /// </summary>
    public bool additive;

    /// <summary>
    /// The weight of the animation
    /// 
    /// This is only relevant for additive animations
    /// </summary>
    public float animationWeight;

    /// <summary>
    /// All of the animation lanes in this animation
    /// </summary>
    public List<AnimationLane> lanes = [];

    /// <summary>
    /// Length in frames
    /// </summary>
    public int length;

    /// <summary>
    /// Time where the lead-in ends
    /// </summary>
    public int leadIn = -1;

    /// <summary>
    /// Time where the lead-out starts
    /// </summary>
    public int leadOut = -1;

    public void reconstruct(Puppet puppet)
    {
        foreach (var lane in lanes.ToArray()) lane.reconstruct(puppet);
    }

    /// <summary>
    /// Finalizes the animation
    /// </summary>
    /// <param name="puppet"></param>
    public void finalize(Puppet puppet)
    {
        foreach (var lane in lanes) lane.finalize(puppet);
    }

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializer.Add("timestep", timestep);
        serializer.Add("additive", additive);
        serializer.Add("length", length);
        serializer.Add("leadIn", leadIn);
        serializer.Add("leadOut", leadOut);
        serializer.Add("animationWeight", animationWeight);

        var list = new JArray();
        foreach (var lane in lanes)
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
            timestep = (float)temp;
        }

        temp = data["additive"];
        if (temp != null)
        {
            additive = (bool)temp;
        }

        temp = data["animationWeight"];
        if (temp != null)
        {
            animationWeight = (float)temp;
        }

        temp = data["length"];
        if (temp != null)
        {
            length = (int)temp;
        }

        temp = data["leadIn"];
        if (temp != null)
        {
            leadIn = (int)temp;
        }

        temp = data["leadOut"];
        if (temp != null)
        {
            leadOut = (int)temp;
        }

        temp = data["lanes"];
        if (temp is JArray array)
        {
            foreach (JObject item in array.Cast<JObject>())
            {
                var land = new AnimationLane();
                land.deserialize(item);
                lanes.Add(land);
            }
        }
    }
}
