using Inochi2dSharp.Core;

namespace Inochi2dSharp.View;

public class I2dView : IDisposable
{
    private readonly DateTime _time;

    private readonly GlApi _gl;
    private readonly I2dCore _core;

    private readonly List<Puppet> _models = [];

    public I2dView(GlApi gl)
    {
        _gl = gl;

        _time = DateTime.Now;

        _core = new(gl, GetTime);
        _core.InCamera.Scale = new(1);
    }

    public void SetView(int width, int height)
    {
        _core.InSetViewport(width, height);
    }

    public Puppet LoadModel(string file)
    {
        var model = _core.InLoadPuppet(file);
        _models.Add(model);

        return model;
    }

    public void Tick()
    {
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT | GlApi.GL_DEPTH_BUFFER_BIT);
        _core.InUpdate();
        _core.InBeginScene();
        foreach (var item in _models)
        {
            item.Update();
            item.Draw();
        }
        _core.InEndScene();
        _core.InGetViewport(out var width, out var height);
        _core.InDrawScene(new(0, 0, width, height));
    }

    private float GetTime()
    {
        var time = DateTime.Now;
        var less = _time - time;
        return (float)less.TotalSeconds;
    }

    public void Dispose()
    {
        foreach (var item in _models)
        {
            item.Dispose();
        }
        _core.Dispose();
    }
}
    