using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Render;

/// <summary>
/// An ID managed by the backend rendering API.
/// </summary>
public struct ResourceID
{
    public IntPtr Ptr;
}

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
    public ResourceID Id;
}
