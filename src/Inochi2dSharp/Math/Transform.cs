using System.Numerics;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Math;

/// <summary>
/// Initialize a transform
/// </summary>
/// <param name="translation"></param>
/// <param name="rotation"></param>
/// <param name="scale"></param>
public class Transform(Vector3 translation, Vector3 rotation, Vector2 scale)
{
    /// <summary>
    /// Gets the matrix for this transform
    /// </summary>
    public Matrix4x4 Matrix = Matrix4x4.Identity;

    /// <summary>
    /// The translation of the transform
    /// </summary>
    public Vector3 Translation = translation;

    /// <summary>
    /// The rotation of the transform
    /// </summary>
    public Vector3 Rotation = rotation;//; = quat.identity;

    /// <summary>
    /// The scale of the transform
    /// </summary>
    public Vector2 Scale = scale;

    /// <summary>
    /// Whether the transform should snap to pixels
    /// </summary>
    public bool PixelSnap = false;

    public Transform() : this(new(), new(0), new(1))
    {

    }

    public Transform CalcOffset(Transform other)
    {
        var tnew = new Transform
        {
            Translation = Translation + other.Translation,
            Rotation = Rotation + other.Rotation,
            Scale = Scale * other.Scale
        };
        tnew.Update();

        return tnew;
    }

    /// <summary>
    /// Updates the internal matrix of this transform
    /// </summary>
    public void Update()
    {
        Matrix =
            MathHelper.Translation(Translation) *
            MathHelper.EulerRotation(Rotation.X, Rotation.Y, Rotation.Z).ToMatrix() * 
            MathHelper.Scaling(Scale.X, Scale.Y, 1);
    }

    public void Clear()
    {
        Translation = new(0);
        Rotation = new(0);
        Scale = new(1);
    }

    public void Serialize(JsonObject obj)
    {
        obj.Add("trans", Translation.ToToken());
        obj.Add("rot", Rotation.ToToken());
        obj.Add("scale", Scale.ToToken());
    }

    public void Deserialize(JsonObject obj)
    {
        if (obj.TryGetPropertyValue("trans", out var temp) && temp is JsonArray array)
        {
            Translation = array.ToVector3();
        }

        if (obj.TryGetPropertyValue("rot", out temp) && temp is JsonArray array1)
        {
            Rotation = array1.ToVector3();
        }

        if (obj.TryGetPropertyValue("scale", out temp) && temp is JsonArray array2)
        {
            Scale = array2.ToVector2();
        }
    }

    /// <summary>
    ///  Returns the result of 2 transforms multiplied together
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public static Transform operator *(Transform v, Transform other)
    {
        var strs = other.Matrix * v.Matrix;

        var res = strs.Multiply(new Vector4(1, 1, 1, 1));

        var tnew = new Transform
        {
            // TRANSLATION
            Translation = new Vector3(res.X, res.Y, res.Z),
            // ROTATION
            Rotation = v.Rotation + other.Rotation,
            // SCALE
            Scale = v.Scale * other.Scale,
            Matrix = strs
        };
        return tnew;
    }

}
