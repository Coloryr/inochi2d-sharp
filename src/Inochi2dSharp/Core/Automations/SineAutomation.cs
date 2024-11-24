using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Automations;

[TypeId("sine")]
public class SineAutomation : Automation
{
    /// <summary>
    /// Speed of the wave
    /// </summary>
    public float Speed = 1f;

    /// <summary>
    /// The phase of the wave
    /// </summary>
    public float Phase = 0f;

    /// <summary>
    /// The type of wave
    /// </summary>
    public SineType SineType = SineType.Sin;

    public SineAutomation(Puppet parent) : base(parent)
    {
        TypeId = "sine";
    }

    protected override void OnUpdate()
    {
        foreach (var binding in Bindings)
        {
            var wave = SineType switch
            {
                SineType.Sin => RemapRange((float.Sin((Inochi2d.currentTime() * Speed) + Phase) + 1.0f) / 2f, binding.Range),
                SineType.Cos => RemapRange((float.Cos((Inochi2d.currentTime() * Speed) + Phase) + 1.0f) / 2f, binding.Range),
                SineType.Tan => RemapRange((float.Tan((Inochi2d.currentTime() * Speed) + Phase) + 1.0f) / 2f, binding.Range),
                _ => throw new Exception("sineType error"),
            };
            binding.AddAxisOffset(wave);
        }
    }

    protected override void SerializeSelf(JObject serializer)
    {
        serializer.Add("speed", Speed);
        serializer.Add("sine_type", (int)SineType);
    }

    protected override void DeserializeSelf(JObject data)
    {
        var temp = data["speed"];
        if (temp != null)
        {
            Speed = (int)temp;
        }

        temp = data["sine_type"];
        if (temp != null)
        {
            SineType = (SineType)(int)temp;
        }
    }
}
