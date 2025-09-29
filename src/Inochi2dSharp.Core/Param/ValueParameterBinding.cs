using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Core.Param;

public class ValueParameterBinding : ParameterBindingImpl<float>
{
    public ValueParameterBinding(Parameter parameter) : base(parameter)
    {

    }

    public ValueParameterBinding(Parameter parameter, Node targetNode, string paramName) : base(parameter, targetNode, paramName)
    {

    }

    public override float Add(float value, float value1)
    {
        return value + value1;
    }

    public override float Add(float value, float value1, float value2)
    {
        return value + value1 + value2;
    }

    public override void ApplyToTarget(float value)
    {
        Target.Node.SetValue(Target.ParamName, value);
    }

    public override void ClearValue(ref float val)
    {
        val = Target.Node.GetDefaultValue(Target.ParamName);
    }

    public override float Cubic(float value, float value1, float value2, float value3, float value4)
    {
        return MathHelper.Cubic(value, value1, value2, value3, value4);
    }

    public override float DeserializeItem(JsonElement data)
    {
        return data.GetSingle();
    }

    public override bool IsCompatibleWithNode(Node other)
    {
        return other.HasParam(Target.ParamName);
    }

    public override float Lerp(float value, float value1, float value2)
    {
        return float.Lerp(value, value1, value2);
    }

    public override float Multiply(float value, float value1)
    {
        return value * value1;
    }

    public override void SerializeItem(float item, JsonArray data)
    {
        data.Add(item);
    }

    public override void ScaleValueAt(Vector2UInt index, int axis, float scale)
    {
        /* Nodes know how to do axis-aware scaling */
        SetValue(index, Node.ScaleValue(Target.ParamName, GetValue(index), axis, scale));
    }
}
