namespace Inochi2dSharp.Core;

public record PuppetAllowedModification(string Data) : PuppetEnum(Data)
{
    /// <summary>
    /// Modification is prohibited
    /// </summary>
    public static readonly PuppetAllowedModification Prohibited = new("prohibited");
    /// <summary>
    /// Modification is only allowed for personal use
    /// </summary>
    public static readonly PuppetAllowedModification AllowPersonal = new("allowPersonal");
    /// <summary>
    /// Modification is allowed with redistribution, see <see cref="PuppetAllowedRedistribution"/> for redistribution terms.
    /// </summary>
    public static readonly PuppetAllowedModification AllowRedistribute = new("allowRedistribute");

    public static PuppetAllowedModification Get(string key)
    {
        if (key == Prohibited.Data)
        {
            return Prohibited;
        }
        else if (key == AllowPersonal.Data)
        {
            return AllowPersonal;
        }
        else if (key == AllowRedistribute.Data)
        {
            return AllowRedistribute;
        }

        return Prohibited;
    }
}