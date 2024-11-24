using Newtonsoft.Json;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet meta information
/// </summary>
public record PuppetMeta
{
    /// <summary>
    /// Name of the puppet
    /// </summary>
    [JsonProperty("name")]
    public string Name;

    /// <summary>
    /// Version of the Inochi2D spec that was used for creating this model
    /// </summary>
    [JsonProperty("version")]
    public string Version = "1.0-alpha";

    /// <summary>
    /// Rigger(s) of the puppet
    /// </summary>
    [JsonProperty("rigger")]
    public string Rigger;

    /// <summary>
    /// Artist(s) of the puppet
    /// </summary>
    [JsonProperty("artist")]
    public string Artist;

    /// <summary>
    /// Usage Rights of the puppet
    /// <see cref="PuppetUsageRights"/>
    /// </summary>
    [JsonProperty("rights")]
    public string Rights;

    /// <summary>
    /// Copyright string
    /// </summary>
    [JsonProperty("copyright")]
    public string Copyright;

    /// <summary>
    /// URL of license
    /// </summary>
    [JsonProperty("licenseURL")]
    public string LicenseURL;

    /// <summary>
    /// Contact information of the first author
    /// </summary>
    [JsonProperty("contact")]
    public string Contact;

    /// <summary>
    /// Link to the origin of this puppet
    /// </summary>
    [JsonProperty("reference")]
    public string Reference;

    /// <summary>
    /// Texture ID of this puppet's thumbnail
    /// </summary>
    [JsonProperty("thumbnailId")]
    public uint ThumbnailId = uint.MaxValue;

    /// <summary>
    /// Whether the puppet should preserve pixel borders.
    /// This feature is mainly useful for puppets which use pixel art.
    /// </summary>
    [JsonProperty("preservePixels")]
    public bool PreservePixels;
}
