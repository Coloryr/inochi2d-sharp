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
    public string ParamId;

    /// <summary>
    /// Parameter to bind to
    /// </summary>
    public Parameter Param;

    /// <summary>
    /// Axis to bind to
    /// 0 = X
    /// 1 = Y
    /// </summary>
    public int Axis;

    /// <summary>
    /// Min/max range of binding
    /// </summary>
    public Vector2 Range;

    /// <summary>
    /// Gets the value at the specified axis
    /// </summary>
    /// <returns></returns>
    public float GetAxisValue()
    {
        return Axis switch
        {
            0 => Param.Value.X,
            1 => Param.Value.Y,
            _ => float.NaN,
        };
    }

    /// <summary>
    /// Sets axis value (WITHOUT REMAPPING)
    /// </summary>
    /// <param name="value"></param>
    public void SetAxisValue(float value)
    {
        switch (Axis)
        {
            case 0:
                Param.Value.X = value;
                break;
            case 1:
                Param.Value.Y = value;
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
        Param.PushIOffsetAxis(Axis, value);
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JObject serializer)
    {
        serializer.Add("param", Param.Name);
        serializer.Add("axis", Axis);
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
            ParamId = temp.ToString();
        }

        temp = data["axis"];
        if (temp != null)
        {
            Axis = (int)temp;
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
            if (parameter.Name == ParamId)
            {
                Param = parameter;
                return;
            }
        }
    }
}
