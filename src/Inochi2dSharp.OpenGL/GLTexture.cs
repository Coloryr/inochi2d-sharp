namespace Inochi2dSharp.OpenGL;

public record GLTexture : IDisposable
{
    private readonly GlApi _gl;
    private readonly int _color;
    private int _width;
    private int _height;

    public uint TexId { get; private set; } = uint.MaxValue;

    public GLTexture(GlApi gl, uint color, int width, int height)
    {
        _gl = gl;
        _color = (int)color;
        _width = width;
        _height = height;

        TexId = gl.GenTexture();
        gl.BindTexture(GlApi.GL_TEXTURE_2D, TexId);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, (int)color, width, height, 0, (int)color, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_WRAP_S, GlApi.GL_CLAMP_TO_BORDER);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_WRAP_T, GlApi.GL_CLAMP_TO_BORDER);
    }

    public unsafe GLTexture(GlApi gl, uint color, int width, int height, byte[] data)
    {
        _gl = gl;
        _color = (int)color;
        _width = width;
        _height = height;

        TexId = gl.GenTexture();
        gl.BindTexture(GlApi.GL_TEXTURE_2D, TexId);
        fixed (void* ptr = data)
        {
            gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, (int)color, width, height, 0, (int)color, GlApi.GL_UNSIGNED_BYTE, new IntPtr(ptr));
        }
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_WRAP_S, GlApi.GL_CLAMP_TO_BORDER);
        gl.TextureParameteri(TexId, GlApi.GL_TEXTURE_WRAP_T, GlApi.GL_CLAMP_TO_BORDER);
    }

    public void Resize(int width, int height)
    {
        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;

        _gl.BindTexture(GlApi.GL_TEXTURE_2D, TexId);
        _gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, _color, width, height, 0, _color, GlApi.GL_UNSIGNED_BYTE, 0);
    }

    public void Bind(uint unit)
    {
        _gl.ActiveTexture(unit + GlApi.GL_TEXTURE0);
        _gl.BindTexture(GlApi.GL_TEXTURE_2D, TexId);
    }

    public void Dispose()
    {
        if (TexId != uint.MaxValue)
        {
            _gl.DeleteTexture(TexId);
            TexId = uint.MaxValue;
        }
    }
}
