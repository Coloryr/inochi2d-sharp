namespace Inochi2dSharp.Core.Nodes.Drivers;

/// <summary>
/// Physics model to use for simple physics
/// </summary>
public static class PhysicsModel
{
    /// <summary>
    /// Rigid pendulum
    /// </summary>
    public const string Pendulum = "pendulum";
    public const string Pendulum1 = "Pendulum";
    /// <summary>
    /// Springy pendulum
    /// </summary>
    public const string SpringPendulum = "spring_pendulum";

    public const string SpringPendulum1 = "SpringPendulum";
}

public static class ParamMapMode
{
    public const string AngleLength = "angle_length";
    public const string AngleLength1 = "AngleLength";
    public const string XY = "xy";
    public const string XY1 = "XY";
    public const string LengthAngle = "length_angle";
    public const string LengthAngle1 = "LengthAngle";
    public const string YX = "yx";
    public const string YX1 = "YX";
}