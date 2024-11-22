using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using SkiaSharp;

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

    private readonly SKBitmap? _image;

    public Texture(ShallowTexture shallow) : this(shallow.Data, shallow.Width, shallow.Height, shallow.Channels, shallow.ConvChannels)
    { 
        
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
    public Texture(string file) 
    {
        _image = SKBitmap.Decode(file);

        // Load in image data to OpenGL
        Width = _image.Width;
        Height = _image.Height;
        Channels = _image.GetChannel();

        InColorMode = GlApi.GL_RGBA;
        OutColorMode = GlApi.GL_RGBA;

        // Generate OpenGL texture
        CoreHelper.gl.GenTextures(1, out var id);
        Id = id;
        SetData(_image.GetPixels());

        // Set default filtering and wrapping
        SetFiltering(Filtering.Linear);
        SetWrapping(Wrapping.Clamp);
        SetAnisotropy(CoreHelper.IncGetMaxAnisotropy() / 2.0f);
        UUID = NodeHelper.InCreateUUID();
    }

    /// <summary>
    /// Creates a new empty texture
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="channels"></param>
    public Texture(int width, int height, int channels = 4) : this(Marshal.AllocHGlobal(width * height * channels), width, height, channels, channels)
    {

    }

    /// <summary>
    /// Creates a new texture from specified data
    /// </summary>
    /// <param name="data"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="inChannels"></param>
    /// <param name="outChannels"></param>
    public Texture(IntPtr data, int width, int height, int inChannels = 4, int outChannels = 4)
    {
        Width = width;
        Height = height;
        Channels = outChannels;

        InColorMode = GlApi.GL_RGBA;
        OutColorMode = GlApi.GL_RGBA;
        if (inChannels == 1) InColorMode = GlApi.GL_RED;
        else if (inChannels == 2) InColorMode = GlApi.GL_RG;
        else if (inChannels == 3) InColorMode = GlApi.GL_RGB;

        // Generate OpenGL texture
        CoreHelper.gl.GenTextures(1, out var id);
        Id = id;
        SetData(data);

        // Set default filtering and wrapping
        SetFiltering(Filtering.Linear);
        SetWrapping(Wrapping.Clamp);
        SetAnisotropy(CoreHelper.IncGetMaxAnisotropy() / 2.0f);
        UUID = NodeHelper.InCreateUUID();
    }

    /// <summary>
    /// Disposes texture from GL
    /// </summary>
    public void Dispose()
    {
        _image?.Dispose();

        if (Id > 0)
        {
            CoreHelper.gl.DeleteTexture(Id);
            Id = 0;
        }
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
        CoreHelper.gl.TexParameterI(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MIN_FILTER,
            filtering == Filtering.Linear ? GlApi.GL_LINEAR_MIPMAP_LINEAR : GlApi.GL_NEAREST
        );

        CoreHelper.gl.TexParameterI(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MAG_FILTER,
            filtering == Filtering.Linear ? GlApi.GL_LINEAR : GlApi.GL_NEAREST
        );
    }

    public void SetAnisotropy(float value)
    {
        Bind();
        CoreHelper.gl.TexParameter(
            GlApi.GL_TEXTURE_2D,
            GlApi.GL_TEXTURE_MAX_ANISOTROPY,
            CoreHelper.Clamp(value, 1, CoreHelper.IncGetMaxAnisotropy())
        );
    }

    /// <summary>
    /// Set the wrapping mode used for the texture
    /// </summary>
    /// <param name="wrapping"></param>
    public void SetWrapping(Wrapping wrapping)
    {
        Bind();
        CoreHelper.gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_WRAP_S, (uint)wrapping);
        CoreHelper.gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_WRAP_T, (uint)wrapping);
        CoreHelper.gl.TexParameter(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_BORDER_COLOR, [0f, 0f, 0f, 0f]);
    }

    /// <summary>
    /// Sets the data of the texture
    /// </summary>
    /// <param name="data"></param>
    public void SetData(IntPtr data)
    {
        Bind();
        CoreHelper.gl.PixelStore(GlApi.GL_UNPACK_ALIGNMENT, 1);
        CoreHelper.gl.PixelStore(GlApi.GL_PACK_ALIGNMENT, 1);
        CoreHelper.gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, OutColorMode, Width, Height, 0, InColorMode, GlApi.GL_UNSIGNED_BYTE, data);

        GenMipmap();
    }

    /// <summary>
    /// Generate mipmaps
    /// </summary>
    public void GenMipmap()
    {
        Bind();
        CoreHelper.gl.GenerateMipmap(GlApi.GL_TEXTURE_2D);
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
            CoreHelper.gl.TexSubImage2D(GlApi.GL_TEXTURE_2D, 0, x, y, width, height, inChannelMode, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
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
        CoreHelper.gl.ActiveTexture(GlApi.GL_TEXTURE0 + (unit <= 31u ? unit : 31u));
        CoreHelper.gl.BindTexture(GlApi.GL_TEXTURE_2D, Id);
    }

    /// <summary>
    /// Saves the texture to file
    /// </summary>
    /// <param name="file"></param>
    public void Save(string file)
    {
        var bitmap = new SKBitmap(Width, Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        var temp = GetTextureData(true);
        bitmap.SetPixels(temp);
        bitmap.Save(file);
    }

    /// <summary>
    /// Gets the texture data for the texture
    /// </summary>
    /// <param name="unmultiply"></param>
    /// <returns></returns>
    public IntPtr GetTextureData(bool unmultiply = false)
    {
        long size = Width * Height * Channels;
        var buf = Marshal.AllocHGlobal(Width * Height * Channels);
        Bind();
        CoreHelper.gl.GetTexImage(GlApi.GL_TEXTURE_2D, 0, OutColorMode, GlApi.GL_UNSIGNED_BYTE, buf);
        if (unmultiply && Channels == 4)
        {
            CoreHelper.inTexUnPremuliply(buf, size);
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
}
