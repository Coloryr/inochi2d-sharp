using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

public record PuppetAllowedUsers(string Data) : PuppetEnum(Data)
{
    /// <summary>
    /// Only the author(s) are allowed to use the puppet
    /// </summary>
    public static readonly PuppetAllowedUsers OnlyAuthor = new("onlyAuthor");
    /// <summary>
    /// Only licensee(s) are allowed to use the puppet
    /// </summary>
    public static readonly PuppetAllowedUsers OnlyLicensee = new("onlyLicensee");
    /// <summary>
    /// Everyone may use the model
    /// </summary>
    public static readonly PuppetAllowedUsers Everyone = new("everyone");

    public static PuppetAllowedUsers Get(string key)
    {
        if (key == OnlyAuthor.Data)
        {
            return OnlyAuthor;
        }
        else if (key == OnlyLicensee.Data)
        {
            return OnlyLicensee;
        }
        else if (key == Everyone.Data)
        {
            return Everyone;
        }

        return OnlyAuthor;
    }
}