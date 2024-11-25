using System.Numerics;
using StbImageSharp;
using StbImageWriteSharp;

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

    public static void Save(byte[] bitmap, int width, int height, string file)
    {
        var writer = new ImageWriter();
        using var stream = File.Create(file);
        if (file.EndsWith(".png"))
        {
            writer.WritePng(bitmap, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
        else if (file.EndsWith(".tga"))
        {
            writer.WriteTga(bitmap, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
        else
        {
            writer.WriteBmp(bitmap, width, height, StbImageWriteSharp.ColorComponents.RedGreenBlueAlpha, stream);
        }
    }

    public static void Save(this ImageResult bitmap, string file)
    {
        Save(bitmap.Data, bitmap.Width, bitmap.Height, file);
    }
}
