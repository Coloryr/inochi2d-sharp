namespace Inochi2dSharp.Core.Nodes.Drivers;

/// <summary>
/// Physics model to use for simple physics
/// </summary>
public enum PhysicsModel
{
    /// <summary>
    /// Rigid pendulum
    /// </summary>
    Pendulum,
    /// <summary>
    /// Springy pendulum
    /// </summary>
    SpringPendulum
}

public static class PhysicsModelHelper
{
    public static PhysicsModel ToPhysicsModel(this string str)
    {
        return str switch
        {
            "pendulum" or "Pendulum" => PhysicsModel.Pendulum,
            "spring_pendulum" or "SpringPendulum" => PhysicsModel.SpringPendulum,
            _ => PhysicsModel.Pendulum
        };
    }

    public static string GetString(this PhysicsModel model)
    {
        return model switch
        {
            PhysicsModel.Pendulum => "pendulum",
            PhysicsModel.SpringPendulum => "spring_pendulum",
            _ => "pendulum"
        };
    }
}