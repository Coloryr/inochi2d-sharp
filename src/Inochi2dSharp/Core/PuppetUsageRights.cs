using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core;

public record PuppetUsageRights
{
    /// <summary>
    /// Who is allowed to use the puppet?
    /// <see cref="PuppetAllowedUsers"/>
    /// </summary>
    public string AllowedUsers { get; set; } = PuppetAllowedUsers.OnlyAuthor;
    /// <summary>
    /// Whether violence content is allowed
    /// </summary>
    public bool AllowViolence { get; set; }
    /// <summary>
    /// Whether sexual content is allowed
    /// </summary>
    public bool AllowSexual { get; set; }
    /// <summary>
    /// Whether commerical use is allowed
    /// </summary>
    public bool AllowCommercial { get; set; }
    /// <summary>
    /// Whether a model may be redistributed
    /// <see cref="PuppetAllowedRedistribution"/>
    /// </summary>
    public string AllowRedistribution { get; set; } = PuppetAllowedRedistribution.Prohibited;
    /// <summary>
    /// Whether a model may be modified
    /// <see cref="PuppetAllowedModification"/>
    /// </summary>
    public string AllowModification { get; set; } = PuppetAllowedModification.Prohibited;
    /// <summary>
    /// Whether the author(s) must be attributed for use.
    /// </summary>
    public bool RequireAttribution { get; set; }

    public void Serialize(JsonObject obj)
    {
        obj.Add("allowedUsers", AllowedUsers);
        obj.Add("allowViolence", AllowViolence);
        obj.Add("allowSexual", AllowSexual);
        obj.Add("allowCommercial", AllowCommercial);
        obj.Add("allowRedistribution", AllowRedistribution);
        obj.Add("allowModification", AllowModification);
        obj.Add("requireAttribution", RequireAttribution);
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "allowedUsers" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowedUsers = item.Value.GetString()!;
            }
            else if (item.Name == "allowViolence" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowViolence = item.Value.GetBoolean();
            }
            else if (item.Name == "allowSexual" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowSexual = item.Value.GetBoolean();
            }
            else if (item.Name == "allowCommercial" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowCommercial = item.Value.GetBoolean();
            }
            else if (item.Name == "allowRedistribution" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowRedistribution = item.Value.GetString()!;
            }
            else if (item.Name == "allowModification" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowModification = item.Value.GetString()!;
            }
            else if (item.Name == "requireAttribution" && item.Value.ValueKind != JsonValueKind.Null)
            {
                RequireAttribution = item.Value.GetBoolean(); ;
            }
        }
    }
}
