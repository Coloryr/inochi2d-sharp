﻿using System.Numerics;

namespace Inochi2dSharp.Phys;

public abstract class PhysicsSystem : IDisposable
{
    //float*
    private readonly Dictionary<IntPtr, ulong> variableMap = [];
    //float*
    private readonly List<IntPtr> refs = [];

    private float[] derivative = [];

    private float t;

    /// <summary>
    /// Add a float variable to the simulation
    /// </summary>
    /// <param name="var"></param>
    /// <returns></returns>
    protected unsafe ulong AddVariable(float* var)
    {
        ulong index = (ulong)refs.Count;

        variableMap[new(var)] = index;
        refs.Add(new nint(var));

        return index;
    }

    /// <summary>
    /// Add a vec2 variable to the simulation
    /// </summary>
    /// <param name="var"></param>
    /// <returns></returns>
    protected unsafe ulong AddVariable(Vector2* var)
    {
        ulong index = AddVariable(&var->X);
        AddVariable(&var->Y);
        return index;
    }

    /// <summary>
    /// Set the derivative of a variable (solver input) by index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    protected void SetD(ulong index, float value)
    {
        derivative[(int)index] = value;
    }

    /// <summary>
    /// Set the derivative of a float variable (solver input)
    /// </summary>
    /// <param name="var"></param>
    /// <param name="value"></param>
    protected unsafe void SetD(float* var, float value)
    {
        ulong index = variableMap[new nint(var)];
        SetD(index, value);
    }

    /// <summary>
    /// Set the derivative of a vec2 variable (solver input)
    /// </summary>
    /// <param name="var"></param>
    /// <param name="value"></param>
    protected unsafe void SetD(Vector2* var, Vector2* value)
    {
        SetD(&var->X, value->X);
        SetD(&var->Y, value->Y);
    }

    protected unsafe List<float> GetState()
    {
        var vals = new List<float>();

        foreach (var ptr in refs)
        {
            vals.Add(*(float*)ptr);
        }

        return vals;
    }

    protected unsafe void SetState(float[] vals)
    {
        for (int idx = 0; idx < refs.Count; idx++)
        {
            var ptr = refs[idx];
            *(float*)ptr = vals[idx];
        }
    }

    /// <summary>
    /// Evaluate the simulation at a given time
    /// </summary>
    /// <param name="t"></param>
    protected abstract void Eval(float t);

    /// <summary>
    /// Run a simulation tick (Runge-Kutta method)
    /// </summary>
    /// <param name="h"></param>
    public unsafe virtual void Tick(float h)
    {
        var cur = GetState();
        var tmp = new float[cur.Count];
        derivative = new float[cur.Count];
        for (var i = 0; i < cur.Count; i++)
        {
            derivative[i] = 0;
        }

        Eval(t);
        float[] k1 = [.. derivative];

        for (int i = 0; i < cur.Count; i++)
            *(float*)refs[i] = cur[i] + h * k1[i] / 2f;
        Eval(t + h / 2f);
        float[] k2 = [.. derivative];

        for (int i = 0; i < cur.Count; i++)
            *(float*)refs[i] = cur[i] + h * k2[i] / 2f;
        Eval(t + h / 2f);
        float[] k3 = [.. derivative];

        for (int i = 0; i < cur.Count; i++)
            *(float*)refs[i] = cur[i] + h * k3[i];
        Eval(t + h);
        float[] k4 = [.. derivative];

        for (int i = 0; i < cur.Count; i++)
        {
            *(float*)refs[i] = cur[i] + h * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]) / 6f;
            if (!(*(float*)refs[i]).IsFinite())
            {
                // Simulation failed, revert
                for (int j = 0; j < cur.Count; j++)
                    *(float*)refs[j] = cur[j];
                break;
            }
        }

        t += h;
    }

    /// <summary>
    /// Updates the anchor for the physics system
    /// </summary>
    public abstract void UpdateAnchor();

    /// <summary>
    /// Draw debug
    /// </summary>
    /// <param name="trans"></param>
    public abstract void DrawDebug(Matrix4x4 trans);
    public abstract void Dispose();
}
