using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;

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
    public abstract void Reconstruct(Puppet puppet);

    /**
        Finalize loading of parameter
    */
    public abstract void Finalize(Puppet puppet);

    /// <summary>
    /// Apply a binding to the model at the given parameter value
    /// </summary>
    /// <param name="leftKeypoint"></param>
    /// <param name="offset"></param>
    public abstract void Apply(Vector2UInt leftKeypoint, Vector2 offset);

    /// <summary>
    /// Clear all keypoint data
    /// </summary>
    public abstract void Clear();

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public abstract void SetCurrent(Vector2UInt point);

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public abstract void Unset(Vector2UInt point);

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public abstract void Reset(Vector2UInt point);

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public abstract bool GetIsSet(Vector2UInt index);

    /// <summary>
    /// Scales the value, optionally with axis awareness
    /// </summary>
    /// <param name="index"></param>
    /// <param name="axis"></param>
    /// <param name="scale"></param>
    public abstract void ScaleValueAt(Vector2UInt index, int axis, float scale);

    /// <summary>
    /// Extrapolates the value across an axis
    /// </summary>
    /// <param name="index"></param>
    /// <param name="axis"></param>
    public abstract void ExtrapolateValueAt(Vector2UInt index, int axis);

    /// <summary>
    /// Copies the value to a point on another compatible binding
    /// </summary>
    /// <param name="src"></param>
    /// <param name="other"></param>
    /// <param name="dest"></param>
    public abstract void CopyKeypointToBinding(Vector2UInt src, ParameterBinding other, Vector2UInt dest);

    /// <summary>
    /// Swaps the value to a point on another compatible binding
    /// </summary>
    /// <param name="src"></param>
    /// <param name="other"></param>
    /// <param name="dest"></param>
    public abstract void SwapKeypointWithBinding(Vector2UInt src, ParameterBinding other, Vector2UInt dest);

    /// <summary>
    /// Flip the keypoints on an axis
    /// </summary>
    /// <param name="axis"></param>
    public abstract void ReverseAxis(uint axis);

    /// <summary>
    /// Update keypoint interpolation
    /// </summary>
    public abstract void ReInterpolate();

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public abstract bool[][] GetIsSet();

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public abstract uint GetSetCount();

    /// <summary>
    /// Move keypoints to a new axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="oldindex"></param>
    /// <param name="index"></param>
    public abstract void MoveKeypoints(uint axis, uint oldindex, uint index);

    /// <summary>
    /// Add keypoints along a new axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public abstract void InsertKeypoints(uint axis, uint index);

    /// <summary>
    /// Remove keypoints along an axis point
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public abstract void DeleteKeypoints(uint axis, uint index);

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public abstract BindTarget GetTarget();

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public abstract string GetName();

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public abstract Node GetNode();

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public abstract Guid GetNodeUUID();

    /// <summary>
    /// Checks whether a binding is compatible with another node
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public abstract bool IsCompatibleWithNode(Node other);

    /// <summary>
    /// The interpolation mode
    /// </summary>
    /// <returns></returns>
    public InterpolateMode InterpolateMode { get; set; } = InterpolateMode.Linear;

    /// <summary>
    /// Serialize
    /// </summary>
    /// <param name="data"></param>
    public abstract void Serialize(JsonObject data);

    /// <summary>
    /// Deserialize
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public abstract void Deserialize(JsonElement data);
}
