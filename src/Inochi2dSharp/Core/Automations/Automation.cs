using System.Numerics;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Automations;

public class Automation(Puppet puppet)
{
    private Puppet Parent = puppet;

    protected List<AutomationBinding> Bindings = [];

    /// <summary>
    /// Human readable name of automation
    /// </summary>
    public string Name;

    /// <summary>
    /// Whether the automation is enabled
    /// </summary>
    public bool Enabled = true;

    /// <summary>
    /// Type ID of the automation
    /// </summary>
    public string TypeId;

    /// <summary>
    /// Adds a binding
    /// </summary>
    /// <param name="binding"></param>
    public virtual void Bind(AutomationBinding binding)
    {
        Bindings.Add(binding);
    }

    public void Reconstruct(Puppet puppet)
    {
        foreach (var binding in Bindings.ToArray())
        {
            binding.Reconstruct(Parent);
        }
    }

    /// <summary>
    /// Finalizes the loading of the automation
    /// </summary>
    /// <param name="parent"></param>
    public void Finalize(Puppet parent)
    {
        Parent = parent;
        foreach (var binding in Bindings)
        {
            binding.Finalize(parent);
        }
    }

    /// <summary>
    /// Updates and applies the automation to all the parameters
    /// that this automation is bound to
    /// </summary>
    public void Update()
    {
        if (!Enabled) return;
        OnUpdate();
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JObject serializer)
    {
        serializer.Add("type", TypeId);
        serializer.Add("name", Name);
        var list = new JArray();
        foreach (var item in Bindings)
        {
            var obj = new JObject();
            item.Serialize(obj);
            list.Add(obj);
        }
        serializer.Add("bindings", list);
        SerializeSelf(serializer);
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JObject data)
    {
        var temp = data["name"];
        if (temp != null)
        {
            Name = temp.ToString();
        }

        temp = data["type"];
        if (temp != null)
        {
            TypeId = temp.ToString();
        }

        temp = data["bindings"];
        if (temp is JArray array)
        {
            foreach (JObject obj in array.Cast<JObject>())
            {
                var item = new AutomationBinding();
                item.Deserialize(obj);
                Bindings.Add(item);
            }
        }
        DeserializeSelf(data);
    }

    /// <summary>
    /// Helper function to remap range from 0.0-1.0
    /// to min-max
    /// </summary>
    /// <param name="value"></param>
    /// <param name="range"></param>
    /// <returns></returns>
    protected float RemapRange(float value, Vector2 range)
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
    protected virtual void OnUpdate() { }

    protected virtual void SerializeSelf(JObject serializer) { }
    protected virtual void DeserializeSelf(JObject data) { }
}
