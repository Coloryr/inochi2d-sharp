using System.Text.Json;
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

    public void Serialize(JsonObject data)
    {
        data.Add("name", Name);
        data.Add("version", Version);
        data.Add("rigger", Rigger);
        data.Add("artist", Artist);
        if (Rights != null)
        {
            var obj = new JsonObject();
            Rights.Serialize(obj);
            data.Add("rights", obj);
        }
        data.Add("copyright", Copyright);
        data.Add("licenseURL", LicenseURL);
        data.Add("contact", Contact);
        data.Add("reference", Reference);
        data.Add("thumbnailId", ThumbnailId);
        data.Add("preservePixels", PreservePixels);
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "name" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Name = item.Value.GetString()!;
            }
            else if (item.Name == "version" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Version = item.Value.GetString()!;
            }
            else if (item.Name == "rigger" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Rigger = item.Value.GetString()!;
            }
            else if (item.Name == "artist" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Artist = item.Value.GetString()!;
            }
            else if (item.Name == "rights" && item.Value.ValueKind == JsonValueKind.Object)
            {
                Rights = new();
                Rights.Deserialize(item.Value);
            }
            else if (item.Name == "copyright" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Copyright = item.Value.GetString()!;
            }
            else if (item.Name == "licenseURL" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LicenseURL = item.Value.GetString()!;
            }
            else if (item.Name == "contact" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Contact = item.Value.GetString()!;
            }
            else if (item.Name == "reference" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Reference = item.Value.GetString()!;
            }
            else if (item.Name == "thumbnailId" && item.Value.ValueKind != JsonValueKind.Null)
            {
                ThumbnailId = item.Value.GetUInt32();
            }
            else if (item.Name == "preservePixels" && item.Value.ValueKind != JsonValueKind.Null)
            {
                PreservePixels = item.Value.GetBoolean();
            }
        }
    }
}
