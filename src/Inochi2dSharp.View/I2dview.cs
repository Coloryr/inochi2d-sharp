namespace Inochi2dSharp.View;

public partial class I2dView : IDisposable
{
    private readonly List<I2dModel> _models = [];

    public I2dView()
    {
        _gl = gl;

        _core = new(gl, null);
        _core.InCamera.Scale = new(0.1f);
    }

    public void SetView(int width, int height)
    {
        _core.InSetViewport(width, height);
    }

    public I2dModel LoadModel(string file)
    {
        var model = new I2dModel(file, _core.InLoadPuppet(file));
        _models.Add(model);

        return model;
    }

    public void Tick(float delta)
    {
        _core.TickTime(delta);
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT | GlApi.GL_DEPTH_BUFFER_BIT);
        _core.InBeginScene();
        foreach (var item in _models)
        {
            item.Update(delta);
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
            item.Dispose();
        }
        _models.Clear();
        _core.Dispose();
    }
}
