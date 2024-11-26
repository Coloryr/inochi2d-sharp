using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Animations;

namespace Inochi2dSharp.View;

public class I2dModel : IDisposable
{
    private readonly Puppet _model;

    private readonly AnimationPlayer _animation;

    public I2dModel(Puppet model)
    {
        _model = model;
        _animation = new(model);
    }

    public void Update(float delta)
    {
        _model.Update();
        _animation.Update(delta);
    }

    public void Draw()
    {
        _animation.PrerenderAll();
        _model.Draw();
    }

    public void Dispose()
    {
        _animation.StopAll(true);
        _animation.Dispose();
        _model.Dispose();
    }
}
