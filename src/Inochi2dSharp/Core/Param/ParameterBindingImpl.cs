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
    public BindTarget Target;

    /// <summary>
    /// Whether the value at each 2D keypoint is user-set
    /// </summary>
    public List<List<bool>> isSet;

    public ParameterBindingImpl(Parameter parameter)
    {
        Parameter = parameter;
    }

    public ParameterBindingImpl(Parameter parameter, Node targetNode, string paramName)
    {
        Parameter = parameter;
        Target = new()
        {
            node = targetNode,
            paramName = paramName
        };

        Clear();
    }

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public override BindTarget getTarget()
    {
        return Target;
    }

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public override string getName()
    {
        return Target.paramName;
    }

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Node getNode()
    {
        return Target.node;
    }

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public override uint getNodeUUID()
    {
        return NodeRef;
    }

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public override List<List<bool>> getIsSet()
    {
        return [.. isSet];
    }

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public override uint getSetCount()
    {
        uint count = 0;
        for (int x = 0; x < isSet.Count; x++)
        {
            for (int y = 0; y < isSet[x].Count; y++)
            {
                if (isSet[x][y]) count++;
            }
        }
        return count;
    }

    public override void reconstruct(Puppet puppet)
    {

    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public override void finalize(Puppet puppet)
    {
        //        writefln("finalize binding %s", this.getName());
        Target.node = puppet.Find<Node>(NodeRef)!;
        //        writefln("node for %d = %x", nodeRef, &(target.node));
    }

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public override void SetCurrent(Vector2Int point)
    {
        isSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public override bool IsSet(Vector2Int index)
    {
        return isSet[index.X][index.Y];
    }
}
