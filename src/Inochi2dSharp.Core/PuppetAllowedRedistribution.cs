namespace Inochi2dSharp.Core;

public record PuppetAllowedRedistribution(string Data) : PuppetEnum(Data)
{
    /// <summary>
    /// Redistribution is prohibited
    /// </summary>
    public static readonly PuppetAllowedRedistribution Prohibited = new("prohibited");
    /// <summary>
    /// Redistribution is allowed, but only under the same license as the original.
    /// </summary>
    public static readonly PuppetAllowedRedistribution ViralLicense = new("viralLicense");
    /// <summary>
    /// Redistribution is allowed, and the puppet may be redistributed under a different license than the original.<para>This goes in conjunction with modification rights.</para>
    /// </summary>
    public static readonly PuppetAllowedRedistribution CopyleftLicense = new("copyleftLicense");

    public static PuppetAllowedRedistribution Get(string key)
    {
        if (key == Prohibited.Data)
        {
            return Prohibited;
        }
        else if (key == ViralLicense.Data)
        {
            return ViralLicense;
        }
        else if (key == CopyleftLicense.Data)
        {
            return CopyleftLicense;
        }

        return Prohibited;
    }
}
