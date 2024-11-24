using Newtonsoft.Json;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet physics settings
/// </summary>
public record PuppetPhysics
{
    [JsonProperty("pixelsPerMeter")]
    public float pixelsPerMeter = 1000;
    [JsonProperty("gravity")]
    public float gravity = 9.8f;
}
