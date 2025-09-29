using Inochi2dSharp.Core.Param;

namespace Inochi2dSharp.Core.Nodes.Drivers;

/// <summary>
/// Driver abstract node type
/// </summary>
[TypeId("Driver", 0x00000003, true)]
public abstract class Driver : Node
{
    /// <summary>
    /// The affected parameters of the driver.
    /// </summary>
    public virtual Parameter[] AffectedParameters => null!;

    public Driver()
    {

    }

    public Driver(Guid guid, Node? parent) : base(guid, parent)
    {

    }

    /// <summary>
    ///  Gets whether the given parameter is affected by this driver.
    /// </summary>
    /// <param name="param">The parameter to query.</param>
    /// <returns><see langword="true"/> if the parameter is affected by  the driver, <see langword="false"/> otherwise.</returns>
    public bool AffectsParameter(Parameter param)
    {
        foreach (var p in AffectedParameters)
        {
            if (p.Guid == param.Guid)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Updates the state of the driver.
    /// </summary>
    /// <param name="delta">Time since the last frame.</param>
    public abstract void UpdateDriver(float delta);
    /// <summary>
    /// Resets the driver's state.
    /// </summary>
    public abstract void Reset();
}
