namespace Inochi2dSharp.Core.Render;

/// <summary>
/// An allocation within the drawlist
/// </summary>
public record DrawListAlloc
{
    /// <summary>
    /// Vertex offset.
    /// </summary>
    public int VtxOffset;

    /// <summary>
    /// Index offset.
    /// </summary>
    public int IdxOffset;

    /// <summary>
    /// Number of indices.
    /// </summary>
    public int IdxCount;

    /// <summary>
    /// Number of vertices.
    /// </summary>
    public int VtxCount;

    /// <summary>
    /// Allocation ID.
    /// </summary>
    public int AllocId;
}
