using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Core.Param;

/// <summary>
/// A target to bind to
/// </summary>
public record BindTarget
{
    /// <summary>
    /// The node to bind to
    /// </summary>
    public Node Node;

    /// <summary>
    /// The parameter to bind
    /// </summary>
    public string ParamName;
}
