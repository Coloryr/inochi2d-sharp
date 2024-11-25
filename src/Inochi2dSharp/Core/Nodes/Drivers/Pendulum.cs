using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Phys;

namespace Inochi2dSharp.Core.Nodes.Drivers;

public class Pendulum : PhysicsSystem
{
    private readonly SimplePhysics _driver;

    private Vector2 _bob = new(0, 0);
    private readonly unsafe float* _angle = (float*)Marshal.AllocHGlobal(sizeof(float));
    private readonly unsafe float* _dAngle = (float*)Marshal.AllocHGlobal(sizeof(float));

    private readonly I2dCore _core;

    public unsafe Pendulum(I2dCore core, SimplePhysics driver)
    {
        _driver = driver;
        _core = core;

        _bob = driver._anchor + new Vector2(0, driver.Length);

        AddVariable(_angle);
        AddVariable(_dAngle);
    }

    public override unsafe void Dispose()
    {
        Marshal.FreeHGlobal(new nint(_angle));
        Marshal.FreeHGlobal(new nint(_dAngle));
    }

    public override unsafe void Tick(float h)
    {
        // Compute the angle against the updated anchor position
        var dBob = _bob - _driver._anchor;
        *_angle = float.Atan2(-dBob.X, dBob.Y);

        // Run the pendulum simulation in terms of angle
        base.Tick(h);

        // Update the bob position at the new angle
        dBob = new(-float.Sin(*_angle), float.Cos(*_angle));
        _bob = _driver._anchor + dBob * _driver.Length;

        _driver._output = _bob;
    }

    public override void DrawDebug(Matrix4x4 trans)
    {
        Vector3[] points = [
            new Vector3(_driver._anchor.X, _driver._anchor.Y, 0),
            new Vector3(_bob.X, _bob.Y, 0),
        ];

        _core.InDbgSetBuffer(points);
        _core.InDbgLineWidth(3);
        _core.InDbgDrawLines(new Vector4(1, 0, 1, 1), trans);
    }

    public override void UpdateAnchor()
    {
        _bob = _driver._anchor + new Vector2(0, _driver.Length);
    }

    protected override unsafe void Eval(float t)
    {
        SetD(_angle, *_dAngle);
        float lengthRatio = _driver.Gravity / _driver.Length;
        float critDamp = 2 * float.Sqrt(lengthRatio);
        float dd = -lengthRatio * MathF.Sin(*_angle);
        dd -= *_dAngle * _driver.AngleDamping * critDamp;
        SetD(_dAngle, dd);
    }
}
