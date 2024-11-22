using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Param;

public record Combinator
{
    public Vector2[] ivalues;
    public float[] iweights;
    public int isum;

    public void clear()
    {
        isum = 0;
    }

    public void Resize(int reqLength)
    {
        ivalues = new Vector2[reqLength];
        iweights = new float[reqLength];
    }

    public void Add(Vector2 value, float weight)
    {
        if (isum >= ivalues.Length) Resize(isum + 8);

        ivalues[isum] = value;
        iweights[isum] = weight;
        isum++;
    }

    public void Add(int axis, float value, float weight)
    {
        if (isum >= ivalues.Length) Resize(isum + 8);

        ivalues[isum] = new Vector2(axis == 0 ? value : 1, axis == 1 ? value : 1);
        iweights[isum] = weight;
        isum++;
    }

    public Vector2 Csum()
    {
        var val = new Vector2(0, 0);
        for (int i = 0; i < isum; i++)
        {
            val += ivalues[i];
        }
        return val;
    }

    public Vector2 Avg()
    {
        if (isum == 0) return new Vector2(1, 1);

        var val = new Vector2(0, 0);
        for (int i = 0; i < isum; i++)
        {
            val += ivalues[i] * iweights[i];
        }
        return val / isum;
    }
}
