using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet physics settings
/// </summary>
public record PuppetPhysics
{
    public float pixelsPerMeter = 1000;

    public float gravity = 9.8f;
}
