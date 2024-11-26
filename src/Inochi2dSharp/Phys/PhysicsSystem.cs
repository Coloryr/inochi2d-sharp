using System.Numerics;

namespace Inochi2dSharp.Phys;

public abstract class PhysicsSystem : IDisposable
{
    //float*
    private readonly Dictionary<IntPtr, int> variableMap = [];
    //float*
    private readonly List<IntPtr> refs = [];

    private float[] derivative = [];

    private float t = 0;

    /// <summary>
    /// Add a float variable to the simulation
    /// </summary>
    /// <param name="var"></param>
    /// <returns></returns>
    protected unsafe nint AddVariable(float* var)
    {
        var index = refs.Count;

        variableMap[new(var)] = index;
        refs.Add(new nint(var));

        return index;
    }

    /// <summary>
    /// Add a vec2 variable to the simulation
    /// </summary>
    /// <param name="var"></param>
    /// <returns></returns>
    protected unsafe nint AddVariable(Vector2* var)
    {
        var index = AddVariable(&var->X);
        AddVariable(&var->Y);
        return index;
    }

    /// <summary>
    /// Set the derivative of a variable (solver input) by index
    /// </summary>
    /// <param name="index"></param>
    /// <param name="value"></param>
    protected void SetD(int index, float value)
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
        var index = variableMap[new nint(var)];
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

    protected unsafe float[] GetState()
    {
        var vals = new float[refs.Count];

        for (int a=0;a<vals.Length;a++)
        {
            vals[a] = *(float*)refs[a];
        }

        return vals;
    }

    protected unsafe void SetState(float[] vals)
    {
        for (int idx = 0; idx < refs.Count; idx++)
        {
            *(float*)refs[idx] = vals[idx];
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
        {
            *(float*)refs[i] = cur[i] + h * k2[i] / 2f;
        }
        Eval(t + h / 2f);
        float[] k3 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
        {
            *(float*)refs[i] = cur[i] + h * k3[i];
        }
        Eval(t + h);
        float[] k4 = [.. derivative];

        for (int i = 0; i < cur.Length; i++)
        {
            *(float*)refs[i] = cur[i] + h * (k1[i] + 2 * k2[i] + 2 * k3[i] + k4[i]) / 6f;
            if (!(*(float*)refs[i]).IsFinite())
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

    /// <summary>
    /// Draw debug
    /// </summary>
    /// <param name="trans"></param>
    public abstract void DrawDebug(Matrix4x4 trans);
    public abstract void Dispose();
}
