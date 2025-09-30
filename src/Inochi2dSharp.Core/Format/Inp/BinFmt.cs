using System.Buffers;
using System.Text;
using System.Text.Json;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Format.Inp;

public static class BinFmt
{
    /// <summary>
    /// Entrypoint magic bytes that define this is an Inochi2D puppet
    /// <br/>
    /// Trans Rights!
    /// </summary>
    public static byte[] MAGIC_BYTES = Encoding.UTF8.GetBytes("TRNSRTS\0");

    public static byte[] TEX_SECTION = Encoding.UTF8.GetBytes("TEX_SECT");
    public static byte[] EXT_SECTION = Encoding.UTF8.GetBytes("EXT_SECT");

    /// <summary>
    /// Verifies that a buffer has the Inochi2D magic bytes present.
    /// </summary>
    /// <param name="buffer"></param>
    /// <returns></returns>
    //public static bool InVerifyMagicBytes(byte[] buffer)
    //{
    //    return InVerifySection(buffer, MAGIC_BYTES);
    //}

    public static bool InVerifyMagicBytes(Stream buffer)
    {
        var temp = new byte[8];
        buffer.ReadExactly(temp);
        return InVerifySection(temp, MAGIC_BYTES);
    }

    //public static bool InVerifyExtBytes(byte[] buffer)
    //{
    //    return InVerifySection(buffer, EXT_SECTION);
    //}

    public static bool InVerifyExtBytes(Stream buffer)
    {
        var temp = new byte[8];
        buffer.ReadExactly(temp);
        return InVerifySection(temp, EXT_SECTION);
    }

    public static bool InVerifyTexBytes(Stream buffer)
    {
        var temp = new byte[8];
        buffer.ReadExactly(temp);
        return InVerifySection(temp, TEX_SECTION);
    }

    /// <summary>
    /// Verifies a section
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="section"></param>
    /// <returns></returns>
    public static bool InVerifySection(byte[] buffer, byte[] section)
    {
        if (buffer.Length >= section.Length)
        {
            var temp = buffer[0..section.Length];
            for (int a = 0; a < section.Length; a++)
            {
                if (temp[a] != section[a])
                {
                    return false;
                }
            }

            return true;
        }
        return false;
    }

    public const uint IN_TEX_PNG = 0u; /// PNG encoded Inochi2D texture
    public const uint IN_TEX_TGA = 1u; /// TGA encoded Inochi2D texture
    public const uint IN_TEX_BC7 = 2u; /// BC7 encoded Inochi2D texture

    /// <summary>
    /// Loads a puppet from a file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static Puppet InLoadPuppet(string file)
    {
        try
        {
            using var stream = File.OpenRead(file);

            switch (Path.GetExtension(file))
            {
                case ".inp":
                    if (!InVerifyMagicBytes(stream))
                    {
                        throw new Exception("Invalid data format for INP puppet");
                    }
                    return InLoadINPPuppet(stream);

                case ".inx":
                    if (!InVerifyMagicBytes(stream))
                    {
                        throw new Exception("Invalid data format for Inochi Creator INX");
                    }
                    return InLoadINPPuppet(stream);

                default:
                    throw new Exception($"Invalid file format of {Path.GetExtension(file)} at path {file}");
            }
        }
        catch (Exception ex)
        {
            throw new Exception("load error", ex);
        }
    }

    /// <summary>
    /// Loads a puppet from memory
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Puppet InLoadPuppetFromMemory(byte[] data)
    {
        var temp = Encoding.UTF8.GetString(data);
        using var doc = JsonDocument.Parse(temp);
        var obj = doc.RootElement;
        var puppet = new Puppet();
        puppet.Deserialize(obj);
        return puppet;
    }

    /// <summary>
    ///  Loads a JSON based puppet
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Puppet InLoadJSONPuppet(string data)
    {
        return JsonSerializer.Deserialize<Puppet>(data)!;
    }

    /// <summary>
    /// Loads a INP based puppet
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public static Puppet InLoadINPPuppet(Stream stream)
    {
        // Find the puppet data
        var buffer = new byte[4];
        stream.ReadExactly(buffer);
        int puppetDataLength = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

        buffer = new byte[puppetDataLength];
        stream.ReadExactly(buffer);

        string puppetData = Encoding.UTF8.GetString(buffer);

        if (!InVerifyTexBytes(stream))
        {
            throw new Exception("Expected Texture Blob section, got nothing!");
        }

        // Get amount of slots
        buffer = new byte[4];
        stream.ReadExactly(buffer);
        int slotCount = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

        var slots = new TextureCache();
        for (int i = 0; i < slotCount; i++)
        {
            buffer = new byte[4];
            stream.ReadExactly(buffer);
            int textureLength = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

            var textureType = stream.ReadByte();
            if (textureType == 0)
            {
                //PNG
            }
            else if (textureType == 1)
            {
                //TGA
            }
            buffer = ArrayPool<byte>.Shared.Rent(textureLength);
            stream.ReadExactly(buffer, 0, textureLength);
            slots.Add(new Texture(TextureData.Load(buffer)));
            ArrayPool<byte>.Shared.Return(buffer);
        }

        var puppet = new Puppet(slots);
        using var doc = JsonDocument.Parse(puppetData);
        var obj = doc.RootElement;
        puppet.Deserialize(obj);

        if (stream.Length >= stream.Position + 8 && InVerifyExtBytes(stream))
        {
            buffer = new byte[4];
            stream.ReadExactly(buffer);

            int sectionCount = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

            for (int section = 0; section < sectionCount; section++)
            {
                buffer = new byte[4];
                stream.ReadExactly(buffer);

                // Get name of payload/vendor extended data
                int sectionNameLength = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

                buffer = new byte[sectionNameLength];
                stream.ReadExactly(buffer);
                string sectionName = Encoding.UTF8.GetString(buffer);

                buffer = new byte[4];
                stream.ReadExactly(buffer);

                // Get length of data
                int payloadLength = buffer[0] << 24 | buffer[1] << 16 | buffer[2] << 8 | buffer[3];

                // Load the vendor JSON data in to the extData section of the puppet
                byte[] payload = new byte[payloadLength];
                stream.ReadExactly(payload);
                puppet.ExtData.Add(sectionName, payload);
            }
        }

        // We're done!
        return puppet;
    }

    /// <summary>
    /// Only write changed EXT section portions to puppet file
    /// </summary>
    /// <param name="p"></param>
    /// <param name="file"></param>
    //public static void inWriteINPExtensions(Puppet p, string file)
    //{
    //    int extSectionStart, extSectionEnd;
    //    bool foundExtSection;
    //    var f = File.OpenWrite(file);

    //    // Verify that we're in an INP file
    //    enforce(inVerifyMagicBytes(f.read(MAGIC_BYTES.length)), "Invalid data format for INP puppet");

    //    // Read puppet payload
    //    uint puppetSectionLength = f.readValue!uint;
    //    f.skip(puppetSectionLength);

    //    // Verify texture section magic bytes
    //    enforce(inVerifySection(f.read(TEX_SECTION.length), TEX_SECTION), "Expected Texture Blob section, got nothing!");

    //    uint slotCount = f.readValue!uint;
    //    foreach (slot; 0..slotCount) {
    //        uint length = f.readValue!uint;
    //        f.skip(length + 1);
    //    }

    //    // Only do this if there is an extended section here
    //    if (inVerifySection(f.peek(EXT_SECTION.length), EXT_SECTION))
    //    {
    //        foundExtSection = true;

    //        extSectionStart = f.tell();
    //        f.skip(EXT_SECTION.length);

    //        uint payloadCount = f.readValue!uint;
    //        foreach (pc; 0..payloadCount) {

    //            uint nameLength = f.readValue!uint;
    //            f.skip(nameLength);

    //            uint payloadLength = f.readValue!uint;
    //            f.skip(payloadLength);
    //        }
    //        extSectionEnd = f.tell();
    //    }
    //    f.close();

    //    ubyte[] fdata = cast(ubyte[])stdfile.read(file);
    //    ubyte[] app = fdata;
    //    if (foundExtSection)
    //    {
    //        // If the extended section was found, reuse it.
    //        app = fdata[0..extSectionStart];
    //        ubyte[] end = fdata[extSectionEnd..$];

    //        // Don't waste bytes on empty EXT data sections
    //        if (p.extData.length > 0)
    //        {
    //            // Begin extended section
    //            app ~= EXT_SECTION;
    //            app ~= nativeToBigEndian(cast(uint)p.extData.length)[0..4];

    //            foreach (name, payload; p.extData) {

    //                // Write payload name and its length
    //                app ~= nativeToBigEndian(cast(uint)name.length)[0..4];
    //                app ~= cast(ubyte[])name;

    //                // Write payload length and payload
    //                app ~= nativeToBigEndian(cast(uint)payload.length)[0..4];
    //                app ~= payload;

    //            }
    //        }

    //        app ~= end;

    //    }
    //    else
    //    {
    //        // Otherwise, make a new one

    //        // Don't waste bytes on empty EXT data sections
    //        if (p.extData.length > 0)
    //        {
    //            // Begin extended section
    //            app ~= EXT_SECTION;
    //            app ~= nativeToBigEndian(cast(uint)p.extData.length)[0..4];

    //            foreach (name, payload; p.extData) {

    //                // Write payload name and its length
    //                app ~= nativeToBigEndian(cast(uint)name.length)[0..4];
    //                app ~= cast(ubyte[])name;

    //                // Write payload length and payload
    //                app ~= nativeToBigEndian(cast(uint)payload.length)[0..4];
    //                app ~= payload;

    //            }
    //        }
    //    }

    //    // write our final file out
    //    stdfile.write(file, app);
    //}

    /// <summary>
    /// Writes out a model to memory
    /// </summary>
    /// <param name="p"></param>
    /// <returns></returns>
    //public static byte[] inWriteINPPuppetMemory(Puppet p)
    //{
    //    IsLoadingINP = true;
    //    auto app = appender!(ubyte[]);

    //    // Write the current used Inochi2D version to the version_ meta tag.
    //    p.meta.version_ = IN_VERSION;
    //    string puppetJson = inToJson(p);

    //    app ~= MAGIC_BYTES;
    //    app ~= nativeToBigEndian(cast(uint)puppetJson.length)[0..4];
    //    app ~= cast(ubyte[])puppetJson;

    //    // Begin texture section
    //    app ~= TEX_SECTION;
    //    app ~= nativeToBigEndian(cast(uint)p.textureSlots.length)[0..4];
    //    foreach (texture; p.textureSlots) {
    //        int e;
    //        ubyte[] tex = write_image_mem(IF_TGA, texture.width, texture.height, texture.getTextureData(), texture.channels, e);
    //        app ~= nativeToBigEndian(cast(uint)tex.length)[0..4];
    //        app ~= (cast(ubyte)IN_TEX_TGA);
    //        app ~= (tex);
    //    }

    //    // Don't waste bytes on empty EXT data sections
    //    if (p.extData.length > 0)
    //    {
    //        // Begin extended section
    //        app ~= EXT_SECTION;
    //        app ~= nativeToBigEndian(cast(uint)p.extData.length)[0..4];

    //        foreach (name, payload; p.extData) {

    //            // Write payload name and its length
    //            app ~= nativeToBigEndian(cast(uint)name.length)[0..4];
    //            app ~= cast(ubyte[])name;

    //            // Write payload length and payload
    //            app ~= nativeToBigEndian(cast(uint)payload.length)[0..4];
    //            app ~= payload;

    //        }
    //    }

    //    return app.data;
    //}

    /// <summary>
    /// Writes Inochi2D puppet to file
    /// </summary>
    /// <param name="p"></param>
    /// <param name="file"></param>
    //public static void inWriteINPPuppet(Puppet p, string file)
    //{
    //    // Write it out to file
    //    File.WriteAllBytes(file, inWriteINPPuppetMemory(p));
    //}

    /// <summary>
    /// Writes a puppet to file
    /// </summary>
    /// <param name="p"></param>
    /// <param name="file"></param>
    //public static void inWriteJSONPuppet(Puppet p, string file)
    //{
    //    IsLoadingINP = false;
    //    File.WriteAllText(file, inToJson(p));
    //}

    /// <summary>
    /// Serialize item with compact Inochi2D JSON serializer
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="item"></param>
    /// <returns></returns>
    //public static string inToJson<T>(T item) 
    //{
    //    return JsonConvert.SerializeObject(item);
    //}
}
