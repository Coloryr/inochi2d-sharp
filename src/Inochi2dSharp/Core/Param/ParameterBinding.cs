using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

/// <summary>
/// A binding to a parameter, of a given value type
/// </summary>
public abstract class ParameterBinding
{
    /// <summary>
    /// Restructure object before finalization
    /// </summary>
    /// <param name="puppet"></param>
    public abstract void reconstruct(Puppet puppet);

    /**
        Finalize loading of parameter
    */
    public abstract void finalize(Puppet puppet);

    /// <summary>
    /// Apply a binding to the model at the given parameter value
    /// </summary>
    /// <param name="leftKeypoint"></param>
    /// <param name="offset"></param>
    public abstract void apply(Vector2Uint leftKeypoint, Vector2 offset);

    /// <summary>
    /// Clear all keypoint data
    /// </summary>
    public abstract void clear();

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public abstract void setCurrent(Vector2Uint point);

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public abstract void unset(Vector2Uint point);

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public abstract void reset(Vector2Uint point);

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public abstract bool isSet(Vector2Uint index);

    /// <summary>
    /// Scales the value, optionally with axis awareness
    /// </summary>
    /// <param name="index"></param>
    /// <param name="axis"></param>
    /// <param name="scale"></param>
    public abstract void scaleValueAt(Vector2Uint index, int axis, float scale);

    /// <summary>
    /// Extrapolates the value across an axis
    /// </summary>
    /// <param name="index"></param>
    /// <param name="axis"></param>
    public abstract void extrapolateValueAt(Vector2Uint index, int axis);

    /// <summary>
    /// Copies the value to a point on another compatible binding
    /// </summary>
    /// <param name="src"></param>
    /// <param name="other"></param>
    /// <param name="dest"></param>
    public abstract void copyKeypointToBinding(Vector2Uint src, ParameterBinding other, Vector2Uint dest);

    /// <summary>
    /// Swaps the value to a point on another compatible binding
    /// </summary>
    /// <param name="src"></param>
    /// <param name="other"></param>
    /// <param name="dest"></param>
    public abstract void swapKeypointWithBinding(Vector2Uint src, ParameterBinding other, Vector2Uint dest);

    /// <summary>
    /// Flip the keypoints on an axis
    /// </summary>
    /// <param name="axis"></param>
    public abstract void reverseAxis(int axis);

    /// <summary>
    /// Update keypoint interpolation
    /// </summary>
    public abstract void reInterpolate();

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public abstract bool[][] getIsSet();

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public abstract uint getSetCount();

    /// <summary>
    /// Move keypoints to a new axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="oldindex"></param>
    /// <param name="index"></param>
    public abstract void moveKeypoints(int axis, int oldindex, int index);

    /// <summary>
    /// Add keypoints along a new axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public abstract void insertKeypoints(int axis, int index);

    /// <summary>
    /// Remove keypoints along an axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public abstract void deleteKeypoints(int axis, int index);

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public abstract BindTarget getTarget();

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public abstract string getName();

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public abstract Node getNode();

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public abstract uint getNodeUUID();

    /// <summary>
    /// Checks whether a binding is compatible with another node
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public abstract bool isCompatibleWithNode(Node other);

    /// <summary>
    /// The interpolation mode
    /// </summary>
    /// <returns></returns>
    public abstract InterpolateMode interpolateMode { get; set; }

    /// <summary>
    /// Serialize
    /// </summary>
    /// <param name="serializer"></param>
    public abstract void serialize(JObject serializer);

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public abstract void deserialize(JObject data);
}
