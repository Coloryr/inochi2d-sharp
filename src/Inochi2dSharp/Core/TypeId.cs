using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

/// <summary>
/// UDA for sub-classable parts of the spec
/// eg.Nodes and Automation can be extended by
/// adding new subclasses that aren't in the base spec.
/// </summary>
public struct TypeId
{
    public string id;
}
