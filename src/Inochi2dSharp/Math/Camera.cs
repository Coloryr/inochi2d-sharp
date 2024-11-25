using System.Numerics;
using Inochi2dSharp.Core;

namespace Inochi2dSharp.Math;

/// <summary>
/// An orthographic camera
/// </summary>
public class Camera(I2dCore core)
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

        var temp1 = ort(0f, realSize.X, realSize.Y, 0, 0, ushort.MaxValue);
        var temp4 = Matrix4x4.CreateTranslation(origin.X, origin.Y, 0);
        var temp5 = Matrix4x4.CreateRotationZ(Rotation);
        var temp2 = temp1 * temp4 * temp5;
        var temp3 = temp2 * Matrix4x4.CreateTranslation(pos);

        return temp3;
    }

    private static Matrix4x4 ort(float left, float right, float bottom, float top, float near, float far)
    {
        Matrix4x4 ret = new();

        ret[0,0] = 2 / (right - left);
        ret[0,3] = -(right + left) / (right - left);
        ret[1,1] = 2 / (top - bottom);
        ret[1,3] = -(top + bottom) / (top - bottom);
        ret[2,2] = -2 / (far - near);
        ret[2,3] = -(far + near) / (far - near);
        ret[3,3] = 1;

        return ret;
    }
}
