using System.Numerics;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// An orthographic camera
/// </summary>
public class Camera2D : Camera
{
    private Matrix4x4 projection;

    /// <summary>
    /// Position of camera
    /// </summary>
    public Vector2 Position = new(0, 0);
    /// <summary>
    /// Rotation of the camera
    /// </summary>
    public float Rotation = 0f;
    /// <summary>
    /// Scale to apply to the camera's viewport.
    /// </summary>
    public float Scale = 0f;
    /// <summary>
    /// Gets the center offset of the camera
    /// </summary>
    public Vector2 CenterOffset => Size / 2.0f;
    /// <summary>
    /// Matrix for this camera
    /// </summary>
    public override Matrix4x4 Matrix => projection;

    /// <summary>
    /// Updates the state of the camera.
    /// </summary>
    public override void Update()
    {
        if (!Position.IsFinite()) Position = new Vector2(0);
        if (!float.IsFinite(Scale)) Scale = 1;
        if (!float.IsFinite(Rotation)) Rotation = 0;

        var origin = new Vector2(Size.X / 2, Size.Y / 2);
        var pos = new Vector3(Position.X, Position.Y, -(ushort.MaxValue / 2));

        projection = MathHelper.Orthographic(0f, Size.X, Size.Y, 0, 0, ushort.MaxValue)
            * MathHelper.Translation(origin.X, origin.Y, 0)
            * MathHelper.ZRotation(Rotation)
            * MathHelper.Translation(pos);
    }
}
