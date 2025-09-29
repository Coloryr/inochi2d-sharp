namespace Inochi2dSharp.Core.Render;

public record Texture : Resource
{
    private int _use;

    public override int Length => Data.Data.Length;

    /// <summary>
    /// Format of the texture.
    /// </summary>
    public TextureFormat Format => Data.Format;

    /// <summary>
    /// Width of the texture in pixels.
    /// </summary>
    public int Width => Data.Width;

    /// <summary>
    /// Height of the texture in pixels.
    /// </summary>
    public int Height => Data.Height;

    /// <summary>
    /// Channel count of the texture.
    /// </summary>
    public int Channels => Data.Channels;

    /// <summary>
    /// Pixel data of the texture.
    /// </summary>
    public byte[] Pixels => Data.Data;

    public TextureData Data { get; init; }

    /// <summary>
    /// Constructs a new texture.
    /// </summary>
    /// <param name="data"></param>
    public Texture(TextureData data)
    {
        Data = data;
    }

    /// <summary>
    /// Constructs a new texture.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="format"></param>
    public Texture(int width, int height, TextureFormat format)
    {
        Data = new TextureData()
        {
            Width = width,
            Height = height,
            Format = format
        };
    }

    /// <summary>
    /// Resizes the texture.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Resize(int width, int height)
    {
        Data.Resize(width, height);
    }

    public void Retain()
    {
        _use++;
    }

    public bool Released()
    {
        _use--;
        return _use != 0;
    }
}
