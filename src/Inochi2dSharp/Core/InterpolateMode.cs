namespace Inochi2dSharp.Core;

/// <summary>
/// Different modes of interpolation between values.
/// </summary>
public enum InterpolateMode
{
    /// <summary>
    /// Round to nearest
    /// </summary>
    Nearest,
    /// <summary>
    /// Linear interpolation
    /// </summary>
    Linear,
    /// <summary>
    /// Round to nearest
    /// </summary>
    Stepped,
    /// <summary>
    /// Cubic interpolation
    /// </summary>
    Cubic,
    /// <summary>
    /// Interpolation using beziér splines
    /// </summary>
    Bezier,

    COUNT
}
