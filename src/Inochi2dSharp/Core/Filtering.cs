namespace Inochi2dSharp.Core;

/// <summary>
/// Filtering mode for texture
/// </summary>
public enum Filtering
{
    /// <summary>
    /// Linear filtering will try to smooth out textures
    /// </summary>
    Linear,
    /// <summary>
    /// Point filtering will try to preserve pixel edges.
    /// Due to texture sampling being float based this is imprecise.
    /// </summary>
    Point
}
