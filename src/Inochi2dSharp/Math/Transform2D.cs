using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Math;

public class Transform2D
{
    /// <summary>
    /// Gets the matrix for this transform
    /// </summary>
    public Matrix3x3 Matrix { get; private set; }

    /// <summary>
    /// Translate
    /// </summary>
    public Vector2 Translation;
    /// <summary>
    /// Scale
    /// </summary>
    public Vector2 Scale;
    /// <summary>
    /// Rotation
    /// </summary>
    public float Rotation;

    /// <summary>
    /// Updates the internal matrix of this transform
    /// </summary>
    public void Update()
    {
        var translation_ = Matrix3x3.CreateTranslation(new(Translation, 0));
        var rotation_ = Matrix3x3.CreateRotationZ(Rotation);
        var scale_ = Matrix3x3.CreateScale(Scale.X, Scale.Y, 1);
        Matrix = translation_ * rotation_ * scale_;
    }
}
