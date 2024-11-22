using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// A keyframe
/// </summary>
public record Keyframe
{
    /// <summary>
    /// The frame at which this frame occurs
    /// </summary>
    [JsonProperty("frame")]
    public int Frame { get; set; }
    /// <summary>
    /// The value of the parameter at the given frame
    /// </summary>
    [JsonProperty("value")]
    public float Value { get; set; }
    /// <summary>
    /// Interpolation tension for cubic/inout
    /// </summary>
    [JsonProperty("tension")]
    public float Tension { get; set; } = 0.5f;
}
