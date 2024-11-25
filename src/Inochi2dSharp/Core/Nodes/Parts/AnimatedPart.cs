using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes.Parts;

/// <summary>
/// Parts which contain spritesheet animation
/// </summary>
[TypeId("AnimatedPart")]
public class AnimatedPart(I2dCore core, Node? parent = null) : Part(core, parent)
{
    public override string TypeId()
    {
        return "AnimatedPart";
    }

    /// <summary>
    /// The amount of splits in the texture
    /// </summary>
    public Vector2Int splits;
}
