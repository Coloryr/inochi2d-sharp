using Newtonsoft.Json;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet physics settings
/// </summary>
public record PuppetPhysics
{
    [JsonProperty("pixelsPerMeter")]
    public float PixelsPerMeter = 1000;

    [JsonProperty("gravity")]
    public float Gravity = 9.8f;
}
