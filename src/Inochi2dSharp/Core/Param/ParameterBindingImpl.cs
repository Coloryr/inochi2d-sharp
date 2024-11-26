using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Param;

public abstract class ParameterBindingImpl : ParameterBinding
{
    /// <summary>
    /// Node reference (for deserialization)
    /// </summary>
    protected uint NodeRef;

    /// <summary>
    /// Parent Parameter owning this binding
    /// </summary>
    public Parameter Parameter;

    /// <summary>
    /// Reference to what parameter we're binding to
    /// </summary>
    public BindTarget Target = new();

    /// <summary>
    /// Whether the value at each 2D keypoint is user-set
    /// </summary>
    public List<List<bool>> IsSet;

    public ParameterBindingImpl(Parameter parameter)
    {
        Parameter = parameter;
    }

    public ParameterBindingImpl(Parameter parameter, Node targetNode, string paramName)
    {
        Parameter = parameter;
        Target = new()
        {
            Node = targetNode,
            ParamName = paramName
        };

        Clear();
    }

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public override BindTarget GetTarget()
    {
        return Target;
    }

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public override string GetName()
    {
        return Target.ParamName;
    }

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Node GetNode()
    {
        return Target.Node;
    }

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public override uint GetNodeUUID()
    {
        return NodeRef;
    }

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public override List<List<bool>> GetIsSet()
    {
        return [.. IsSet];
    }

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public override uint GetSetCount()
    {
        uint count = 0;
        for (int x = 0; x < IsSet.Count; x++)
        {
            for (int y = 0; y < IsSet[x].Count; y++)
            {
                if (IsSet[x][y]) count++;
            }
        }
        return count;
    }

    public override void Reconstruct(Puppet puppet)
    {

    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public override void Finalize(Puppet puppet)
    {
        //        writefln("finalize binding %s", this.getName());
        Target.Node = puppet.Find<Node>(NodeRef)!;
        //        writefln("node for %d = %x", nodeRef, &(target.node));
    }

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public override void SetCurrent(Vector2Int point)
    {
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public override bool GetIsSet(Vector2Int index)
    {
        return IsSet[index.X][index.Y];
    }
}
