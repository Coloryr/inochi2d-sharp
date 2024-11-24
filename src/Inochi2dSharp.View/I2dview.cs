using Inochi2dSharp.Core;

namespace Inochi2dSharp.View;

public class I2dview
{
    private readonly DateTime _time;

    private readonly GlApi _gl;
    private readonly I2dCore _core;

    public I2dview(GlApi gl)
    {
        _gl = gl;

        _time = DateTime.Now;

        _core = new(gl, GetTime);
        _core.InCamera.Scale = new(1);
    }

    private float GetTime()
    {
        var time = DateTime.Now;
        var less = _time - time;
        return (float)less.TotalSeconds;
    }
}
