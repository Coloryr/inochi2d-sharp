using System.Numerics;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// Interface implemented by types which can be deformed.
/// </summary>
public interface IDeformable
{
    /// <summary>
    /// The base position of the deformable's points.
    /// </summary>
    Vector2[] BasePoints { get; }

    /// <summary>
    /// The points which may be deformed by the deformer.
    /// </summary>
    Vector2[] DeformPoints { get; }

    /// <summary>
    /// The base transform of the object before any parameters have been applied.
    /// </summary>
    Transform BaseTransform { get; }

    /// <summary>
    /// World transform of the deformable object.
    /// </summary>
    Transform WorldTransform { get; }

    /// <summary>
    /// Deforms the IDeformable.
    /// </summary>
    /// <param name="deformed">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    void Deform(Vector2[] deformed, bool absolute = false);

    /// <summary>
    /// Deforms a single vertex in the IDeformable
    /// </summary>
    /// <param name="offset">The offset into the point list to deform.</param>
    /// <param name="deform">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    void Deform(int offset, Vector2 deform, bool absolute = false);

    /// <summary>
    /// Applies an offset to the IDeformable's transform.
    /// </summary>
    /// <param name="other">The transform to offset the current global transform by.</param>
    void OffsetTransform(Transform other);

    /// <summary>
    /// Resets the deformation for the IDeformable.
    /// </summary>
    void ResetDeform();
}
