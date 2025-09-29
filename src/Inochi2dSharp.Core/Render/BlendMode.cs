namespace Inochi2dSharp.Core.Render;

/*
    INFORMATION ABOUT BLENDING MODES
    Blending is a complicated topic, especially once we get to mobile devices and games consoles.

    The following blending modes are supported in Standard mode:
        Normal
        Multiply
        Screen
        Overlay
        Darken
        Lighten
        Color Dodge
        Linear Dodge
        Add (Glow)
        Color Burn
        Hard Light
        Soft Light
        Difference
        Exclusion
        Subtract
        Inverse
        Destination In
        Clip To Lower
        Slice from Lower
    Some of these blending modes behave better on Tiling GPUs.

    The following blending modes are supported in Core mode:
        Normal
        Multiply
        Screen
        Lighten
        Color Dodge
        Linear Dodge
        Add (Glow)
        Inverse
        Destination In
        Clip to Lower
        Slice from Lower
    Tiling GPUs on older mobile devices don't have great drivers, we shouldn't tempt fate.
*/

/// <summary>
/// Blending modes
/// </summary>
public enum BlendMode
{
    /// <summary>
    /// Normal blending mode
    /// </summary>
    Normal = 0x00,
    /// <summary>
    /// Multiply blending mode
    /// </summary>
    Multiply,
    /// <summary>
    /// Screen
    /// </summary>
    Screen,
    /// <summary>
    /// Overlay
    /// </summary>
    Overlay,
    /// <summary>
    /// Darken
    /// </summary>
    Darken,
    /// <summary>
    /// Lighten
    /// </summary>
    Lighten,
    /// <summary>
    /// Color Dodge
    /// </summary>
    ColorDodge,
    /// <summary>
    /// Linear Dodge
    /// </summary>
    LinearDodge,
    /// <summary>
    /// Add (Glow)
    /// </summary>
    AddGlow,
    /// <summary>
    /// Color Burn
    /// </summary>
    ColorBurn,
    /// <summary>
    /// Hard Light
    /// </summary>
    HardLight,
    /// <summary>
    /// Soft Light
    /// </summary>
    SoftLight,
    /// <summary>
    /// Difference
    /// </summary>
    Difference,
    /// <summary>
    /// Exclusion
    /// </summary>
    Exclusion,
    /// <summary>
    /// Subtract
    /// </summary>
    Subtract,
    /// <summary>
    /// Inverse
    /// </summary>
    Inverse,
    /// <summary>
    /// Destination In
    /// </summary>
    DestinationIn,
    /// <summary>
    /// Clip to Lower
    /// Special blending mode that clips the drawable
    /// to a lower rendered area.
    /// </summary>
    SourceIn,
    /// <summary>
    /// Slice from Lower
    /// Special blending mode that slices the drawable
    /// via a lower rendered area.
    /// Basically inverse ClipToLower
    /// </summary>
    SourceOut
}

public static class BlendModeHelper
{ 
    public static BlendMode ToBlendMode(this string name)
    {
        return name switch
        {
            "Multiply" => BlendMode.Multiply,
            "Screen" => BlendMode.Screen,
            "Overlay" => BlendMode.Overlay,
            "Darken" => BlendMode.Darken,
            "Lighten" => BlendMode.Lighten,
            "ColorDodge" => BlendMode.ColorDodge,
            "LinearDodge" => BlendMode.LinearDodge,
            "AddGlow" => BlendMode.AddGlow,
            "ColorBurn" => BlendMode.ColorBurn,
            "HardLight" => BlendMode.HardLight,
            "SoftLight" => BlendMode.SoftLight,
            "Difference" => BlendMode.Difference,
            "Exclusion" => BlendMode.Exclusion,
            "Subtract" => BlendMode.Subtract,
            "Inverse" => BlendMode.Inverse,
            "DestinationIn" => BlendMode.DestinationIn,
            "ClipToLower" => BlendMode.SourceIn,
            "SliceFromLower" => BlendMode.SourceOut,
            _ => BlendMode.Normal,
        };
    }
}