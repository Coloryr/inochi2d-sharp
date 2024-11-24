﻿using SkiaSharp;

namespace Inochi2dSharp.Core;

public class ShallowTexture
{
    /// <summary>
    /// 8-bit RGBA color data
    /// </summary>
    public IntPtr Data { get; private set; }

    /// <summary>
    /// Width of texture
    /// </summary>
    public int Width { get; private set; }

    /// <summary>
    /// Height of texture
    /// </summary>
    public int Height { get; private set; }

    /// <summary>
    /// Amount of color channels
    /// </summary>
    public int Channels { get; private set; }

    /// <summary>
    /// Amount of channels to conver to when passed to OpenGL
    /// </summary>
    public int ConvChannels { get; private set; }

    private readonly SKBitmap _image;

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
        _image = SKBitmap.Decode(fData);

        // Copy data from IFImage to this ShallowTexture
        Data = _image.GetPixels();

        // Set the width/height data
        Width = _image.Width;
        Height = _image.Height;
        Channels = _image.GetChannel();
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
        _image = SKBitmap.Decode(buffer);

        // Copy data from IFImage to this ShallowTexture
        Data = _image.GetPixels();

        // Set the width/height data
        Width = _image.Width;
        Height = _image.Height;
        Channels = channels;
        ConvChannels = channels == 0 ? _image.GetChannel() : channels;
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
        _image = SKBitmap.Decode(buffer);
        Data = _image.GetPixels();

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
        _image = SKBitmap.Decode(buffer);
        Data = _image.GetPixels();
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