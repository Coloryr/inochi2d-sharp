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

    public void Deserialize(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("allowedUsers", out var temp) && temp != null)
        {
            AllowedUsers = temp.GetValue<string>();
        }

        if (obj.TryGetPropertyValue("allowViolence", out temp) && temp != null)
        {
            AllowViolence = temp.GetValue<bool>();
        }

        if (obj.TryGetPropertyValue("allowSexual", out temp) && temp != null)
        {
            AllowSexual = temp.GetValue<bool>();
        }

        if (obj.TryGetPropertyValue("allowCommercial", out temp) && temp != null)
        {
            AllowCommercial = temp.GetValue<bool>();
        }

        if (obj.TryGetPropertyValue("allowRedistribution", out temp) && temp != null)
        {
            AllowRedistribution = temp.GetValue<string>();
        }

        if (obj.TryGetPropertyValue("allowModification", out temp) && temp != null)
        {
            AllowModification = temp.GetValue<string>();
        }

        if (obj.TryGetPropertyValue("requireAttribution", out temp) && temp != null)
        {
            RequireAttribution = temp.GetValue<bool>();
        }
    }
}
