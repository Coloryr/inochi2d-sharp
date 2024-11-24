using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes.MeshGroups;

public record Triangle
{
    public Matrix3x3 offsetMatrices;
    public Matrix3x3 transformMatrix;
}
