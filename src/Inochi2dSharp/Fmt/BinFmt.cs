using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Fmt;

public static class BinFmt
{
    public static readonly byte[] MAGIC_BYTES = Encoding.UTF8.GetBytes("TRNSRTS\0");

    public static readonly byte[] TEX_SECTION = Encoding.UTF8.GetBytes("TEX_SECT");
    public static readonly byte[] EXT_SECTION = Encoding.UTF8.GetBytes("EXT_SECT");

    /// <summary>
    /// Verifies that a buffer has the Inochi2D magic bytes present.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    public static bool InVerifyMagicBytes(byte[] buffer)
    {
        return InVerifySection(buffer, MAGIC_BYTES);
    }

    /// <summary>
    /// Verifies a section
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="section"></param>
    /// <returns></returns>
    public static bool InVerifySection(byte[] buffer, byte[] section)
    {
        return buffer.Length >= section.Length && buffer[0..section.Length] == section;
    }
}
