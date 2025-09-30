using System.Numerics;
using Inochi2dSharp.Core.Format.Inp;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.View;

public partial class I2dView : IDisposable
{
    private readonly List<I2dModel> _models = [];

    private readonly Camera2D _cam;
    private readonly IRender _render;

    public I2dView(IRender render, int width, int height, float scale)
    {
        _render = render;
        _render.SetSize(width, height);
        _cam = new Camera2D()
        {
            Scale = scale,
            Size = new Vector2((uint)width, (uint)height)
        };
        _cam.Update();
    }

    public I2dModel LoadModel(string file)
    {
        var model = new I2dModel(file, BinFmt.InLoadPuppet(file));
        _models.Add(model);
        _render.AddPuppet(model.Model);
        return model;
    }

    public void SetSize(int width, int height)
    {
        _cam.Size = new Vector2((uint)width, (uint)height);
        _cam.Update();
        _render.SetSize(width, height);
    }

    public void Tick(float delta, uint fb)
    {
        _render.PreRender();

        foreach (var item in _models)
        {
            item.Update(delta);
            item.Draw(delta);

            _render.Render(item.Model, _cam);
        }

        _render.PostRender(fb);
    }

    public void Dispose()
    {
        foreach (var item in _models)
        {
            item.Dispose();
        }
        _models.Clear();
    }
}
