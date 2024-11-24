using System.Numerics;
using SkiaSharp;

namespace Inochi2dSharp;

public static class Helper
{
    public static bool IsFinite(this Vector2 vector)
    {
        for (int a = 0; a < 2; a++)
        {
            if (!vector[a].IsFinite())
            {
                return false;
            }
        }

        return true;
    }

    public static bool IsFinite(this float vector)
    {
        if (float.IsNaN(vector) || float.IsInfinity(vector))
        {
            return false;
        }

        return true;
    }

    public static void Save(this SKBitmap bitmap, string file)
    {
        byte[] temp;
        if (file.EndsWith(".png"))
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsSpan().ToArray();
        }
        else if (file.EndsWith(".jpg"))
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100).AsSpan().ToArray();
        }
        else
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Bmp, 100).AsSpan().ToArray();
        }

        File.WriteAllBytes(file, temp);
    }

    public static int GetChannel(this SKBitmap bitmap)
    {
        var type = bitmap.ColorType;
        if (type == SKColorType.Rgba8888 || type == SKColorType.Bgra8888)
        {
            return 4;
        }

        return 0;
    }
}
