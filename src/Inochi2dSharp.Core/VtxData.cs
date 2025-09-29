using System.Numerics;
using System.Runtime.InteropServices;

namespace Inochi2dSharp.Core;

/// <summary>
/// Vertex Data that gets submitted to the GPU.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct VtxData
{
    public Vector2 Vtx;
    public Vector2 Uv;
}
