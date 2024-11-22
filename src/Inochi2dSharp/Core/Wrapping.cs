using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

/// <summary>
/// Texture wrapping modes
/// </summary>
public enum Wrapping : uint
{
    /// <summary>
    /// Clamp texture sampling to be within the texture
    /// </summary>
    Clamp = GlApi.GL_CLAMP_TO_BORDER,
    /// <summary>
    /// Wrap the texture in every direction idefinitely
    /// </summary>
    Repeat = GlApi.GL_REPEAT,
    /// <summary>
    /// Wrap the texture mirrored in every direction indefinitely
    /// </summary>
    Mirror = GlApi.GL_MIRRORED_REPEAT
}
