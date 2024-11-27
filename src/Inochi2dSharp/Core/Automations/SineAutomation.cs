using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Automations;

[TypeId("sine")]
internal class SineAutomation : Automation
{
    /// <summary>
    /// Speed of the wave
    /// </summary>
    private float _speed = 1f;

    /// <summary>
    /// The phase of the wave
    /// </summary>
    private float _phase = 0f;

    /// <summary>
    /// The type of wave
    /// </summary>
    private SineType _sineType = SineType.Sin;

    private readonly I2dTime _time;

    public SineAutomation(Puppet parent, I2dTime time) : base(parent)
    {
        _time = time;
        TypeId = "sine";
    }

    protected override void OnUpdate()
    {
        foreach (var binding in Bindings)
        {
            var wave = _sineType switch
            {
                SineType.Sin => RemapRange((float.Sin((_time.CurrentTime() * _speed) + _phase) + 1.0f) / 2f, binding.Range),
                SineType.Cos => RemapRange((float.Cos((_time.CurrentTime() * _speed) + _phase) + 1.0f) / 2f, binding.Range),
                SineType.Tan => RemapRange((float.Tan((_time.CurrentTime() * _speed) + _phase) + 1.0f) / 2f, binding.Range),
                _ => throw new Exception("sineType error"),
            };
            binding.AddAxisOffset(wave);
        }
    }

    protected override void SerializeSelf(JsonObject serializer)
    {
        serializer.Add("speed", _speed);
        serializer.Add("sine_type", (int)_sineType);
    }

    protected override void DeserializeSelf(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "speed" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _speed = item.Value.GetInt32();
            }

            else if (item.Name == "sine_type" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _sineType = (SineType)item.Value.GetInt32();
            }
        }
    }
}
