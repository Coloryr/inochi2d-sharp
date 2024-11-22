using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Math;

public struct Matrix3x3
{
    public float M11, M12, M13;
    public float M21, M22, M23;
    public float M31, M32, M33;

    public Matrix3x3()
    {
        M11 = 1;
        M22 = 1;
        M33 = 1;
    }

    public Matrix3x3(float m11, float m12, float m13,
                     float m21, float m22, float m23,
                     float m31, float m32, float m33)
    {
        M11 = m11; M12 = m12; M13 = m13;
        M21 = m21; M22 = m22; M23 = m23;
        M31 = m31; M32 = m32; M33 = m33;
    }

    public float this[int row, int col]
    {
        readonly get
        {
            return (row, col) switch
            {
                (0, 0) => M11,
                (0, 1) => M12,
                (0, 2) => M13,
                (1, 0) => M21,
                (1, 1) => M22,
                (1, 2) => M23,
                (2, 0) => M31,
                (2, 1) => M32,
                (2, 2) => M33,
                _ => throw new IndexOutOfRangeException("Invalid matrix index")
            };
        }
        set
        {
            switch (row, col)
            {
                case (0, 0): M11 = value; break;
                case (0, 1): M12 = value; break;
                case (0, 2): M13 = value; break;
                case (1, 0): M21 = value; break;
                case (1, 1): M22 = value; break;
                case (1, 2): M23 = value; break;
                case (2, 0): M31 = value; break;
                case (2, 1): M32 = value; break;
                case (2, 2): M33 = value; break;
                default: throw new IndexOutOfRangeException("Invalid matrix index");
            }
        }
    }

    public static Matrix3x3 CreateTranslation(Vector3 translation)
    {
        return new Matrix3x3(
            1, 0, translation.X,
            0, 1, translation.Y,
            0, 0, 1
        );
    }

    public static Matrix3x3 CreateRotationZ(float angle)
    {
        float cos = MathF.Cos(angle);
        float sin = MathF.Sin(angle);

        return new Matrix3x3(
            cos, -sin, 0,
            sin, cos, 0,
            0, 0, 1
        );
    }

    public static Matrix3x3 CreateScale(float x, float y, float z)
    {
        return new Matrix3x3(
            x, 0, 0,
            0, y, 0,
            0, 0, z
        );
    }

    public readonly Matrix3x3 Transpose()
    {
        return new Matrix3x3(
            M11, M21, M31,
            M12, M22, M32,
            M13, M23, M33
        );
    }

    public readonly double Determinant()
    {
        return M11 * (M22 * M33 - M23 * M32)
             - M12 * (M21 * M33 - M23 * M31)
             + M13 * (M21 * M32 - M22 * M31);
    }

    public readonly override string ToString()
    {
        return $"[{M11}, {M12}, {M13}]\n" +
               $"[{M21}, {M22}, {M23}]\n" +
               $"[{M31}, {M32}, {M33}]";
    }

    public static Matrix3x3 operator +(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 + b.M11, a.M12 + b.M12, a.M13 + b.M13,
            a.M21 + b.M21, a.M22 + b.M22, a.M23 + b.M23,
            a.M31 + b.M31, a.M32 + b.M32, a.M33 + b.M33
        );
    }

    public static Matrix3x3 operator -(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 - b.M11, a.M12 - b.M12, a.M13 - b.M13,
            a.M21 - b.M21, a.M22 - b.M22, a.M23 - b.M23,
            a.M31 - b.M31, a.M32 - b.M32, a.M33 - b.M33
        );
    }

    public static Matrix3x3 operator *(Matrix3x3 a, Matrix3x3 b)
    {
        return new Matrix3x3(
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33,

            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33,

            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33
        );
    }

    public static Vector3 operator *(Matrix3x3 a, Vector3 b)
    {
        float x = a.M11 * b.X + a.M12 * b.Y + a.M13 * b.Z;
        float y = a.M21 * b.X + a.M22 * b.Y + a.M23 * b.Z;
        float z = a.M31 * b.X + a.M32 * b.Y + a.M33 * b.Z;
        return new Vector3(x, y, z);
    }

}
