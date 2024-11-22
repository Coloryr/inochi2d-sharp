using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Automations;

/// <summary>
/// Automation binding
/// </summary>
public record AutomationBinding
{
    /// <summary>
    /// Used for serialization.
    /// Name of parameter
    /// </summary>
    public string paramId;

    /**
        Parameter to bind to
    */
    public Parameter param;

    /// <summary>
    /// Axis to bind to
    /// 0 = X
    /// 1 = Y
    /// </summary>
    public int axis;

    /// <summary>
    /// Min/max range of binding
    /// </summary>
    public Vector2 range;

    /// <summary>
    /// Gets the value at the specified axis
    /// </summary>
    /// <returns></returns>
    public float getAxisValue()
    {
        switch (axis)
        {
            case 0:
                return param.value.X;
            case 1:
                return param.value.Y;
            default: return float.NaN;
        }
    }

    /// <summary>
    /// Sets axis value (WITHOUT REMAPPING)
    /// </summary>
    /// <param name="value"></param>
    public void setAxisValue(float value)
    {
        switch (axis)
        {
            case 0:
                param.value.X = value;
                break;
            case 1:
                param.value.Y = value;
                break;
            default: throw new IndexOutOfRangeException("axis was out");
        }
    }

    /// <summary>
    /// Sets axis value (WITHOUT REMAPPING)
    /// </summary>
    /// <param name="value"></param>
    public void addAxisOffset(float value)
    {
        param.pushIOffsetAxis(axis, value);
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializer.Add("param", param.name);
        serializer.Add("axis", axis);
        serializer.Add("range", range.ToToken());
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void deserialize(JObject data)
    {
        var temp = data["param"];
        if (temp != null)
        {
            paramId = temp.ToString();
        }

        temp = data["axis"];
        if (temp != null)
        {
            axis = (int)temp;
        }

        temp = data["range"];
        if (temp != null)
        {
            range = temp.ToVector2();
        }
    }

    public void reconstruct(Puppet puppet) { }

    public void finalize(Puppet puppet)
    {
        foreach (var parameter in puppet.parameters)
        {
            if (parameter.name == paramId)
            {
                param = parameter;
                return;
            }
        }
    }
}
