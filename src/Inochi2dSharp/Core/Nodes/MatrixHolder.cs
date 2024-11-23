using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public record MatrixHolder
{
    public Matrix4x4 Matrix;

    public MatrixHolder(Matrix4x4 matrix) => Matrix = matrix;
}