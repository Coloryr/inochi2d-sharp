namespace Inochi2dSharp.Core.Render;

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
    Dodge
}

public static class MaskingModeHelper
{
    public static MaskingMode ToMaskingMode(this string name)
    {
        return name switch
        {
            "Mask" or "mask" => MaskingMode.Mask,
            "DodgeMask" or "dodgeMask" => MaskingMode.Dodge,
            _ => MaskingMode.Mask,
        };
    }
}