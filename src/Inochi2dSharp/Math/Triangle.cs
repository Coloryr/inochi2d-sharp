using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core;

namespace Inochi2dSharp.Math;

public static class Triangle
{
    public static bool IsPointInTriangle(Vector2 pt, Vector2[] triangle)
    {

        var p1 = triangle[0];
        var p2 = triangle[1];
        var p3 = triangle[2];

        var d1 = Sign(pt, p1, p2);
        var d2 = Sign(pt, p2, p3);
        var d3 = Sign(pt, p3, p1);

        var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }

    public static int[]? FindSurroundingTriangle(Vector2 pt, MeshData bindingMesh)
    {

        int i = 0;
        int[] triangle = [0, 1, 2];
        while (i < bindingMesh.Indices.Count)
        {
            triangle[0] = bindingMesh.Indices[i];
            triangle[1] = bindingMesh.Indices[i + 1];
            triangle[2] = bindingMesh.Indices[i + 2];
            if (IsPointInTriangle(bindingMesh, pt, triangle))
            {
                return triangle;
            }
            i += 3;
        }
        return null;
    }

    private static float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
    {
        return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
    }

    private static bool IsPointInTriangle(MeshData bindingMesh, Vector2 pt, int[] triangle)
    {
        var p1 = bindingMesh.Vertices[triangle[0]];
        var p2 = bindingMesh.Vertices[triangle[1]];
        var p3 = bindingMesh.Vertices[triangle[2]];

        var d1 = Sign(pt, p1, p2);
        var d2 = Sign(pt, p2, p3);
        var d3 = Sign(pt, p3, p1);

        var hasNeg = (d1 < 0) || (d2 < 0) || (d3 < 0);
        var hasPos = (d1 > 0) || (d2 > 0) || (d3 > 0);

        return !(hasNeg && hasPos);
    }
}
