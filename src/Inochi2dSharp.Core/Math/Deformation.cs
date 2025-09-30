using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// A deformation
/// </summary>
public record Deformation
{
    public Vector2[] VertexOffsets;

    private Deformation(int size)
    {
        VertexOffsets = new Vector2[size];
    }

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
        VertexOffsets = [.. points];
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
        var list = new List<Vector2>();
        foreach (var elem in data.EnumerateArray())
        {
            list.Add(elem.ToVector2());
        }
        VertexOffsets = [.. list];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator -(Deformation v)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int a = 0; a < v.VertexOffsets.Length; a++)
        {
            def.VertexOffsets[a] = -v.VertexOffsets[a];
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator *(Deformation v, Deformation other)
    {
        if (v.VertexOffsets.Length > other.VertexOffsets.Length)
        {
            throw new Exception("size bigger than other");
        }
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] * other.VertexOffsets[i];
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator *(Deformation v, Vector2 other)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] * other;
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator *(Deformation v, float scalar)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] * scalar;
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator +(Deformation v, Deformation other)
    {
        if (v.VertexOffsets.Length > other.VertexOffsets.Length)
        {
            throw new Exception("size bigger than other");
        }
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] + other.VertexOffsets[i];
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator +(Deformation v, Vector2 other)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] + other;
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator +(Deformation v, float scalar)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = new Vector2(v.VertexOffsets[i].X + scalar, v.VertexOffsets[i].Y + scalar);
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator -(Deformation v, Deformation other)
    {
        if (v.VertexOffsets.Length > other.VertexOffsets.Length)
        {
            throw new Exception("size bigger than other");
        }
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] - other.VertexOffsets[i];
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator -(Deformation v, Vector2 other)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = v.VertexOffsets[i] - other;
        }
        return def;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Deformation operator -(Deformation v, float scalar)
    {
        var def = new Deformation(v.VertexOffsets.Length);
        for (int i = 0; i < v.VertexOffsets.Length; i++)
        {
            def.VertexOffsets[i] = new Vector2(v.VertexOffsets[i].X - scalar, v.VertexOffsets[i].Y - scalar);
        }
        return def;
    }

    public static Deformation Lerp(Deformation value1, Deformation value2, float amount) => (value1 * (1.0f - amount)) + (value2 * amount);

    public static Deformation Cubic(Deformation p0, Deformation p1, Deformation p2, Deformation p3, float t)
    {
        // Assuming T supports multiplication and addition with float
        Deformation dp0 = p0, dp1 = p1, dp2 = p2, dp3 = p3;

        Deformation a = dp0 * -0.5f + dp1 * 1.5f - dp2 * 1.5f + dp3 * 0.5f;
        Deformation b = dp0 - dp1 * 2.5f + dp2 * 2f - dp3 * 0.5f;
        Deformation c = dp0 * -0.5f + dp2 * 0.5f;
        Deformation d = dp1;

        return a * (t * t * t) + b * (t * t) + c * t + d;
    }
}