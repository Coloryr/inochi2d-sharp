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
    /// <summary>
    /// Springy pendulum
    /// </summary>
    public const string SpringPendulum = "spring_pendulum";
}

public static class ParamMapMode
{
    public const string AngleLength = "angle_length";
    public const string XY = "xy";
    public const string LengthAngle = "length_angle";
    public const string YX = "yx";
}