using System.Numerics;

namespace Inochi2dSharp.Math;

/// <summary>
/// An orthographic camera
/// </summary>
public class Camera(I2dCore core)
{
    /// <summary>
    /// Position of camera
    /// </summary>
    public Vector2 Position = new(0, 0);

    /// <summary>
    /// Rotation of the camera
    /// </summary>
    public float Rotation = 0f;

    /// <summary>
    /// Size of the camera
    /// </summary>
    public Vector2 Scale = new(1, 1);

    public Vector2 GetRealSize()
    {
        core.InGetViewport(out var width, out var height);

        return new(width / Scale.X, height / Scale.Y);
    }

    public Vector2 GetCenterOffset()
    {
        Vector2 realSize = GetRealSize();
        return realSize / 2;
    }

    /// <summary>
    /// Matrix for this camera
    /// 
    /// width = width of camera area
    /// height = height of camera area
    /// </summary>
    /// <returns></returns>
    public Matrix4x4 Matrix()
    {
        if (!Position.IsFinite()) Position = new Vector2(0);
        if (!Scale.IsFinite()) Scale = new Vector2(1);
        if (!Rotation.IsFinite()) Rotation = 0;

        var realSize = GetRealSize();
        if (!realSize.IsFinite()) return Matrix4x4.Identity;

        var origin = new Vector2(realSize.X / 2, realSize.Y / 2);
        var pos = new Vector3(Position.X, Position.Y, -(ushort.MaxValue / 2));

        var temp = MathHelper.Orthographic(0f, realSize.X, realSize.Y, 0, 0, ushort.MaxValue);
        var temp1 = MathHelper.Translation(origin.X, origin.Y, 0);
        var temp2 = MathHelper.ZRotation(Rotation);
        var temp3 = MathHelper.Translation(pos);

        return temp * temp1 * temp2 * temp3;
    }
}
