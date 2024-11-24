using System.Numerics;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Automations;

/// <summary>
/// Automation binding
/// </summary>
public class AutomationBinding
{
    /// <summary>
    /// Used for serialization.
    /// Name of parameter
    /// </summary>
    private string _paramId;

    /// <summary>
    /// Parameter to bind to
    /// </summary>
    private Parameter _param;

    /// <summary>
    /// Axis to bind to
    /// 0 = X
    /// 1 = Y
    /// </summary>
    private int _axis;

    /// <summary>
    /// Min/max range of binding
    /// </summary>
    public Vector2 Range { get; private set; }

    /// <summary>
    /// Gets the value at the specified axis
    /// </summary>
    /// <returns></returns>
    public float GetAxisValue()
    {
        return _axis switch
        {
            0 => _param.Value.X,
            1 => _param.Value.Y,
            _ => float.NaN,
        };
    }

    /// <summary>
    /// Sets axis value (WITHOUT REMAPPING)
    /// </summary>
    /// <param name="value"></param>
    public void SetAxisValue(float value)
    {
        switch (_axis)
        {
            case 0:
                _param.Value.X = value;
                break;
            case 1:
                _param.Value.Y = value;
                break;
            default: throw new IndexOutOfRangeException("axis was out");
        }
    }

    /// <summary>
    /// Sets axis value (WITHOUT REMAPPING)
    /// </summary>
    /// <param name="value"></param>
    public void AddAxisOffset(float value)
    {
        _param.PushIOffsetAxis(_axis, value);
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JObject serializer)
    {
        serializer.Add("param", _param.Name);
        serializer.Add("axis", _axis);
        serializer.Add("range", Range.ToToken());
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JObject data)
    {
        var temp = data["param"];
        if (temp != null)
        {
            _paramId = temp.ToString();
        }

        temp = data["axis"];
        if (temp != null)
        {
            _axis = (int)temp;
        }

        temp = data["range"];
        if (temp != null)
        {
            Range = temp.ToVector2();
        }
    }

    public void Reconstruct(Puppet puppet) { }

    public void Finalize(Puppet puppet)
    {
        foreach (var parameter in puppet.Parameters)
        {
            if (parameter.Name == _paramId)
            {
                _param = parameter;
                return;
            }
        }
    }
}
