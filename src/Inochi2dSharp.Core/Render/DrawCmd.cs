namespace Inochi2dSharp.Core.Render;

/// <summary>
/// A drawing command that is sent to the GPU.
/// </summary>
public record DrawCmd
{
    /// <summary>
    /// Maximum number of texture attachments.
    /// </summary>
    public const int IN_MAX_ATTACHMENTS = 8;

    /// <summary>
    /// Source textures
    /// </summary>
    public Texture[] Sources = new Texture[IN_MAX_ATTACHMENTS];

    /// <summary>
    /// The current state of the drawing command.
    /// </summary>
    public DrawState State;

    /// <summary>
    /// Blending mode to apply
    /// </summary>
    public BlendMode BlendMode;

    /// <summary>
    /// Masking mode to apply.
    /// </summary>
    public MaskingMode MaskMode;

    /// <summary>
    /// Allocation ID.
    /// </summary>
    public int AllocId;

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
    public int ElemCount;

    /// <summary>
    /// Type ID of the node being drawn.
    /// </summary>
    public uint TypeId;

    /// <summary>
    /// Variables passed to the draw list.
    /// </summary>
    public object[] Variables = new object[64];

    /// <summary>
    /// Whether the command is empty.
    /// </summary>
    public bool IsEmpty => ElemCount == 0;
}
