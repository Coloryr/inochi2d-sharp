using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Math;

public struct Vector2Int(int x, int y)
{
    public int x = x;
    public int y = y;

    public Vector2Int(int value) : this(value, value)
    { 
        
    }

    public override readonly string ToString()
    {
        return $"({x}, {y})";
    }

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x + b.x, a.y + b.y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.x - b.x, a.y - b.y);
    }

    public static Vector2Int operator *(Vector2Int a, int scalar)
    {
        return new Vector2Int(a.x * scalar, a.y * scalar);
    }

    public static Vector2Int operator /(Vector2Int a, int scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Vector2Int(a.x / scalar, a.y / scalar);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not Vector2Int)
            return false;

        var vec = (Vector2Int)obj;
        return x == vec.x && y == vec.y;
    }

    public override readonly int GetHashCode()
    {
        return x.GetHashCode() ^ y.GetHashCode();
    }

    public static bool operator ==(Vector2Int a, Vector2Int b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Vector2Int a, Vector2Int b)
    {
        return !a.Equals(b);
    }

    /// <summary>
    /// 向量的长度平方
    /// </summary>
    /// <returns></returns>
    public readonly int SqrMagnitude()
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