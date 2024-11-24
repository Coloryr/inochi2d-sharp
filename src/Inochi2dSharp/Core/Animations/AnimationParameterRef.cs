﻿using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Animations;

public record AnimationParameterRef
{
    /// <summary>
    /// A parameter to target
    /// </summary>
    public Parameter TargetParam { get; set; }
    /// <summary>
    /// Target axis of the parameter
    /// </summary>
    public int TargetAxis { get; set; }
}
