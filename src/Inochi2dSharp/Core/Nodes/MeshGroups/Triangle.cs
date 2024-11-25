using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes.MeshGroups;

public record Triangle
{
    public Matrix3x3 OffsetMatrices { get; set; }
    public Matrix3x3 TransformMatrix { get; set; }
}
