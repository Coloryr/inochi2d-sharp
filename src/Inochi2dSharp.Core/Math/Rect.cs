using System.Numerics;

namespace Inochi2dSharp.Core.Math;

public struct Rect
{
    public float X;
    public float Y;
    public float Width;
    public float Height;

    public Rect()
    {

    }

    public Rect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float Left()
    {
        return X;
    }

    public float Right()
    {
        return X + Width;
    }

    public float Top()
    {
        return Y;
    }

    public float Bottom()
    {
        return Y + Height;
    }

    public readonly double GetArea()
    {
        return Width * Height;
    }

    public readonly double GetPerimeter()
    {
        return 2 * (Width + Height);
    }

    // 重写ToString方法，便于输出
    public override readonly string ToString()
    {
        return $"Rect [X={X}, Y={Y}, Width={Width}, Height={Height}]";
    }

    /// <summary>
    /// Calculates bounding box of a mesh.
    /// </summary>
    /// <param name="mesh">The mesh to get the bounds for.</param>
    /// <returns>A rectangle enclosing the mesh.</returns>
    public static Rect GetBounds(Vector2[] mesh)
    {
        var minp = new Vector2(float.MaxValue, float.MaxValue);
        var maxp = new Vector2(-float.MaxValue, -float.MaxValue);

        foreach (var item in mesh)
        {
            minp.X = float.Min(minp.X, item.X);
            minp.Y = float.Min(minp.Y, item.Y);

            maxp.X = float.Max(maxp.X, item.X);
            maxp.Y = float.Max(maxp.Y, item.Y);
        }
        return new Rect(minp.X, minp.Y, maxp.X - minp.X, maxp.Y - minp.Y);
    }
}
