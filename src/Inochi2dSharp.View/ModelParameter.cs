using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.View;

public record ModelParameter
{
    public int Index { get; init; }
    public uint UUID { get; init; }
    public string Name { get; init; }
    public bool IsVec2 { get; init; }
    public Vector2 Value { get; init; }
    public Vector2 Min { get; init; }
    public Vector2 Max { get; init; }
    public Vector2 Default { get; init; }
}
