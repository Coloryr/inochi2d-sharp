using System.Numerics;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Math;

public static class MathHelper
{
    /// <summary>
    /// Smoothly dampens from a position to a target
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="target"></param>
    /// <param name="delta"></param>
    /// <param name="speed"></param>
    /// <returns></returns>
    public static float Dampen(float pos, float target, double delta, double speed = 1)
    {
        return (pos - target) * MathF.Pow(0.001f, (float)(delta * speed)) + target;
    }

    /// <summary>
    /// Gets whether a point is within an axis aligned rectangle
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public static bool Contains(Vector4 a, Vector2 b)
    {
        return b.X >= a.X &&
                b.Y >= a.Y &&
                b.X <= a.X + a.Z &&
                b.Y <= a.Y + a.W;
    }

    /// <summary>
    /// Checks if 2 lines segments are intersecting
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <param name="p3"></param>
    /// <param name="p4"></param>
    /// <returns></returns>
    public static bool AreLineSegmentsIntersecting(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float epsilon = 0.00001f;
        float demoninator = (p4.Y - p3.Y) * (p2.X - p1.X) - (p4.X - p3.X) * (p2.Y - p1.Y);
        if (demoninator == 0) return false;

        float uA = ((p4.X - p3.X) * (p1.Y - p3.Y) - (p4.Y - p3.Y) * (p1.X - p3.X)) / demoninator;
        float uB = ((p2.X - p1.X) * (p1.Y - p3.Y) - (p2.Y - p1.Y) * (p1.X - p3.X)) / demoninator;
        return (uA > 0 + epsilon && uA < 1 - epsilon && uB > 0 + epsilon && uB < 1 - epsilon);
    }

    public static Matrix4x4 Copy(this Matrix4x4 matrix)
    {
        var temp = new Matrix4x4();
        for (int a = 0; a < 4; a++)
        {
            for (int b = 0; b < 4; b++)
            {
                temp[a, b] = matrix[a, b];
            }
        }
        return temp;
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

        return (a * (t * t * t) + b * (t * t) + c * t + d);
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

        float cosamt = MathF.Cos(alpha);
        float sinamt = MathF.Sin(alpha);

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
        ret[2, 2] = y;

        return ret;
    }
}
