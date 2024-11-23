using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes;

public record Deformation
{
    private List<Vector2> vertexOffsets;

    public Deformation()
    {
        vertexOffsets = [];
    }

    public Deformation(Deformation rhs)
    {
        vertexOffsets = [.. rhs.vertexOffsets];
    }

    public Deformation(Vector2[] data)
    {
        Update(data);
    }

    public Deformation(List<Vector2> data)
    {
        vertexOffsets = [.. data];
    }

    public void Update(Vector2[] points)
    {
        vertexOffsets.Clear();
        vertexOffsets.AddRange(points);
    }

    public void Clear(int length)
    {
        vertexOffsets = [.. new Vector2[length]];
    }

    public void serialize(JArray serializer)
    {
        foreach (var offset in vertexOffsets)
        {
            serializer.Add(offset.ToToken());
        }
    }

    public void deserializeFromFghj(JArray data)
    {
        foreach (var elem in data)
        {
            vertexOffsets.Add(elem.ToVector2());
        }
    }

    public static Deformation operator -(Deformation v)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(-offset);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.vertexOffsets.Count; i++)
        {
            newDeformation.vertexOffsets.Add(v.vertexOffsets[i] * other.vertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(offset * other);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(offset * scalar);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.vertexOffsets.Count; i++)
        {
            newDeformation.vertexOffsets.Add(v.vertexOffsets[i] + other.vertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(offset + other);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(new Vector2(offset.X + scalar, offset.Y + scalar));
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.vertexOffsets.Count; i++)
        {
            newDeformation.vertexOffsets.Add(v.vertexOffsets[i] - other.vertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(offset - other);
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.vertexOffsets)
        {
            newDeformation.vertexOffsets.Add(new Vector2(offset.X - scalar, offset.Y - scalar));
        }
        return newDeformation;
    }
}