using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core;

public class PuppetUsageRights
{
    /// <summary>
    /// Who is allowed to use the puppet?
    /// </summary>
    public PuppetAllowedUsers AllowedUsers = PuppetAllowedUsers.OnlyAuthor;
    /// <summary>
    /// Whether violence content is allowed
    /// </summary>
    public bool AllowViolence = false;
    /// <summary>
    /// Whether sexual content is allowed
    /// </summary>
    public bool AllowSexual = false;
    /// <summary>
    /// Whether commerical use is allowed
    /// </summary>
    public bool AllowCommercial = false;
    /// <summary>
    /// Whether a model may be redistributed
    /// </summary>
    public PuppetAllowedRedistribution AllowRedistribution = PuppetAllowedRedistribution.Prohibited;
    /// <summary>
    /// Whether a model may be modified
    /// </summary>
    public PuppetAllowedModification AllowModification = PuppetAllowedModification.Prohibited;
    /// <summary>
    /// Whether the author(s) must be attributed for use.
    /// </summary>
    public bool RequireAttribution = false;

    public void Serialize(JsonObject obj)
    {
        obj["allowedUsers"] = AllowedUsers.Data;
        obj["allowViolence"] = AllowViolence;
        obj["allowSexual"] = AllowSexual;
        obj["allowCommercial"] = AllowCommercial;
        obj["allowRedistribution"] = AllowRedistribution.Data;
        obj["allowModification"] = AllowModification.Data;
        obj["requireAttribution"] = RequireAttribution;
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "allowedUsers" && item.Value.ValueKind is JsonValueKind.String)
            {
                AllowedUsers = PuppetAllowedUsers.Get(item.Value.GetString()!);
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
                AllowRedistribution = PuppetAllowedRedistribution.Get(item.Value.GetString()!);
            }
            else if (item.Name == "allowModification" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AllowModification = PuppetAllowedModification.Get(item.Value.GetString()!);
            }
            else if (item.Name == "requireAttribution" && item.Value.ValueKind != JsonValueKind.Null)
            {
                RequireAttribution = item.Value.GetBoolean(); ;
            }
        }
    }
}
