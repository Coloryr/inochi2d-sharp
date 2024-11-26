using System.Numerics;

namespace Inochi2dSharp.Core.Nodes.Shape;

/// <summary>
/// A Shape Node
/// </summary>
public record ShapeNode
{
    /// <summary>
    /// The breakpoint in which the Shape Node activates
    /// </summary>
    public Vector2 Breakpoint;

    /// <summary>
    /// The shape data
    /// </summary>
    public Vector2[] ShapeData;
}
