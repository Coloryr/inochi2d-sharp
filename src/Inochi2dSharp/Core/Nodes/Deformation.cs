﻿using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Nodes;

public record Deformation
{
    public List<Vector2> VertexOffsets;

    public Deformation()
    {
        VertexOffsets = [];
    }

    public Deformation(Deformation rhs)
    {
        VertexOffsets = [.. rhs.VertexOffsets];
    }

    public Deformation(Vector2[] data)
    {
        Update(data);
    }

    public Deformation(List<Vector2> data)
    {
        VertexOffsets = [.. data];
    }

    public void Update(Vector2[] points)
    {
        VertexOffsets.Clear();
        VertexOffsets.AddRange(points);
    }

    public void Clear(int length)
    {
        VertexOffsets = [.. new Vector2[length]];
    }

    public void Serialize(JsonArray serializer)
    {
        foreach (var offset in VertexOffsets)
        {
            serializer.Add(offset.ToToken());
        }
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var elem in data.EnumerateArray())
        {
            VertexOffsets.Add(elem.ToVector2());
        }
    }

    public static Deformation operator -(Deformation v)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(-offset);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.VertexOffsets.Count; i++)
        {
            newDeformation.VertexOffsets.Add(v.VertexOffsets[i] * other.VertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(offset * other);
        }
        return newDeformation;
    }

    public static Deformation operator *(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(offset * scalar);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.VertexOffsets.Count; i++)
        {
            newDeformation.VertexOffsets.Add(v.VertexOffsets[i] + other.VertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(offset + other);
        }
        return newDeformation;
    }

    public static Deformation operator +(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(new Vector2(offset.X + scalar, offset.Y + scalar));
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, Deformation other)
    {
        var newDeformation = new Deformation();
        for (int i = 0; i < v.VertexOffsets.Count; i++)
        {
            newDeformation.VertexOffsets.Add(v.VertexOffsets[i] - other.VertexOffsets[i]);
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, Vector2 other)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(offset - other);
        }
        return newDeformation;
    }

    public static Deformation operator -(Deformation v, float scalar)
    {
        var newDeformation = new Deformation();
        foreach (var offset in v.VertexOffsets)
        {
            newDeformation.VertexOffsets.Add(new Vector2(offset.X - scalar, offset.Y - scalar));
        }
        return newDeformation;
    }
}