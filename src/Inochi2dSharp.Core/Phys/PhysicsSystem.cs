using System.Numerics;

namespace Inochi2dSharp.Core.Phys;

public abstract class PhysicsSystem : IDisposable
{
    private readonly Dictionary<IntPtr, ulong> variableMap = [];
    private readonly List<IntPtr> refs = [];
    private float[] derivative;

    private float t;

    /// <summary>
    /// Add a float variable to the simulation
    /// </summary>
    /// <param name="var"></param>
    /// <returns></returns>
    protected unsafe ulong AddVariable(float* var)
    {
        ulong index = (ulong)refs.Count;

        variableMap[new IntPtr(var)] = index;
        refs.Add(new IntPtr(var));

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
        derivative[index] = value;
    }

    /// <summary>
    /// Set the derivative of a float variable (solver input)
    /// </summary>
    /// <param name="var"></param>
    /// <param name="value"></param>
    protected unsafe void SetD(float* var, float value)
    {
        //ulong index = variableMap[new IntPtr(var)];
        SetD(variableMap[new IntPtr(var)], value);
    }

    /// <summary>
    /// Set the derivative of a vec2 variable (solver input)
    /// </summary>
    /// <param name="var"></param>
    /// <param name="value"></param>
    protected unsafe void SetD(Vector2* var, Vector2 value)
    {
        SetD(&var->X, value.X);
        SetD(&var->Y, value.Y);
    }

    protected unsafe float[] GetState()
    {
        float[] vals = new float[refs.Count];

        for (int i = 0; i < refs.Count; i++)
        {
            vals[i] = *(float*)refs[i];
        }

        return vals;
    }

    protected unsafe void SetState(float[] vals)
    {
        for (int i = 0; i < refs.Count; i++)
        {
            *(float*)refs[i] = vals[i];
        }
    }

    /**
        Evaluate the simulation at a given time
    */
    protected abstract void Eval(float t);

    /// <summary>
    /// Run a simulation tick (Runge-Kutta method)
    /// </summary>
    /// <param name="h"></param>
    public virtual unsafe void Tick(float h)
    {
        float[] cur = GetState();

        derivative = new float[cur.Length];

        Eval(t);
        float[] k1 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
        {
            *(float*)refs[i] = cur[i] + h * k1[i] / 2f;
        }
        Eval(t + h / 2f);
        float[] k2 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
            *(float*)refs[i] = cur[i] + h * k2[i] / 2f;
        Eval(t + h / 2f);
        float[] k3 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
            *(float*)refs[i] = cur[i] + h * k3[i];
        Eval(t + h);
        float[] k4 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
        {
            *(float*)refs[i] = cur[i] + h * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]) / 6f;
            if (!float.IsFinite(*(float*)refs[i]))
            {
                // Simulation failed, revert
                for (int j = 0; j < cur.Length; j++)
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

    public virtual void Dispose()
    {

    }
}
