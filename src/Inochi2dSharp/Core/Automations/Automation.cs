using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Animations;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Automations;

public class Automation
{
    private Puppet parent;

    protected List<AutomationBinding> bindings = [];

    /// <summary>
    /// Human readable name of automation
    /// </summary>
    public string name;

    /// <summary>
    /// Whether the automation is enabled
    /// </summary>
    public bool enabled = true;

    /// <summary>
    /// Type ID of the automation
    /// </summary>
    public string typeId;

    public Automation(Puppet puppet)
    {
        this.parent = puppet;
    }

    /// <summary>
    /// Adds a binding
    /// </summary>
    /// <param name="binding"></param>
    public void bind(AutomationBinding binding)
    {
        this.bindings.Add(binding);
    }


    public void reconstruct(Puppet puppet)
    {
        foreach (var binding in bindings.ToArray()) 
            {
            binding.reconstruct(parent);
        }
    }

    /// <summary>
    /// Finalizes the loading of the automation
    /// </summary>
    /// <param name="parent"></param>
    public void finalize(Puppet parent)
    {
        this.parent = parent;
        foreach (var binding in bindings)
        {
            binding.finalize(parent);
        }
    }

    /// <summary>
    /// Updates and applies the automation to all the parameters
    /// that this automation is bound to
    /// </summary>
    public void update()
    {
        if (!enabled) return;
        this.onUpdate();
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializer.Add("type", typeId);
        serializer.Add("name", name);
        var list = new JArray(bindings);
        serializer.Add("bindings", list);
        serializeSelf(serializer);
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void deserialize(JObject data)
    {
        var temp = data["name"];
        if (temp != null)
        {
            name = temp.ToString();
        }

        temp = data["bindings"];
        if (temp is JArray array)
        {
            foreach (JObject obj in temp)
            {
                var item = new AutomationBinding();
                item.deserialize(obj);
                bindings.Add(item);
            }
        }
        deserializeSelf(data);
    }

    /// <summary>
    /// Helper function to remap range from 0.0-1.0
    /// to min-max
    /// </summary>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    protected float remapRange(float value, Vector2 range)
    {
        return range.X + value * (range.Y - range.X);
    }

    /// <summary>
    /// Called on update to update a single binding.
    /// 
    /// Use currTime() to get the current time
    /// Use deltaTime() to get delta time
    /// Use binding.range to get the range to apply the automation within.
    /// </summary>
    protected void onUpdate() { }

    protected void serializeSelf(JObject serializer) { }
    protected void deserializeSelf(JObject data) { }


}
