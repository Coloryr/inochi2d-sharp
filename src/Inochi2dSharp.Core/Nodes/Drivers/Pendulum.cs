using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Core.Phys;

namespace Inochi2dSharp.Core.Nodes.Drivers;

public unsafe class Pendulum : PhysicsSystem
{
    private readonly SimplePhysics _driver;

    private Vector2 _bob = new(0, 0);
    private unsafe float* _angle;
    private unsafe float* _dAngle;

    public unsafe Pendulum(SimplePhysics driver)
    {
        _driver = driver;

        _bob = driver.Anchor + new Vector2(0, driver.Length);

        _angle = (float*)Marshal.AllocHGlobal(sizeof(float));
        _dAngle = (float*)Marshal.AllocHGlobal(sizeof(float));

        *_angle = 0;
        *_dAngle = 0;

        AddVariable(_angle);
        AddVariable(_dAngle);
    }

    public override unsafe void Dispose()
    {
        if (_angle != null)
        {
            Marshal.FreeHGlobal(new nint(_angle));
            _angle = null;
        }
        if (_dAngle != null)
        {
            Marshal.FreeHGlobal(new nint(_dAngle));
            _dAngle = null;
        }
    }

    public override unsafe void Tick(float h)
    {
        // Compute the angle against the updated anchor position
        var dBob = _bob - _driver.Anchor;
        *_angle = MathF.Atan2(-dBob.X, dBob.Y);

        // Run the pendulum simulation in terms of angle
        base.Tick(h);

        // Update the bob position at the new angle
        dBob = new(-MathF.Sin(*_angle), MathF.Cos(*_angle));
        _bob = _driver.Anchor + dBob * _driver.Length;

        _driver.Output = _bob;
    }

    public override void UpdateAnchor()
    {
        _bob = _driver.Anchor + new Vector2(0, _driver.Length);
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
