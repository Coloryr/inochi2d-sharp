using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Math;

public struct Matrix2x2
{
    public float m00, m10; // 第一列
    public float m01, m11; // 第二列

    // 单位矩阵
    public static readonly Matrix2x2 Identity = new(1f, 0f, 0f, 1f);

    // 零矩阵
    public static readonly Matrix2x2 Zero = new(0f, 0f, 0f, 0f);

    // 构造函数
    public Matrix2x2(float m00, float m01, float m10, float m11)
    {
        this.m00 = m00; this.m01 = m01;
        this.m10 = m10; this.m11 = m11;
    }

    // 通过对角线值构造
    public Matrix2x2(float diagonal)
    {
        m00 = diagonal; m01 = 0f;
        m10 = 0f; m11 = diagonal;
    }

    // 通过两个向量构造（列向量）
    public Matrix2x2(Vector2 column0, Vector2 column1)
    {
        m00 = column0.X; m01 = column1.X;
        m10 = column0.Y; m11 = column1.Y;
    }

    // 矩阵加法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator +(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a.m00 + b.m00, a.m01 + b.m01,
            a.m10 + b.m10, a.m11 + b.m11
        );
    }

    // 矩阵减法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator -(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a.m00 - b.m00, a.m01 - b.m01,
            a.m10 - b.m10, a.m11 - b.m11
        );
    }

    // 矩阵乘法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator *(Matrix2x2 a, Matrix2x2 b)
    {
        return new Matrix2x2(
            a.m00 * b.m00 + a.m01 * b.m10,
            a.m00 * b.m01 + a.m01 * b.m11,
            a.m10 * b.m00 + a.m11 * b.m10,
            a.m10 * b.m01 + a.m11 * b.m11
        );
    }

    // 矩阵标量乘法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator *(Matrix2x2 m, float scalar)
    {
        return new Matrix2x2(
            m.m00 * scalar, m.m01 * scalar,
            m.m10 * scalar, m.m11 * scalar
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator *(float scalar, Matrix2x2 m) => m * scalar;

    // 矩阵向量乘法
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 operator *(Matrix2x2 m, Vector2 v)
    {
        return new Vector2(
            m.m00 * v.X + m.m01 * v.Y,
            m.m10 * v.X + m.m11 * v.Y
        );
    }

    // 矩阵除法（标量）
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix2x2 operator /(Matrix2x2 m, float scalar)
    {
        float invScalar = 1f / scalar;
        return m * invScalar;
    }
}
