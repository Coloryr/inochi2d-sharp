using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Math;

public struct Vector2Uint(uint x, uint y)
{
    public uint x = x;
    public uint y = y;

    public Vector2Uint(uint value) : this(value, value)
    {

    }

    public override readonly string ToString()
    {
        return $"({x}, {y})";
    }

    public static Vector2Uint operator +(Vector2Uint a, Vector2Uint b)
    {
        return new Vector2Uint(a.x + b.x, a.y + b.y);
    }

    public static Vector2Uint operator -(Vector2Uint a, Vector2Uint b)
    {
        return new Vector2Uint(a.x - b.x, a.y - b.y);
    }

    public static Vector2Uint operator *(Vector2Uint a, uint scalar)
    {
        return new Vector2Uint(a.x * scalar, a.y * scalar);
    }

    public static Vector2Uint operator /(Vector2Uint a, uint scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Vector2Uint(a.x / scalar, a.y / scalar);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not Vector2Uint)
            return false;

        var vec = (Vector2Uint)obj;
        return x == vec.x && y == vec.y;
    }

    public override readonly int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }

    public static bool operator ==(Vector2Uint a, Vector2Uint b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Vector2Uint a, Vector2Uint b)
    {
        return !a.Equals(b);
    }

    /// <summary>
    /// 向量的长度平方
    /// </summary>
    /// <returns></returns>
    public readonly uint SqrMagnitude()
    {
        return x * x + y * y;
    }

    /// <summary>
    /// 向量的长度
    /// </summary>
    /// <returns></returns>
    public readonly double Magnitude()
    {
        return System.Math.Sqrt(SqrMagnitude());
    }
}
