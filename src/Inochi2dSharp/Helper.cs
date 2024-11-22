using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp;

public static class Helper
{
    public static bool IsFinite(this Vector2 vector)
    {
        for (int a = 0; a < 2; a++)
        {
            if (!vector[a].IsFinite())
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsFinite(this float vector)
    {
        if (float.IsNaN(vector) || float.IsInfinity(vector))
        {
            return false;
        }

        return true;
    }
}
