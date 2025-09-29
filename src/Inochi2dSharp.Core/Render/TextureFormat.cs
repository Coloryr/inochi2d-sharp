using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Render;

/// <summary>
/// Format of texture data.
/// </summary>
public enum TextureFormat : uint
{
    /// <summary>
    /// None or unknown encoding.
    /// </summary>
    None = 0,
    /// <summary>
    /// RGBA8 data.
    /// </summary>
    Rgba8Unorm = 1,
    /// <summary>
    /// Red-channel only mask data.
    /// </summary>
    R8 = 2,
    /// <summary>
    /// Depth-stencil texture.
    /// </summary>
    DepthStencil = 3,
}
