using System.Numerics;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Automations;

public class Automation(Puppet puppet)
{
    private Puppet _parent = puppet;

    protected List<AutomationBinding> Bindings { get; init; } = [];

    /// <summary>
    /// Human readable name of automation
    /// </summary>
    private string _name;

    /// <summary>
    /// Whether the automation is enabled
    /// </summary>
    private bool _enabled = true;

    /// <summary>
    /// Type ID of the automation
    /// </summary>
    public string TypeId { get; set; }

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
            binding.Reconstruct(_parent);
        }
    }

    /// <summary>
    /// Finalizes the loading of the automation
    /// </summary>
    /// <param name="parent"></param>
    public void JsonLoadDone(Puppet parent)
    {
        _parent = parent;
        foreach (var binding in Bindings)
        {
            binding.JsonLoadDone(parent);
        }
    }

    /// <summary>
    /// Updates and applies the automation to all the parameters
    /// that this automation is bound to
    /// </summary>
    public void Update()
    {
        if (!_enabled) return;
        OnUpdate();
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("type", TypeId);
        serializer.Add("name", _name);
        var list = new JsonArray();
        foreach (var item in Bindings)
        {
            var obj = new JsonObject();
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
    public void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("name", out var temp) && temp != null)
        {
            _name = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("type", out temp) && temp != null)
        {
            TypeId = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("bindings", out temp) && temp is JsonArray array)
        {
            foreach (JsonObject obj in array.Cast<JsonObject>())
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
    protected static float RemapRange(float value, Vector2 range)
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

    protected virtual void SerializeSelf(JsonObject serializer) { }
    protected virtual void DeserializeSelf(JsonObject data) { }
}
