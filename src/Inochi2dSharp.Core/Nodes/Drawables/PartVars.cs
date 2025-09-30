using System.Numerics;
using System.Runtime.InteropServices;

namespace Inochi2dSharp.Core.Nodes.Drawables;

[StructLayout(LayoutKind.Explicit)]
public struct PartVars
{
    [FieldOffset(0)] public Vector3 Tint;
    [FieldOffset(16)] public Vector3 ScreenTint;
    [FieldOffset(28)] public float Opacity;
    [FieldOffset(36)] public float EmissionStrength;
}

public static class PartVarsHelper
{
    public static readonly int Size = Marshal.SizeOf<PartVars>();
}