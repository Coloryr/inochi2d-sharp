using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core;
using Inochi2dSharp.Fmt;

namespace Inochi2dSharp;

public partial class I2dCore
{
    public const uint IN_TEX_PNG = 0u; /// PNG encoded Inochi2D texture
    public const uint IN_TEX_TGA = 1u; /// TGA encoded Inochi2D texture
    public const uint IN_TEX_BC7 = 2u; /// BC7 encoded Inochi2D texture

    public bool IsLoadingINP { get; set; }

    /// <summary>
    /// Loads a puppet from a file
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public Puppet InLoadPuppet(string file)
    {
        try
        {
            using var buffer = File.OpenRead(file);

            switch (Path.GetExtension(file))
            {
                case ".inp":
                    if (!BinFmt.InVerifyMagicBytes(buffer))
                    {
                        throw new Exception("Invalid data format for INP puppet");
                    }
                    return InLoadINPPuppet(buffer);

                case ".inx":
                    if (!BinFmt.InVerifyMagicBytes(buffer))
                    {
                        throw new Exception("Invalid data format for Inochi Creator INX");
                    }
                    return InLoadINPPuppet(buffer);

                default:
                    throw new Exception($"Invalid file format of {Path.GetExtension(file)} at path {file}");
            }
        }
        catch (Exception ex)
        {
            InEndTextureLoading(false);
            throw new Exception("load error", ex);
        }
    }

    /// <summary>
    /// Loads a puppet from memory
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Puppet InLoadPuppetFromMemory(byte[] data)
    {
        var temp = Encoding.UTF8.GetString(data);
        var obj = JsonNode.Parse(temp)!.AsObject();
        var puppet = new Puppet(this);
        puppet.Deserialize(obj);
        return puppet;
    }

    /// <summary>
    ///  Loads a JSON based puppet
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public Puppet InLoadJSONPuppet(string data)
    {
        IsLoadingINP = false;
        return JsonSerializer.Deserialize<Puppet>(data)!;
    }

    /// <summary>
    /// Loads a INP based puppet
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public Puppet InLoadINPPuppet(Stream buffer)
    {
        IsLoadingINP = true;

        // Find the puppet data
        var temp = new byte[4];
        buffer.ReadExactly(temp);

        int puppetDataLength = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

        temp = new byte[puppetDataLength];
        buffer.ReadExactly(temp);

        string puppetData = Encoding.UTF8.GetString(temp);

        if (!BinFmt.InVerifyTexBytes(buffer))
        {
            throw new Exception("Expected Texture Blob section, got nothing!");
        }

        // Load textures in to memory

        InBeginTextureLoading();

        // Get amount of slots
        temp = new byte[4];
        buffer.ReadExactly(temp);
        int slotCount = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

        var slots = new List<Texture>();
        for (int i = 0; i < slotCount; i++)
        {
            temp = new byte[4];
            buffer.ReadExactly(temp);
            int textureLength = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

            var textureType = buffer.ReadByte();
            if (textureLength == 0)
            {
                InAddTextureBinary(new ShallowTexture([], 0, 0, 4));
            }
            else
            {
                if (textureType == 0)
                {
                    //PNG
                }
                else if (textureType == 1)
                {
                    //TGA
                }
                temp = ArrayPool<byte>.Shared.Rent(textureLength);
                buffer.ReadExactly(temp, 0, textureLength);
                InAddTextureBinary(new ShallowTexture(temp));
                ArrayPool<byte>.Shared.Return(temp);
            }

            // Readd to puppet so that stuff doesn't break if we re-save the puppet
            slots.Add(InGetLatestTexture());
        }

        var puppet = new Puppet(this);
        var obj = JsonNode.Parse(puppetData)!.AsObject();
        puppet.Deserialize(obj);
        puppet.TextureSlots = slots;
        puppet.UpdateTextureState();
        InEndTextureLoading();

        if (buffer.Length >= buffer.Position + 8 && BinFmt.InVerifyExtBytes(buffer))
        {
            temp = new byte[4];
            buffer.ReadExactly(temp);

            int sectionCount = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

            for (int section = 0; section < sectionCount; section++)
            {
                temp = new byte[4];
                buffer.ReadExactly(temp);

                // Get name of payload/vendor extended data
                int sectionNameLength = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

                temp = new byte[sectionNameLength];
                buffer.ReadExactly(temp);
                string sectionName = Encoding.UTF8.GetString(temp);

                temp = new byte[4];
                buffer.ReadExactly(temp);

                // Get length of data
                int payloadLength = temp[0] << 24 | temp[1] << 16 | temp[2] << 8 | temp[3];

                // Load the vendor JSON data in to the extData section of the puppet
                byte[] payload = new byte[payloadLength];
                buffer.ReadExactly(payload);
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
