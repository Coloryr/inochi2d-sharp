using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Core.Phys;

namespace Inochi2dSharp.Core.Nodes.Drivers;

public class SpringPendulum : PhysicsSystem
{
    private readonly SimplePhysics _driver;

    private unsafe Vector2* _bob = (Vector2*)Marshal.AllocHGlobal(Marshal.SizeOf<Vector2>());
    private unsafe Vector2* _dBob = (Vector2*)Marshal.AllocHGlobal(Marshal.SizeOf<Vector2>());

    public unsafe SpringPendulum(SimplePhysics driver)
    {
        _driver = driver;

        _bob->X = driver.Anchor.X;
        _bob->Y = driver.Anchor.Y + driver.FinalLength;
        _dBob->X = 0;
        _dBob->Y = 0;

        AddVariable(_bob);
        AddVariable(_dBob);
    }

    public override unsafe void Tick(float h)
    {
        // Run the spring pendulum simulation
        base.Tick(h);

        _driver.Output = *_bob;
    }

    public override unsafe void UpdateAnchor()
    {
        _bob->X = _driver.Anchor.X;
        _bob->Y = _driver.Anchor.Y + _driver.FinalLength;
    }

    protected override unsafe void Eval(float t)
    {
        SetD(_bob, *_dBob);
        // These are normalized vs. mass
        float springKsqrt = _driver.FinalFrequency * 2 * MathF.PI;
        float springK = MathF.Pow(springKsqrt, 2);

        float g = _driver.FinalGravity;
        float restLength = _driver.FinalLength - g / springK;

        var offPos = *_bob - _driver.Anchor;
        var offPosNorm = Vector2.Normalize(offPos);

        float lengthRatio = _driver.FinalGravity / _driver.FinalLength;
        float critDampAngle = 2 * MathF.Sqrt(lengthRatio);
        float critDampLength = 2 * springKsqrt;

        float dist = float.Abs(Vector2.Distance(_driver.Anchor, *_bob));
        var force = new Vector2(0, g);
        force -= offPosNorm * (dist - restLength) * springK;
        var ddBob = force;

        var dBobRot = new Vector2(
            _dBob->X * offPosNorm.Y + _dBob->Y * offPosNorm.X,
            _dBob->Y * offPosNorm.Y - _dBob->X * offPosNorm.X
        );

        var ddBobRot = -new Vector2(
            dBobRot.X * _driver.FinalAngleDamping * critDampAngle,
            dBobRot.Y * _driver.FinalLengthDamping * critDampLength
        );

        var ddBobDamping = new Vector2(
            ddBobRot.X * offPosNorm.Y - dBobRot.Y * offPosNorm.X,
            ddBobRot.Y * offPosNorm.Y + dBobRot.X * offPosNorm.X
        );

        ddBob += ddBobDamping;

        SetD(_dBob, ddBob);
    }

    public override unsafe void Dispose()
    {
        if (_bob != null)
        {
            Marshal.FreeHGlobal(new nint(_bob));
            _bob = null;
        }
        if (_dBob != null)
        {
            Marshal.FreeHGlobal(new nint(_dBob));
            _dBob = null;
        }
    }
}
