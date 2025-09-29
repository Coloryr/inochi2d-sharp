using System.Numerics;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// A camera
/// </summary>
public abstract class Camera
{
    /// <summary>
    /// Size of the camera's viewport.
    /// </summary>
    public Vector2 Size = new(0, 0);
    /// <summary>
    /// The view-projection matrix for the camera.
    /// </summary>
    public abstract Matrix4x4 Matrix { get; }
    /// <summary>
    /// Updates the state of the camera.
    /// </summary>
    public abstract void Update();
}
