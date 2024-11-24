namespace Inochi2dSharp.Math;

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
}
