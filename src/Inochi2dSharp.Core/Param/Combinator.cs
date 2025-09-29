using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Param;

public record Combinator
{
    public Vector2[] Ivalues;
    public float[] Iweights;
    public int Isum;

    public void Clear()
    {
        Isum = 0;
    }

    public void Resize(int reqLength)
    {
        Ivalues = new Vector2[reqLength];
        Iweights = new float[reqLength];
    }

    public void Add(Vector2 value, float weight)
    {
        if (Isum >= Ivalues.Length) Resize(Isum + 8);

        Ivalues[Isum] = value;
        Iweights[Isum] = weight;
        Isum++;
    }

    public void Add(int axis, float value, float weight)
    {
        if (Isum >= Ivalues.Length) Resize(Isum + 8);

        Ivalues[Isum] = new Vector2(axis == 0 ? value : 1, axis == 1 ? value : 1);
        Iweights[Isum] = weight;
        Isum++;
    }

    public Vector2 Csum()
    {
        var val = new Vector2(0, 0);
        for (int i = 0; i < Isum; i++)
        {
            val += Ivalues[i];
        }
        return val;
    }

    public Vector2 Avg()
    {
        if (Isum == 0) return new Vector2(1, 1);

        var val = new Vector2(0, 0);
        for (int i = 0; i < Isum; i++)
        {
            val += Ivalues[i] * Iweights[i];
        }
        return val / Isum;
    }
}
