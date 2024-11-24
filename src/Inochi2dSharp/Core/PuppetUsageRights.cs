using Newtonsoft.Json;

namespace Inochi2dSharp.Core;

public record PuppetUsageRights
{
    /// <summary>
    /// Who is allowed to use the puppet?
    /// <see cref="PuppetAllowedUsers"/>
    /// </summary>
    [JsonProperty("allowedUsers")]
    public string AllowedUsers = PuppetAllowedUsers.OnlyAuthor;

    /// <summary>
    /// Whether violence content is allowed
    /// </summary>
    [JsonProperty("allowViolence")]
    public bool AllowViolence;

    /// <summary>
    /// Whether sexual content is allowed
    /// </summary>
    [JsonProperty("allowViolence")]
    public bool AllowSexual;

    /// <summary>
    /// Whether commerical use is allowed
    /// </summary>
    [JsonProperty("allowViolence")]
    public bool AllowCommercial;

    /// <summary>
    /// Whether a model may be redistributed
    /// <see cref="PuppetAllowedRedistribution"/>
    /// </summary>
    [JsonProperty("allowRedistribution")]
    public string AllowRedistribution = PuppetAllowedRedistribution.Prohibited;

    /// <summary>
    /// Whether a model may be modified
    /// <see cref="PuppetAllowedModification"/>
    /// </summary>
    [JsonProperty("allowModification")]
    public string AllowModification = PuppetAllowedModification.Prohibited;

    /// <summary>
    /// Whether the author(s) must be attributed for use.
    /// </summary>
    [JsonProperty("requireAttribution")]
    public bool RequireAttribution;
}
