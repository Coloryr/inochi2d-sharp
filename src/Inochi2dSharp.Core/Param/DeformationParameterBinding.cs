using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Core.Param;

public class DeformationParameterBinding : ParameterBindingImpl<Deformation>
{
    public DeformationParameterBinding(Parameter parameter) : base(parameter)
    {

    }

    public DeformationParameterBinding(Parameter parameter, Node targetNode, string paramName) : base(parameter, targetNode, paramName)
    {

    }

    public void Update(Vector2UInt point, Vector2[] offsets)
    {
        IsSet[point.X][point.Y] = true;
        Values[point.X][point.Y].Update([.. offsets]);
        ReInterpolate();
    }

    public override void ApplyToTarget(Deformation value)
    {
        if (Target.ParamName != "deform")
        {
            throw new Exception("paramName is not deform");
        }

        if (Target.Node is IDeformable df)
        {
            df.Deform(value.VertexOffsets, false);
        }
    }

    public override void ClearValue(ref Deformation val)
    {
        // Reset deformation to identity, with the right vertex count
        if (Target.Node is IDeformable df)
        {
            val.Clear(df.DeformPoints.Length);
        }
    }

    public override void ScaleValueAt(Vector2UInt index, int axis, float scale)
    {
        var vecScale = axis switch
        {
            -1 => new Vector2(scale, scale),
            0 => new Vector2(scale, 1),
            1 => new Vector2(1, scale),
            _ => throw new Exception("Bad axis"),
        };

        /* Default to just scalar scale */
        SetValue(index, GetValue(index) * vecScale);
    }

    public override bool IsCompatibleWithNode(Node other)
    {
        if (Target.Node is IDeformable a)
        {
            if (other is IDeformable b)
            {
                return a.DeformPoints.Length == b.DeformPoints.Length;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public override void SerializeItem(Deformation item, JsonArray data)
    {
        var list = new JsonArray();
        item.Serialize(list);
        data.Add(list);
    }

    public override Deformation DeserializeItem(JsonElement data)
    {
        var deformation = new Deformation();
        deformation.Deserialize(data);
        return deformation;
    }

    public override Deformation Multiply(Deformation value, float value1)
    {
        return value * value1;
    }

    public override Deformation Add(Deformation value, Deformation value1)
    {
        return value + value1;
    }

    public override Deformation Add(Deformation value, Deformation value1, Deformation value2)
    {
        return value + value1 + value2;
    }

    public override Deformation Lerp(Deformation value, Deformation value1, float value2)
    {
        return Deformation.Lerp(value, value1, value2);
    }

    public override Deformation Cubic(Deformation value, Deformation value1, Deformation value2, Deformation value3, float value4)
    {
        return Deformation.Cubic(value, value1, value2, value3, value4);
    }
}
