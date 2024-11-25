using Inochi2dSharp.Core;

namespace Inochi2dSharp;

public partial class I2dCore
{
    private readonly List<Texture> _textureBindings = [];
    private bool _startedTexLoad = false;

    /// <summary>
    /// Gets the maximum level of anisotropy
    /// </summary>
    /// <returns></returns>
    public float IncGetMaxAnisotropy()
    {
        return gl.GetFloat(GlApi.GL_MAX_TEXTURE_MAX_ANISOTROPY);
    }

    /// <summary>
    /// Begins a texture loading pass
    /// </summary>
    /// <exception cref="Exception"></exception>
    public void InBeginTextureLoading()
    {
        if (_startedTexLoad)
        {
            throw new Exception("Texture loading pass already started!");
        }
        _startedTexLoad = true;
    }

    /// <summary>
    /// Returns a texture from the internal texture list
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public Texture InGetTextureFromId(uint id)
    {
        if (!_startedTexLoad)
        {
            throw new Exception("Texture loading pass not started!");
        }
        return _textureBindings[(int)id];
    }

    /// <summary>
    /// Gets the latest texture from the internal texture list
    /// </summary>
    /// <returns></returns>
    public Texture InGetLatestTexture()
    {
        return _textureBindings[^1];
    }

    /// <summary>
    /// Adds binary texture
    /// </summary>
    /// <param name="data"></param>
    public void InAddTextureBinary(ShallowTexture data)
    {
        _textureBindings.Add(new Texture(this, data));
    }

    /// <summary>
    /// Ends a texture loading pass
    /// </summary>
    /// <param name="checkErrors"></param>
    public void InEndTextureLoading(bool checkErrors = true)
    {
        if (checkErrors && !_startedTexLoad)
        {
            throw new Exception("Texture loading pass not started!");
        }
        _startedTexLoad = false;
        _textureBindings.Clear();
    }

    public void InTexPremultiply(byte[] data, int channels = 4)
    {
        if (channels < 4) return;

        for (int i = 0; i < data.Length / channels; i++)
        {
            var offsetPixel = i * channels;
            data[offsetPixel + 0] = (byte)(data[offsetPixel + 0] * data[offsetPixel + 3] / 255);
            data[offsetPixel + 1] = (byte)(data[offsetPixel + 1] * data[offsetPixel + 3] / 255);
            data[offsetPixel + 2] = (byte)(data[offsetPixel + 2] * data[offsetPixel + 3] / 255);
        }
    }

    public void InTexUnPremuliply(byte[] data, long size)
    {
        for (int i = 0; i < size / 4; i++)
        {
            if (data[(i * 4) + 3] == 0) continue;

            data[(i * 4) + 0] = (byte)(data[(i * 4) + 0] * 255 / data[(i * 4) + 3]);
            data[(i * 4) + 1] = (byte)(data[(i * 4) + 1] * 255 / data[(i * 4) + 3]);
            data[(i * 4) + 2] = (byte)(data[(i * 4) + 2] * 255 / data[(i * 4) + 3]);
        }
    }
}
