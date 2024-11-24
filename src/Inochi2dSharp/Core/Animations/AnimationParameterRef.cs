using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Animations;

public record AnimationParameterRef
{
    /// <summary>
    /// A parameter to target
    /// </summary>
    public Parameter targetParam;
    /// <summary>
    /// Target axis of the parameter
    /// </summary>
    public int targetAxis;
}
