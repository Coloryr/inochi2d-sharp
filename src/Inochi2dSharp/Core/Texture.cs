using System.Runtime.InteropServices;
using Inochi2dSharp.Math;
using StbImageSharp;

namespace Inochi2dSharp.Core;

/// <summary>
/// A texture, only format supported is unsigned 8 bit RGBA
/// </summary>
public class Texture : IDisposable
{
    public uint Id { get; private set; }

    public int Width { get; private set; }
    public int Height { get; private set; }

    public uint InColorMode { get; private set; }
    public uint OutColorMode { get; private set; }
    public int Channels { get; private set; }

    public uint UUID { get; private set; }

    private readonly ImageResult? _image;
    private readonly I2dCore _core;
    public Texture(I2dCore core, ShallowTexture shallow) 
        : this(core, shallow.Data, shallow.Width, shallow.Height, shallow.Channels, shallow.ConvChannels)
    {
        _core = core;
    }

    /// <summary>
    /// Loads texture from image file
    /// Supported file types:
    /// * PNG 8-bit
    /// * BMP 8-bit
    /// * TGA 8-bit non-palleted
    /// * JPEG baseline
    /// </summary>
    /// <param name="file"></param>
    /// <param name="channels"></param>
    public Texture(I2dCore core, string file)
    {
        _core = core;
        var data = File.ReadAllBytes(file);
        _image = ImageResult.FromMemory(data);

        // Load in image data to OpenGL
        Width = _image.Width;
        Height = _image.Height;
        Channels = (int)_image.SourceComp;

        InColorMode = GlApi.GL_RGBA;
        OutColorMode = GlApi.GL_RGBA;

        // Generate OpenGL texture
        Id = _core.gl.GenTexture();
        SetData(_image.Data);

        // Set default filtering and wrapping
        SetFiltering(Filtering.Linear);
        SetWrapping(Wrapping.Clamp);
        SetAnisotropy(_core.IncGetMaxAnisotropy() / 2.0f);
        UUID = _core.InCreateUUID();
    }

    /// <summary>
    /// Creates a new empty texture
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="channels"></param>
    public Texture(I2dCore core, int width, int height, int channels = 4) : this(core, new byte[width * height * channels], width, height, channels, channels)
    {
        _core = core;
    }

    /// <summary>
    /// Creates a new texture from specified data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="inChannels"></param>
    /// <param name="outChannels"></param>
    public Texture(I2dCore core, byte[] data, int width, int height, int inChannels = 4, int outChannels = 4)
    {
        _core = core;

        Width = width;
        Height = height;
        Channels = outChannels;

        InColorMode = GlApi.GL_RGBA;
        OutColorMode = GlApi.GL_RGBA;
        if (inChannels == 1) InColorMode = GlApi.GL_RED;
        else if (inChannels == 2) InColorMode = GlApi.GL_RG;
        else if (inChannels == 3) InColorMode = GlApi.GL_RGB;

        // Generate OpenGL texture
        Id = _core.gl.GenTexture();
        SetData(data);

        // Set default filtering and wrapping
        SetFiltering(Filtering.Linear);
        SetWrapping(Wrapping.Clamp);
        SetAnisotropy(_core.IncGetMaxAnisotropy() / 2.0f);
        UUID = _core.InCreateUUID();
    }

    /// <summary>
    ///  Center of texture
    /// </summary>
    /// <returns></returns>
    public Vector2Int Center()
    {
        return new(Width / 2, Height / 2);
    }

    /// <summary>
    /// Gets the size of the texture
    /// </summary>
    /// <returns></returns>
    public Vector2Int Size()
    {
        return new(Width, Height);
    }

    /// <summary>
    /// Set the filtering mode used for the texture
    /// </summary>
    /// <param name="filtering"></param>
    public void SetFiltering(Filtering filtering)
    {
        Bind();
        _core.gl.TexParameterI(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MIN_FILTER,
            filtering == Filtering.Linear ? GlApi.GL_LINEAR_MIPMAP_LINEAR : GlApi.GL_NEAREST
        );

        _core.gl.TexParameterI(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MAG_FILTER,
            filtering == Filtering.Linear ? GlApi.GL_LINEAR : GlApi.GL_NEAREST
        );
    }

    public void SetAnisotropy(float value)
    {
        Bind();
        _core.gl.TexParameter(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MAX_ANISOTROPY,
            float.Clamp(value, 1, _core.IncGetMaxAnisotropy())
        );
    }

    /// <summary>
    /// Set the wrapping mode used for the texture
    /// </summary>
    /// <param name="wrapping"></param>
    public void SetWrapping(Wrapping wrapping)
    {
        Bind();
        _core.gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_WRAP_S, (uint)wrapping);
        _core.gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_WRAP_T, (uint)wrapping);
        _core.gl.TexParameter(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_BORDER_COLOR, [0f, 0f, 0f, 0f]);
    }

    /// <summary>
    /// Sets the data of the texture
    /// </summary>
    /// <param name="data"></param>
    public unsafe void SetData(byte[] data)
    {
        Bind();
        _core.gl.PixelStore(GlApi.GL_UNPACK_ALIGNMENT, 1);
        _core.gl.PixelStore(GlApi.GL_PACK_ALIGNMENT, 1);
        fixed (void* ptr = data)
        {
            _core.gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, OutColorMode, Width, Height, 0, InColorMode, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
        }

        GenMipmap();
    }

    /// <summary>
    /// Generate mipmaps
    /// </summary>
    public void GenMipmap()
    {
        Bind();
        _core.gl.GenerateMipmap(GlApi.GL_TEXTURE_2D);
    }

    /// <summary>
    /// Sets a region of a texture to new data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="channels"></param>
    public unsafe void SetDataRegion(byte[] data, int x, int y, int width, int height, int channels = 4)
    {
        Bind();

        // Make sure we don't try to change the texture in an out of bounds area.
        if (x < 0 || x + width > Width)
        {
            throw new Exception($"x offset is out of bounds (xoffset={x + width}, xbound={Width})");
        }
        if (y < 0 || y + height > Height)
        {
            throw new Exception($"y offset is out of bounds (yoffset={y + height}, ybound={Height})");
        }

        uint inChannelMode = GlApi.GL_RGBA;
        if (channels == 1) inChannelMode = GlApi.GL_RED;
        else if (channels == 2) inChannelMode = GlApi.GL_RG;
        else if (channels == 3) inChannelMode = GlApi.GL_RGB;

        // Update the texture
        fixed (void* ptr = data)
        {
            _core.gl.TexSubImage2D(GlApi.GL_TEXTURE_2D, 0, x, y, width, height, inChannelMode, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
        }

        GenMipmap();
    }

    /// <summary>
    /// Bind this texture
    /// 
    /// Notes
    /// - In release mode the unit value is clamped to 31 (The max OpenGL texture unit value)
    /// - In debug mode unit values over 31 will assert.
    /// </summary>
    /// <param name="unit"></param>
    public void Bind(uint unit = 0)
    {
        if (unit > 31)
        {
            throw new Exception("Outside maximum OpenGL texture unit value");
        }
        _core.gl.ActiveTexture(GlApi.GL_TEXTURE0 + (unit <= 31u ? unit : 31u));
        _core.gl.BindTexture(GlApi.GL_TEXTURE_2D, Id);
    }

    /// <summary>
    /// Saves the texture to file
    /// </summary>
    /// <param name="file"></param>
    public void Save(string file)
    {
        var temp = GetTextureData(true);
        Helper.Save(temp, Width, Height, file);
    }

    /// <summary>
    /// Gets the texture data for the texture
    /// </summary>
    /// <param name="unmultiply"></param>
    /// <returns></returns>
    public unsafe byte[] GetTextureData(bool unmultiply = false)
    {
        long size = Width * Height * Channels;
        var buf = new byte[Width * Height * Channels];
        Bind();
        fixed (byte* ptr = buf)
        {
            _core.gl.GetTexImage(GlApi.GL_TEXTURE_2D, 0, OutColorMode, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
        }
        if (unmultiply && Channels == 4)
        {
            _core.InTexUnPremuliply(buf, size);
        }
        return buf;
    }

    /// <summary>
    /// Gets this texture's texture id
    /// </summary>
    /// <returns></returns>
    public uint GetTextureId()
    {
        return Id;
    }

    /// <summary>
    /// Disposes texture from GL
    /// </summary>
    public void Dispose()
    {
        if (Id > 0)
        {
            _core.gl.DeleteTexture(Id);
            Id = 0;
        }
    }
}
