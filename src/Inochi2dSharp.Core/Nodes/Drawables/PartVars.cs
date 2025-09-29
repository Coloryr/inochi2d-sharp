using System.Numerics;
using System.Runtime.InteropServices;

namespace Inochi2dSharp.Core.Nodes.Drawables;

[StructLayout(LayoutKind.Sequential)]
public struct PartVars
{
    public Vector3 Tint;
    public Vector3 ScreenTint;
    public float Opacity;
    public float EmissionStrength;
}

public static class PartVarsHelper
{
    public static readonly int Size = Marshal.SizeOf<PartVars>();
}