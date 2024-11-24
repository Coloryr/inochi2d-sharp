namespace Inochi2dSharp.Math;

public struct Vector2Int(int x, int y)
{
    public int X = x;
    public int Y = y;

    public Vector2Int(int value) : this(value, value)
    {

    }

    public override readonly string ToString()
    {
        return $"({X}, {Y})";
    }

    public static Vector2Int operator +(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X + b.X, a.Y + b.Y);
    }

    public static Vector2Int operator -(Vector2Int a, Vector2Int b)
    {
        return new Vector2Int(a.X - b.X, a.Y - b.Y);
    }

    public static Vector2Int operator *(Vector2Int a, int scalar)
    {
        return new Vector2Int(a.X * scalar, a.Y * scalar);
    }

    public static Vector2Int operator /(Vector2Int a, int scalar)
    {
        if (scalar == 0)
            throw new DivideByZeroException("Cannot divide by zero.");

        return new Vector2Int(a.X / scalar, a.Y / scalar);
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not Vector2Int)
            return false;

        var vec = (Vector2Int)obj;
        return X == vec.X && Y == vec.Y;
    }

    public override readonly int GetHashCode()
    {
        return X.GetHashCode() ^ Y.GetHashCode();
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