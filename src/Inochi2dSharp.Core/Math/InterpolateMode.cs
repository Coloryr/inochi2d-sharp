using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Math;

/// <summary>
/// Different modes of interpolation between values.
/// </summary>
public enum InterpolateMode : uint
{
    /// <summary>
    /// Round to nearest
    /// </summary>
    Nearest = 0,
    /// <summary>
    /// Linear interpolation
    /// </summary>
    Linear = 1,
    /// <summary>
    /// Round to nearest
    /// </summary>
    Stepped = 2,
    /// <summary>
    /// Interpolation using quadratic interpolation
    /// </summary>
    Quadratic = 3,
    /// <summary>
    /// Cubic interpolation
    /// </summary>
    Cubic = 4
}

public static class InterpolateModeHelper
{
    /// <summary>
    /// Converts a string key into a interpolation mode.
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public static InterpolateMode ToInterpolateMode(this string key)
    {
        return key switch
        {
            "nearest" or "Nearest" => InterpolateMode.Nearest,
            "linear" or "Linear" => InterpolateMode.Linear,
            "stepped" or "Stepped" => InterpolateMode.Stepped,
            "bezier" or "Bezier" or "quadratic" => InterpolateMode.Quadratic,
            "cubic" or "Cubic" => InterpolateMode.Cubic,
            _ => InterpolateMode.Linear,
        };
    }
}