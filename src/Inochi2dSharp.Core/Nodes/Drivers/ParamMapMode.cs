namespace Inochi2dSharp.Core.Nodes.Drivers;

public enum ParamMapMode
{
    AngleLength,
    XY,
    LengthAngle,
    YX
}

public static class ParamMapModeHelper
{
    public static ParamMapMode ToParamMapMode(this string str)
    {
        return str switch
        {
            "angle_length" or "AngleLength" => ParamMapMode.AngleLength,
            "xy" or "XY" => ParamMapMode.XY,
            "length_angle" or "LengthAngle" => ParamMapMode.LengthAngle,
            "yx" or "YX" => ParamMapMode.YX,
            _ => ParamMapMode.AngleLength
        };
    }

    public static string GetString(this ParamMapMode mode)
    {
        return mode switch
        {
            ParamMapMode.AngleLength => "angle_length",
            ParamMapMode.XY => "xy",
            ParamMapMode.LengthAngle => "length_angle",
            ParamMapMode.YX => "yx",
            _ => "angle_length"
        };
    }
}
