namespace Inochi2dSharp.Core.Render;

/// <summary>
/// A resource that can be transferred between CPU and GPU.
/// </summary>
public abstract record Resource
{
    /// <summary>
    /// Length of the resource's data allocation in bytes.
    /// </summary>
    public abstract int Length { get; }
    /// <summary>
    /// ID of a resource, differs based on the underlying rendering API.
    /// </summary>
    public object Id { get; set; }
}
