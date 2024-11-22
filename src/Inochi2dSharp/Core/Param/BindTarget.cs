using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public Node? node;

    /// <summary>
    /// The parameter to bind
    /// </summary>
    public string paramName;
}
