using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

public static class PuppetAllowedUsers
{
    /// <summary>
    /// Only the author(s) are allowed to use the puppet
    /// </summary>
    public const string OnlyAuthor = "onlyAuthor";

    /// <summary>
    /// Only licensee(s) are allowed to use the puppet
    /// </summary>
    public const string OnlyLicensee = "onlyLicensee";

    /// <summary>
    /// Everyone may use the model
    /// </summary>
    public const string Everyone = "everyone";
}

public static class PuppetAllowedRedistribution
{
    /// <summary>
    /// Redistribution is prohibited
    /// </summary>
    public const string Prohibited = "prohibited";

    /// <summary>
    /// Redistribution is allowed, but only under
    /// the same license as the original.
    /// </summary>
    public const string ViralLicense = "viralLicense";

    /// <summary>
    /// Redistribution is allowed, and the puppet
    /// may be redistributed under a different
    /// license than the original.
    /// 
    /// This goes in conjunction with modification rights.
    /// </summary>
    public const string CopyleftLicense = "copyleftLicense";
}

public static class PuppetAllowedModification
{
    /// <summary>
    /// Modification is prohibited
    /// </summary>
    public const string Prohibited = "prohibited";

    /// <summary>
    /// Modification is only allowed for personal use
    /// </summary>
    public const string AllowPersonal = "allowPersonal";

    /// <summary>
    /// Modification is allowed with redistribution,
    /// see allowedRedistribution for redistribution terms.
    /// </summary>
    public const string AllowRedistribute = "allowRedistribute";
}