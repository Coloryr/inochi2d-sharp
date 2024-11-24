using System.Numerics;
using Inochi2dSharp.Core;

namespace Inochi2dSharp.Math;

/// <summary>
/// An orthographic camera
/// </summary>
public class Camera
{
    private Matrix4x4 _projection;

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
        CoreHelper.inGetViewport(out var width, out var height);

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

        Vector2 realSize = GetRealSize();
        if (!realSize.IsFinite()) return Matrix4x4.Identity;

        Vector2 origin = new Vector2(realSize.X / 2, realSize.Y / 2);
        Vector3 pos = new Vector3(Position.X, Position.Y, -(ushort.MaxValue / 2));

        return
            Matrix4x4.CreateOrthographicOffCenter(0f, realSize.X, realSize.Y, 0, 0, ushort.MaxValue) *
            Matrix4x4.CreateTranslation(origin.X, origin.Y, 0) *
            Matrix4x4.CreateRotationZ(Rotation) *
            Matrix4x4.CreateTranslation(pos);
    }
}
