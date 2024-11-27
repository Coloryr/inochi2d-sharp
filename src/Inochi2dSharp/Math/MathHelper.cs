using System.Numerics;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Math;

public static class MathHelper
{
    /// <summary>
    /// Gets whether a point is within an axis aligned rectangle
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Contains(Vector2 min, Vector2 max, Vector2 value)
    {
        return max.X >= value.X &&
                max.Y >= value.Y &&
                min.X <= value.X &&
                min.Y <= value.Y;
    }

    /// <summary>
    /// 扩展方法用于矩阵左乘向量。
    /// </summary>
    /// <param name="matrix">4x4矩阵</param>
    /// <param name="vector">4维向量</param>
    /// <returns>变换后的4维向量</returns>
    public static Vector4 Multiply(this Matrix4x4 matrix, Vector4 vector)
    {
        // 手动实现矩阵左乘向量
        return new Vector4(
            matrix.M11 * vector.X + matrix.M12 * vector.Y + matrix.M13 * vector.Z + matrix.M14 * vector.W,
            matrix.M21 * vector.X + matrix.M22 * vector.Y + matrix.M23 * vector.Z + matrix.M24 * vector.W,
            matrix.M31 * vector.X + matrix.M32 * vector.Y + matrix.M33 * vector.Z + matrix.M34 * vector.W,
            matrix.M41 * vector.X + matrix.M42 * vector.Y + matrix.M43 * vector.Z + matrix.M44 * vector.W
        );
    }

    public static float Hermite(float x, float tx, float y, float ty, float t)
    {
        float h1 = 2 * t * t * t - 3 * t * t + 1;
        float h2 = -2 * t * t * t + 3 * t * t;
        float h3 = t * t * t - 2 * t * t + t;
        float h4 = t * t * t - t * t;

        // Assuming T supports multiplication and addition with float
        float dx = x, dtx = tx, dy = y, dty = ty;
        return h1 * dx + h3 * dtx + h2 * dy + h4 * dty;
    }

    public static float Cubic(float p0, float p1, float p2, float p3, float t)
    {
        // Assuming T supports multiplication and addition with float
        float dp0 = p0, dp1 = p1, dp2 = p2, dp3 = p3;

        float a = -0.5f * dp0 + 1.5f * dp1 - 1.5f * dp2 + 0.5f * dp3;
        float b = dp0 - 2.5f * dp1 + 2f * dp2 - 0.5f * dp3;
        float c = -0.5f * dp0 + 0.5f * dp2;
        float d = dp1;

        return (float)(a * (t * t * t) + b * (t * t) + c * t + d);
    }

    public static Deformation Lerp(Deformation value1, Deformation value2, float amount) => (value1 * (1.0f - amount)) + (value2 * amount);

    public static Deformation Cubic(Deformation p0, Deformation p1, Deformation p2, Deformation p3, float t)
    {
        // Assuming T supports multiplication and addition with float
        Deformation dp0 = p0, dp1 = p1, dp2 = p2, dp3 = p3;

        Deformation a = dp0 * -0.5f + dp1 * 1.5f - dp2 * 1.5f + dp3 * 0.5f;
        Deformation b = dp0 - dp1 * 2.5f + dp2 * 2f - dp3 * 0.5f;
        Deformation c = dp0 * -0.5f + dp2 * 0.5f;
        Deformation d = dp1;

        return a * (t * t * t) + b * (t * t) + c * t + d;
    }

    public static Matrix4x4 Orthographic(float left, float right, float bottom, float top, float near, float far)
    {
        var ret = new Matrix4x4();

        ret[0, 0] = 2 / (right - left);
        ret[0, 3] = -(right + left) / (right - left);
        ret[1, 1] = 2 / (top - bottom);
        ret[1, 3] = -(top + bottom) / (top - bottom);
        ret[2, 2] = -2 / (far - near);
        ret[2, 3] = -(far + near) / (far - near);
        ret[3, 3] = 1;

        return ret;
    }

    public static Matrix4x4 Translation(float x, float y, float z)
    {
        var ret = Matrix4x4.Identity;

        ret[0, 3] = x;
        ret[1, 3] = y;
        ret[2, 3] = z;

        return ret;
    }

    public static Matrix4x4 Translation(Vector3 vector)
    {
        return Translation(vector.X, vector.Y, vector.Z);
    }

    public static Matrix4x4 ZRotation(float alpha)
    {
        var ret = Matrix4x4.Identity;

        float cosamt = float.Cos(alpha);
        float sinamt = float.Sin(alpha);

        ret[0, 0] = cosamt;
        ret[0, 1] = -sinamt;
        ret[1, 0] = sinamt;
        ret[1, 1] = cosamt;

        return ret;
    }

    public static Matrix4x4 Scaling(float x, float y, float z)
    {
        var ret = Matrix4x4.Identity;

        ret[0, 0] = x;
        ret[1, 1] = y;
        ret[2, 2] = z;

        return ret;
    }

    public static Quaternion EulerRotation(float roll, float pitch, float yaw)
    {
        var ret = new Quaternion();

        float cr = MathF.Cos(roll / 2.0f);
        float cp = MathF.Cos(pitch / 2.0f);
        float cy = MathF.Cos(yaw / 2.0f);
        float sr = MathF.Sin(roll / 2.0f);
        float sp = MathF.Sin(pitch / 2.0f);
        float sy = MathF.Sin(yaw / 2.0f);

        ret.W = cr * cp * cy + sr * sp * sy;
        ret.X = sr * cp * cy - cr * sp * sy;
        ret.Y = cr * sp * cy + sr * cp * sy;
        ret.Z = cr * cp * sy - sr * sp * cy;

        return ret;
    }

    public static Matrix4x4 ToMatrix(this Quaternion quaternion)
    {
        float x = quaternion.X;
        float y = quaternion.Y;
        float z = quaternion.Z;
        float w = quaternion.W;

        var ret = Matrix4x4.Identity;
        float xx = float.Pow(x, 2);
        float xy = x * y;
        float xz = x * z;
        float xw = x * w;
        float yy = float.Pow(y, 2);
        float yz = y * z;
        float yw = y * w;
        float zz = float.Pow(z, 2);
        float zw = z * w;

        ret[0, 0] = 1 - 2 * (yy + zz);
        ret[0, 1] = 2 * (xy - zw);
        ret[0, 2] = 2 * (xz + yw);

        ret[1, 0] = 2 * (xy + zw);
        ret[1, 1] = 1 - 2 * (xx + zz);
        ret[1, 2] = 2 * (yz - xw);

        ret[2, 0] = 2 * (xz - yw);
        ret[2, 1] = 2 * (yz + xw);
        ret[2, 2] = 1 - 2 * (xx + yy);

        return ret;
    }
}
