using Inochi2dSharp.Core;

namespace Inochi2dSharp.View;

public class I2dView : IDisposable
{
    private readonly GlApi _gl;
    private readonly I2dCore _core;

    private readonly List<Puppet> _models = [];

    public I2dView(GlApi gl)
    {
        _gl = gl;

        _core = new(gl, null);
        _core.InCamera.Scale = new(0.1f);
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

    public void Tick(float time)
    {
        //_core.TickTime(time);
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT | GlApi.GL_DEPTH_BUFFER_BIT);
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

    public void Dispose()
    {
        foreach (var item in _models)
        {
            item.JsonLoadDone();
        }
        _core.Dispose();
    }
}
    