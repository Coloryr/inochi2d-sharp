using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes.Parts;

/// <summary>
/// Parts which contain spritesheet animation
/// </summary>
[TypeId("AnimatedPart")]
public class AnimatedPart : Part
{
    public override string typeId()
    {
        return "AnimatedPart";
    }

    /// <summary>
    /// The amount of splits in the texture
    /// </summary>
    public Vector2Int splits;
}
