using System.Numerics;

namespace Inochi2dSharp.Core.Nodes;

public record MatrixHolder
{
    public Matrix4x4 Matrix;

    public MatrixHolder(Matrix4x4 matrix) => Matrix = matrix;
}