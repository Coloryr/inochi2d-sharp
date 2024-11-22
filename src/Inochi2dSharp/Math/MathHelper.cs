using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Math;

public static class MathHelper
{
    /// <summary>
    /// Smoothly dampens from a position to a target
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="target"></param>
    /// <param name="delta"></param>
    /// <param name="speed"></param>
    /// <returns></returns>
    public static float Dampen(float pos, float target, double delta, double speed = 1)
    {
        return (pos - target) * MathF.Pow(0.001f, (float)(delta * speed)) + target;
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
}
