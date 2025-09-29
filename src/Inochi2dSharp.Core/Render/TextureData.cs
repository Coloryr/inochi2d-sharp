using StbImageSharp;
using StbImageWriteSharp;
using ColorComponents = StbImageSharp.ColorComponents;

namespace Inochi2dSharp.Core.Render;

public record TextureData
{
    public static TextureData Load(byte[] data)
    {
        var result = new TextureData();

        var img = ImageResult.FromMemory(data);
        result.Width = img.Width;
        result.Height = img.Height;
        result.Data = img.Data;
        result.Format = img.Comp == ColorComponents.Grey ? TextureFormat.R8 : TextureFormat.Rgba8Unorm;

        return result;
    }

    public static TextureData Load(Stream data)
    {
        var result = new TextureData();

        var img = ImageResult.FromStream(data);
        result.Width = img.Width;
        result.Height = img.Height;
        result.Data = img.Data;
        result.Format = img.Comp == ColorComponents.Grey ? TextureFormat.R8 : TextureFormat.Rgba8Unorm;

        return result;
    }

    public int Width { get; set; }
    public int Height { get; set; }
    public TextureFormat Format { get; set; }
    public byte[] Data { get; set; }

    public int Channels
    {
        get
        {
            return Format switch
            {
                TextureFormat.DepthStencil => 4,
                TextureFormat.Rgba8Unorm => 4,
                TextureFormat.R8 => 1,
                TextureFormat.None => 0,
                _ => 0,
            };
        }
    }

    /// <summary>
    /// Premultiplies incoming color data.
    /// </summary>
    public void Premultiply()
    {
        if (Format == TextureFormat.Rgba8Unorm)
        {
            for (int i = 0; i < Data.Length / 4; i++)
            {
                var offsetPixel = i * 4;

                float r = Data[offsetPixel + 0] / 255.0f * (Data[offsetPixel + 3] / 255.0f);
                float g = Data[offsetPixel + 1] / 255.0f * (Data[offsetPixel + 3] / 255.0f);
                float b = Data[offsetPixel + 2] / 255.0f * (Data[offsetPixel + 3] / 255.0f);

                Data[offsetPixel + 0] = (byte)(r * 255.0);
                Data[offsetPixel + 1] = (byte)(g * 255.0);
                Data[offsetPixel + 2] = (byte)(b * 255.0);
            }
        }
    }

    /// <summary>
    /// Un-premultiplies incoming color data.
    /// </summary>
    public void Unpremultiply()
    {
        if (Format == TextureFormat.Rgba8Unorm)
        {
            for (int i = 0; i < Data.Length / 4; i++)
            {
                var offsetPixel = i * 4;

                // Ensure no divide by zero happens.
                if (Data[offsetPixel + 3] == 0)
                {
                    Array.Clear(Data, offsetPixel, 3);
                    continue;
                }

                float r = Data[offsetPixel + 0] / 255.0f / (Data[offsetPixel + 3] / 255.0f);
                float g = Data[offsetPixel + 1] / 255.0f / (Data[offsetPixel + 3] / 255.0f);
                float b = Data[offsetPixel + 2] / 255.0f / (Data[offsetPixel + 3] / 255.0f);
                Data[offsetPixel + 0] = (byte)(r * 255.0);
                Data[offsetPixel + 1] = (byte)(g * 255.0);
                Data[offsetPixel + 2] = (byte)(b * 255.0);
            }
        }
    }

    /// <summary>
    /// Dumps the image data to the specified file.
    /// </summary>
    /// <param name="file">The file to dump the texture data to.</param>
    public void Dump(string file)
    {
        if (Data.Length > 0)
        {
            var writer = new ImageWriter();
            StbImageWriteSharp.ColorComponents temp = StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha;
            if (Format == TextureFormat.R8)
            {
                temp = StbImageWriteSharp.ColorComponents.Grey;
            }
            using var stream = File.Create(file);
            writer.WriteTga(Data, Width, Height, temp, stream);
        }
    }

    /// <summary>
    /// Pads the texture with a 1-pixel wide border.
    /// </summary>
    /// <param name="thickness">The border thickness to generate.</param>
    public void Pad(int thickness)
    {
        if (Data.Length == 0)
            return;

        int totalPad = thickness * 2;
        byte[] newData = new byte[(Width + totalPad) * (Height + totalPad) * Channels];

        var srcStride = Width * Channels;
        var dstStride = (Width + totalPad) * Channels;
        for (int y = 0; y < Height; y++)
        {
            var start = (dstStride * (y + thickness)) + (thickness * Channels);
            Array.Copy(Data, srcStride * y, newData, start, srcStride);
        }

        // Update the texture
        Data = newData;
        Width += totalPad;
        Height += totalPad;
    }

    /// <summary>
    /// Resizes the texture data, ensuring that if any data is supplied it is updated to fit within the new target size.
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void Resize(int width, int height)
    {
        if (Data.Length > 0)
        {
            byte[] newData = new byte[width * height * Channels];

            // Copy as many horizontal lines as requested
            // into our new buffer.
            var oldStride = Width * Channels;
            var newStride = width * Channels;
            var cStride = int.Min(oldStride, newStride);
            for (int y = 0; y < int.Min(Height, height); y++)
            {
                Array.Copy(Data, oldStride * y, newData, newStride * y, cStride);
            }

            Data = newData;
        }

        Width = width;
        Height = height;
    }

    /// <summary>
    /// Flip the texture vertically.
    /// </summary>
    public void Vflip()
    {
        if (Data.Length > 0)
        {
            var stride = Width * Channels;
            byte[] tmp = new byte[stride];
            for (int y = 0; y < Height / 2; y++)
            {
                int topStart = stride * y;
                int bottomStart = stride * (Height - (y + 1));
                Array.Copy(Data, topStart, tmp, 0, stride);

                Array.Copy(Data, bottomStart, Data, topStart, stride);

                Array.Copy(tmp, 0, Data, bottomStart, stride);
            }
        }
    }
}
