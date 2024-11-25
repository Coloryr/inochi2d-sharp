using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core;

/// <summary>
/// Puppet meta information
/// </summary>
public record PuppetMeta
{
    /// <summary>
    /// Name of the puppet
    /// </summary>
    public string Name { get; set; }
    /// <summary>
    /// Version of the Inochi2D spec that was used for creating this model
    /// </summary>
    public string Version { get; set; } = "1.0-alpha";
    /// <summary>
    /// Rigger(s) of the puppet
    /// </summary>
    public string Rigger { get; set; }
    /// <summary>
    /// Artist(s) of the puppet
    /// </summary>
    public string Artist { get; set; }
    /// <summary>
    /// Usage Rights of the puppet
    /// </summary>
    public PuppetUsageRights Rights { get; set; }
    /// <summary>
    /// Copyright string
    /// </summary>
    public string Copyright { get; set; }
    /// <summary>
    /// URL of license
    /// </summary>
    public string LicenseURL { get; set; }
    /// <summary>
    /// Contact information of the first author
    /// </summary>
    public string Contact { get; set; }
    /// <summary>
    /// Link to the origin of this puppet
    /// </summary>
    public string Reference { get; set; }
    /// <summary>
    /// Texture ID of this puppet's thumbnail
    /// </summary>
    public uint ThumbnailId { get; set; } = uint.MaxValue;
    /// <summary>
    /// Whether the puppet should preserve pixel borders.
    /// This feature is mainly useful for puppets which use pixel art.
    /// </summary>
    public bool PreservePixels { get; set; }

    public void Serialize(JsonObject serializer)
    {
        serializer.Add("name", Name);
        serializer.Add("version", Version);
        serializer.Add("rigger", Rigger);
        serializer.Add("artist", Artist);
        var obj = new JsonObject();
        Rights.Serialize(obj);
        serializer.Add("rights", obj);
        serializer.Add("copyright", Copyright);
        serializer.Add("licenseURL", LicenseURL);
        serializer.Add("contact", Contact);
        serializer.Add("reference", Reference);
        serializer.Add("thumbnailId", ThumbnailId);
        serializer.Add("preservePixels", PreservePixels);
    }

    public void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("name", out var temp) && temp != null)
        {
            Name = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("version", out temp) && temp != null)
        {
            Version = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("rigger", out temp) && temp != null)
        {
            Rigger = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("artist", out temp) && temp != null)
        {
            Artist = temp.GetValue<string>();
        }

        Rights = new();
        if (data.TryGetPropertyValue("rights", out temp) && temp is JsonObject obj)
        {
            Rights.Deserialize(obj);
        }

        if (data.TryGetPropertyValue("copyright", out temp) && temp != null)
        {
            Copyright = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("licenseURL", out temp) && temp != null)
        {
            LicenseURL = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("contact", out temp) && temp != null)
        {
            Contact = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("reference", out temp) && temp != null)
        {
            Reference = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("thumbnailId", out temp) && temp != null)
        {
            ThumbnailId = temp.GetValue<uint>();
        }

        if (data.TryGetPropertyValue("preservePixels", out temp) && temp != null)
        {
            PreservePixels = temp.GetValue<bool>();
        }
    }
}
