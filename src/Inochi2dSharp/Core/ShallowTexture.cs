using StbImageSharp;

namespace Inochi2dSharp.Core;

public class ShallowTexture
{
    /// <summary>
    /// 8-bit RGBA color data
    /// </summary>
    public byte[] Data;

    /// <summary>
    /// Width of texture
    /// </summary>
    public int Width;

    /// <summary>
    /// Height of texture
    /// </summary>
    public int Height;

    /// <summary>
    /// Amount of color channels
    /// </summary>
    public int Channels;

    /// <summary>
    /// Amount of channels to conver to when passed to OpenGL
    /// </summary>
    public int ConvChannels;

    private readonly ImageResult _image;

    /// <summary>
    /// Loads a shallow texture from image file
    /// Supported file types:
    /// * PNG 8-bit
    /// * BMP 8-bit
    /// * TGA 8-bit non-palleted
    /// * JPEG baseline
    /// </summary>
    /// <param name="file"></param>
    /// <param name="channels"></param>
    public ShallowTexture(string file, int channels = 0)
    {
        // Ensure we keep this ref alive until we're done with it
        var fData = File.ReadAllBytes(file);

        // Load image from disk, as <channels> 8-bit
        _image = ImageResult.FromMemory(fData, ColorComponents.RedGreenBlueAlpha);

        // Copy data from IFImage to this ShallowTexture
        Data = _image.Data;

        // Set the width/height data
        Width = _image.Width;
        Height = _image.Height;
        Channels = (int)_image.SourceComp;
        ConvChannels = channels == 0 ? Channels : channels;
    }

    /// <summary>
    /// Loads uncompressed texture from memory
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="channels"></param>
    public ShallowTexture(byte[] buffer, int channels = 0)
    {
        // Load image from disk, as < channels > 8 - bit
        _image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);

        // Copy data from IFImage to this ShallowTexture
        Data = _image.Data;

        // Set the width/height data
        Width = _image.Width;
        Height = _image.Height;
        Channels = channels;
        ConvChannels = channels == 0 ? (int)_image.SourceComp : channels;
    }

    /// <summary>
    /// Loads uncompressed texture from memory
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="channels"></param>
    public ShallowTexture(byte[] buffer, int w, int h, int channels = 4)
    {
        _image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
        Data = _image.Data;

        // Set the width/height data
        Width = w;
        Height = h;
        Channels = channels;
        ConvChannels = channels;
    }

    /// <summary>
    /// Loads uncompressed texture from memory
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="w"></param>
    /// <param name="h"></param>
    /// <param name="channels"></param>
    /// <param name="convChannels"></param>
    public ShallowTexture(byte[] buffer, int w, int h, int channels = 4, int convChannels = 4)
    {
        _image = ImageResult.FromMemory(buffer, ColorComponents.RedGreenBlueAlpha);
        Data = _image.Data;
        // Set the width/height data
        Width = w;
        Height = h;
        Channels = channels;
        ConvChannels = convChannels;
    }

    /// <summary>
    /// Saves image
    /// </summary>
    /// <param name="file"></param>
    public void Save(string file)
    {
        _image.Save(file);
    }
}
