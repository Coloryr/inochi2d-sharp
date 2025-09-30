namespace Inochi2dSharp.OpenGL;

public record GLFramebuffer : IDisposable
{
    private readonly GlApi _gl;

    private int _width;
    private int _height;

    public uint Fb;

    public readonly List<GLTexture> Textures = [];

    public unsafe GLFramebuffer(GlApi gl, int width, int height)
    {
        _gl = gl;
        _width = width;
        _height = height;

        Fb = gl.GenFramebuffer();
    }

    public void Resize(int width, int height)
    {
        if (_width == width && _height == height)
            return;

        _width = width;
        _height = height;
        foreach (var texture in Textures) 
        {
            texture.Resize(width, height);
        }
    }

    public void ReattachAll()
    {
        uint i = 0;
        foreach (var texture in Textures)
        {
            _gl.FramebufferTexture2D(Fb, GlApi.GL_COLOR_ATTACHMENT0 + i, GlApi.GL_TEXTURE_2D, texture.TexId, 0);
            i++;
        }
    }

    public void Attach(GLTexture texture)
    {
        Textures.Add(texture);
    }

    public void BindAsTarget(uint offset = 0)
    {
        uint i = 0;
        foreach (var texture in Textures) 
        {
            _gl.ActiveTexture(offset + GlApi.GL_TEXTURE0 + i);
            _gl.BindTexture(GlApi.GL_TEXTURE_2D, texture.TexId);
            i++;
        }
    }

    public void Use()
    {
        _gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, Fb);
        ReattachAll();
    }

    public void BlitTo(GLFramebuffer? fb)
    {
        if (fb != null)
        {
            _gl.BindFramebuffer(GlApi.GL_READ_FRAMEBUFFER, Fb);
            _gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, fb.Fb);
            _gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, fb._width, fb._height, GlApi.GL_COLOR_BUFFER_BIT, GlApi.GL_LINEAR);
            return;
        }

        _gl.BindFramebuffer(GlApi.GL_READ_FRAMEBUFFER, Fb);
        _gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, 0);
        _gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GlApi.GL_COLOR_BUFFER_BIT, GlApi.GL_LINEAR);
    }

    public void BlitTo(uint fb)
    {
        _gl.BindFramebuffer(GlApi.GL_READ_FRAMEBUFFER, Fb);
        _gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, fb);
        _gl.BlitFramebuffer(0, 0, _width, _height, 0, 0, _width, _height, GlApi.GL_COLOR_BUFFER_BIT, GlApi.GL_LINEAR);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(Fb);
        foreach (var item in Textures)
        {
            item.Dispose();
        }
        Textures.Clear();
    }
}
