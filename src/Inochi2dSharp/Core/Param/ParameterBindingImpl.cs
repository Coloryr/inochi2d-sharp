using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public abstract class ParameterBindingImpl : ParameterBinding
{
    /// <summary>
    /// Node reference (for deserialization)
    /// </summary>
    protected uint nodeRef;

    /// <summary>
    /// Parent Parameter owning this binding
    /// </summary>
    public Parameter parameter;

    /// <summary>
    /// Reference to what parameter we're binding to
    /// </summary>
    public BindTarget target;

    /// <summary>
    /// Whether the value at each 2D keypoint is user-set
    /// </summary>
    public List<List<bool>> isSet_;

    public ParameterBindingImpl(Parameter parameter)
    {
        this.parameter = parameter;
    }

    public ParameterBindingImpl(Parameter parameter, Node targetNode, string paramName) 
    {
        this.parameter = parameter;
        target = new()
        {
            node = targetNode,
            paramName = paramName
        };

        clear();
    }

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public override BindTarget getTarget()
    {
        return target;
    }

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public override string getName()
    {
        return target.paramName;
    }

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Node getNode()
    {
        return target.node;
    }

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public override uint getNodeUUID()
    {
        return nodeRef;
    }

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public override List<List<bool>> getIsSet()
    {
        return [.. isSet_];
    }

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public override uint getSetCount()
    {
        uint count = 0;
        for (int x = 0; x < isSet_.Count; x++)
        {
            for (int y = 0; y < isSet_[x].Count; y++)
            {
                if (isSet_[x][y]) count++;
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
        target.node = puppet.find<Node>(nodeRef)!;
        //        writefln("node for %d = %x", nodeRef, &(target.node));
    }

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public override void setCurrent(Vector2Int point)
    {
        isSet_[point.X][point.Y] = true;

        reInterpolate();
    }

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public override bool isSet(Vector2Int index)
    {
        return isSet_[index.X][index.Y];
    }
}
