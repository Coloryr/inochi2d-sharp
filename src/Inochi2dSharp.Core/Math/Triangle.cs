using System.Numerics;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// A 2D triangle
/// </summary>
public struct Triangle
{
    public Vector2 P1;
    public Vector2 P2;
    public Vector2 P3;

    /// <summary>
    /// Gets the barycentric coordinates of the given point.
    /// </summary>
    /// <param name="pt">The point to check.</param>
    /// <returns>The barycentric coordinates in relation to each vertex of the triangle.</returns>
    public readonly Vector3 Barycentric(Vector2 pt)
    {
        Vector2 v1 = P2 - P1;
        Vector2 v2 = P3 - P1;
        Vector2 v3 = pt - P1;
        float den = v1.X * v2.Y - v2.X * v1.Y;
        float v = (v3.X * v2.Y - v2.X * v3.Y) / den;
        float w = (v1.X * v3.Y - v3.X * v1.Y) / den;
        return new Vector3(1.0f - v - w, v, w);
    }

    /// <summary>
    /// Whether the triangle contains the given point.
    /// </summary>
    /// <param name="pt">The point to check.</param>
    /// <returns><see langword="true" /> if the given point lies within this triangle, <br/><see langword="false" /> otherwise.</returns>
    public bool Contains(Vector2 pt)
    {
        float d1 = Sign(ref pt, ref P1, ref P2);
        float d2 = Sign(ref pt, ref P2, ref P3);
        float d3 = Sign(ref pt, ref P3, ref P1);
        return !(
            (d1 < 0 || d2 < 0 || d3 < 0) &&
            (d1 > 0 || d2 > 0 || d3 > 0)
        );
    }

    /// <summary>
    /// Gets the sign between 3 points.
    /// </summary>
    /// <param name="p1">The first point</param>
    /// <param name="p2">The second point</param>
    /// <param name="p3">The third point.</param>
    /// <returns>A float determining the sign between p1, p2 and p3.</returns>
    private static float Sign(ref Vector2 p1, ref Vector2 p2, ref Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }
}
