namespace Inochi2dSharp.Math;

public struct Vector2Uint(uint x, uint y)
{
    public uint X = x;
    public uint Y = y;

    public Vector2Uint(uint value) : this(value, value)
    {

    }

    public override readonly string ToString()
    {
        return $"({X}, {Y})";
    }

    public static Vector2Uint operator +(Vector2Uint a, Vector2Uint b)
    {
        return new Vector2Uint(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2Uint operator -(Vector2Uint a, Vector2Uint b)
    {
        return new Vector2Uint(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2Uint operator *(Vector2Uint a, uint scalar)
    {
        return new Vector2Uint(a.X * scalar, a.Y * scalar);
    }

    public static Vector2Uint operator /(Vector2Uint a, uint scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Vector2Uint(a.X / scalar, a.Y / scalar);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not Vector2Uint)
            return false;

        var vec = (Vector2Uint)obj;
        return X == vec.X && Y == vec.Y;
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
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
        return X * X + Y * Y;
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
