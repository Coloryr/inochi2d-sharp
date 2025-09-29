using System.Numerics;
using System.Runtime.InteropServices;

namespace Inochi2dSharp.Core.Nodes.Composites;

[StructLayout(LayoutKind.Sequential)]
public struct CompositeVars
{
    public Vector3 Tint;
    public Vector3 ScreenTint;
    public float Opacity;
}

public static class CompositeVarsHelper
{
    public static readonly int Size = Marshal.SizeOf<CompositeVars>();
}