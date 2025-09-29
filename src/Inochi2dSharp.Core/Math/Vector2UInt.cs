namespace Inochi2dSharp.Core.Math;

public struct Vector2UInt(uint x, uint y)
{
    public uint X = x;
    public uint Y = y;

    public Vector2UInt(uint value) : this(value, value)
    {

    }

    public override readonly string ToString()
    {
        return $"({X}, {Y})";
    }

    public static Vector2UInt operator +(Vector2UInt a, Vector2UInt b)
    {
        return new Vector2UInt(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2UInt operator -(Vector2UInt a, Vector2UInt b)
    {
        return new Vector2UInt(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2UInt operator *(Vector2UInt a, uint scalar)
    {
        return new Vector2UInt(a.X * scalar, a.Y * scalar);
    }

    public static Vector2UInt operator /(Vector2UInt a, uint scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Vector2UInt(a.X / scalar, a.Y / scalar);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not Vector2UInt)
            return false;

        var vec = (Vector2UInt)obj;
        return X == vec.X && Y == vec.Y;
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
    }

    public static bool operator ==(Vector2UInt a, Vector2UInt b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Vector2UInt a, Vector2UInt b)
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
