using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

/// <summary>
/// Masking mode
/// </summary>
public enum MaskingMode
{
    /// <summary>
    /// The part should be masked by the drawables specified
    /// </summary>
    Mask,
    /// <summary>
    /// The path should be dodge masked by the drawables specified
    /// </summary>
    DodgeMask
}
