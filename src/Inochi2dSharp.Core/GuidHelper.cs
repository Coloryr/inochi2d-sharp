using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

public static class GuidHelper
{
    /// <summary>
    /// Tries to get a GUID from a JSON Object.<br/>
    /// Inochi2D has transitioned over to GUIDs, as such we convert the old 32 bit UUIDs into fake GUIDs if they're in use.
    /// </summary>
    /// <param name="obj">The object to get the GUID from.</param>
    /// <param name="uuidKey">The legacy UUID key to check for.</param>
    /// <param name="guidKey">The GUID key to check for.</param>
    /// <returns>A GUID.</returns>
    public static Guid GetGuid(this JsonElement obj, string uuidKey, string guidKey = "guid")
    {
        if (obj.TryGetProperty(uuidKey, out var item) && item.ValueKind != JsonValueKind.Null)
        {
            var temp = item.GetUInt32();
            return new Guid(temp, 0, 0, 0, 0, 0, 0, 0, 0, 0, 255);
        }
        else
        {
            return Guid.Parse(obj.GetProperty(guidKey).GetString()!);
        }
    }
}
