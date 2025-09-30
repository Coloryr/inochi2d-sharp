using System.Numerics;

namespace Inochi2dSharp.Core.Math;

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

    public static Vector2 GetXY(this Vector3 vec3)
    {
        return new Vector2(vec3.X, vec3.Y);
    }

    public static Vector2 GetXY(this Vector4 vec3)
    {
        return new Vector2(vec3.X, vec3.Y);
    }

    public static bool IsFinite(this Vector2 vec2)
    {
        return float.IsFinite(vec2.X) && float.IsFinite(vec2.Y);
    }

    public static int[]? FindSurroundingTriangle(Vector2 pt, MeshData bindingMesh)
    {
        bool IsPointInTriangle(Vector2 pt, int[] triangle)
        {
            float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
            {
                return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
            }
            var p1 = bindingMesh.Vertices[triangle[0]];
            var p2 = bindingMesh.Vertices[triangle[1]];
            var p3 = bindingMesh.Vertices[triangle[2]];

            var d1 = Sign(pt, p1, p2);
            var d2 = Sign(pt, p2, p3);
            var d3 = Sign(pt, p3, p1);

            var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
            var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

            return !(hasNeg && hasPos);
        }
        int i = 0;
        int[] triangle = [0, 1, 2];
        while (i < bindingMesh.Indices.Length)
        {
            triangle[0] = (int)bindingMesh.Indices[i];
            triangle[1] = (int)bindingMesh.Indices[i + 1];
            triangle[2] = (int)bindingMesh.Indices[i + 2];
            if (IsPointInTriangle(pt, triangle))
            {
                return triangle;
            }
            i += 3;
        }
        return null;
    }

    /// <summary>
    /// Calculate offset of point in coordinates of triangle.
    /// </summary>
    /// <param name="pt"></param>
    /// <param name="bindingMesh"></param>
    /// <param name="triangle"></param>
    /// <returns></returns>
    public static Vector2 CalcOffsetInTriangleCoords(Vector2 pt, MeshData bindingMesh, int[] triangle)
    {
        if ((pt - bindingMesh.Vertices[triangle[0]]).LengthSquared() >
            (pt - bindingMesh.Vertices[triangle[1]]).LengthSquared())
        {
            (triangle[0], triangle[1]) = (triangle[1], triangle[0]);
        }
        if ((pt - bindingMesh.Vertices[triangle[0]]).LengthSquared() >
            (pt - bindingMesh.Vertices[triangle[2]]).LengthSquared())
        {
            (triangle[0], triangle[2]) = (triangle[2], triangle[0]);
        }
        var p1 = bindingMesh.Vertices[triangle[0]];
        var p2 = bindingMesh.Vertices[triangle[1]];
        var p3 = bindingMesh.Vertices[triangle[2]];
        var axis0 = p2 - p1;
        //float axis0len = axis0.Length();
        axis0 /= axis0.Length();
        var axis1 = p3 - p1;
        //float axis1len = axis1.Length();
        axis1 /= axis1.Length();

        var relPt = pt - p1;
        if (relPt.LengthSquared() == 0)
        {
            return new Vector2(0, 0);
        }
        float cosA = Vector2.Dot(axis0, axis1);
        if (cosA == 0)
        {
            return new Vector2(Vector2.Dot(relPt, axis0), Vector2.Dot(relPt, axis1));
        }
        else
        {
            float argA = MathF.Acos(cosA);
            float sinA = MathF.Sin(argA);
            float tanA = MathF.Tan(argA);
            float cosB = Vector2.Dot(axis0, relPt) / relPt.Length();
            float argB = MathF.Acos(cosB);
            float sinB = MathF.Sin(argB);

            var ortPt = new Vector2(relPt.Length() * cosB, relPt.Length() * sinB);

            var H = new Matrix2x2(1, -1 / tanA, 0, 1 / sinA);
            var result = H * ortPt;

            return result;
        }
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
}
