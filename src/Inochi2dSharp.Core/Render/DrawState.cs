namespace Inochi2dSharp.Core.Render;

/// <summary>
/// Draw state flags.
/// </summary>
public enum DrawState : uint
{
    /// <summary>
    /// Normal drawing.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// A masking run is being defined.
    /// </summary>
    DefineMask = 1,

    /// <summary>
    /// Use the mask to draw the current command, reuse the mask if the next state also is maskedDraw.
    /// </summary>
    MaskedDraw = 2,

    /// <summary>
    /// A composition into composition textures has begun.
    /// </summary>
    CompositeBegin = 3,

    /// <summary>
    /// A composition into composition textures has ended.
    /// </summary>
    CompositeEnd = 4,

    /// <summary>
    /// Sources should be drawn to targets using the given blending mode.
    /// </summary>
    CompositeBlit = 5,
}
