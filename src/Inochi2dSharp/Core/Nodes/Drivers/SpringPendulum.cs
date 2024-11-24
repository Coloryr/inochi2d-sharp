﻿using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Phys;

namespace Inochi2dSharp.Core.Nodes.Drivers;

public class SpringPendulum : PhysicsSystem
{
    private readonly SimplePhysics _driver;

    private readonly unsafe Vector2* _bob = (Vector2*)Marshal.AllocHGlobal(Marshal.SizeOf<Vector2>());
    private readonly unsafe Vector2* _dBob = (Vector2*)Marshal.AllocHGlobal(Marshal.SizeOf<Vector2>());

    public unsafe SpringPendulum(SimplePhysics driver)
    {
        _driver = driver;

        *_bob = driver._anchor + new Vector2(0, driver.Length);

        AddVariable(_bob);
        AddVariable(_dBob);
    }

    public override unsafe void Tick(float h)
    {
        // Run the spring pendulum simulation
        base.Tick(h);

        _driver._output = *_bob;
    }

    public override unsafe void DrawDebug(Matrix4x4 trans)
    {
        Vector3[] points =
        [
            new Vector3(_driver._anchor.X, _driver._anchor.Y, 0),
            new Vector3(_bob->X, _bob->Y, 0),
        ];

        CoreHelper.inDbgSetBuffer(points);
        CoreHelper.inDbgLineWidth(3);
        CoreHelper.inDbgDrawLines(new Vector4(1, 0, 1, 1), trans);
    }

    public override unsafe void UpdateAnchor()
    {
        *_bob = _driver._anchor + new Vector2(0, _driver.Length);
    }

    protected override unsafe void Eval(float t)
    {
        SetD(_bob, _dBob);
        // These are normalized vs. mass
        float springKsqrt = _driver.Frequency * 2 * MathF.PI;
        float springK = MathF.Pow(springKsqrt, 2);

        float g = _driver.Gravity;
        float restLength = _driver.Length - g / springK;

        var offPos = *_bob - _driver._anchor;
        var offPosNorm = Vector2.Normalize(offPos);

        float lengthRatio = _driver.Gravity / _driver.Length;
        float critDampAngle = 2 * MathF.Sqrt(lengthRatio);
        float critDampLength = 2 * springKsqrt;

        float dist = float.Abs(Vector2.Distance(_driver._anchor, *_bob));
        var force = new Vector2(0, g);
        force -= offPosNorm * (dist - restLength) * springK;
        var ddBob = force;

        var dBobRot = new Vector2(
            _dBob->X * offPosNorm.Y + _dBob->Y * offPosNorm.X,
            _dBob->Y * offPosNorm.Y - _dBob->X * offPosNorm.X
        );

        var ddBobRot = -new Vector2(
            dBobRot.X * _driver.AngleDamping * critDampAngle,
            dBobRot.Y * _driver.LengthDamping * critDampLength
        );

        var ddBobDamping = new Vector2(
            ddBobRot.X * offPosNorm.Y - dBobRot.Y * offPosNorm.X,
            ddBobRot.Y * offPosNorm.Y + dBobRot.X * offPosNorm.X
        );

        ddBob += ddBobDamping;

        SetD(_dBob, &ddBob);
    }
}