using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core;

/// <summary>
/// Vertex Data that gets submitted to the GPU.
/// </summary>
public struct VtxData
{
    public Vector3 Vtx;
    public Vector2 Uv;
}
