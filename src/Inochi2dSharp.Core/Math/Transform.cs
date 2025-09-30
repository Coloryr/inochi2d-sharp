using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Math;

public struct Transform
{
    private Matrix4x4 trs = Matrix4x4.Identity;

    /// <summary>
    /// The translation of the transform
    /// </summary>
    public Vector3 Translation = new(0, 0, 0);

    /// <summary>
    /// The rotation of the transform
    /// </summary>
    public Vector3 Rotation = new(0, 0, 0);//; = quat.identity;

    /// <summary>
    /// The scale of the transform
    /// </summary>
    public Vector2 Scale = new(1, 1);

    /// <summary>
    /// Whether the transform should snap to pixels
    /// </summary>
    public bool PixelSnap = false;

    public Transform()
    {
    }

    /// <summary>
    /// Calculates offset to other vector.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public Transform CalcOffset(Transform other)
    {
        var tnew = new Transform()
        {
            Translation = Translation + other.Translation,
            Rotation = Rotation + other.Rotation,
            Scale = Scale * other.Scale
        };
        tnew.Update();

        return tnew;
    }

    /// <summary>
    /// Gets the matrix for this transform
    /// </summary>
    /// <returns></returns>
    public readonly Matrix4x4 Matrix => trs;

    /// <summary>
    /// Updates the internal matrix of this transform
    /// </summary>
    public void Update()
    {
        trs = MathHelper.Translation(Translation) *
            MathHelper.EulerRotation(Rotation.X, Rotation.Y, Rotation.Z).ToMatrix() *
            MathHelper.Scaling(Scale.X, Scale.Y, 1);
    }

    /// <summary>
    /// Clears the vector
    /// </summary>
    public void Clear()
    {
        Translation = new Vector3(0);
        Rotation = new Vector3(0);
        Scale = new Vector2(1, 1);
    }

    /// <summary>
    /// Gets a string representation of the transform.
    /// </summary>
    /// <returns></returns>
    public override readonly string ToString()
    {
        return $"{Translation}, {Rotation}, {Scale}";
    }

    /// <summary>
    ///  Serializes the transform.
    /// </summary>
    /// <param name=""></param>
    /// <param name=""></param>
    public readonly void Serialize(JsonObject obj)
    {
        obj["trans"] = Translation.ToToken();
        obj["rot"] = Rotation.ToToken();
        obj["scale"] = Scale.ToToken();
    }

    /// <summary>
    /// Deserializes a transform from JSON.
    /// </summary>
    /// <param name=""></param>
    /// <param name=""></param>
    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "trans" && item.Value.ValueKind is JsonValueKind.Array)
            {
                Translation = item.Value.ToVector3();
            }
            else if (item.Name == "rot" && item.Value.ValueKind is JsonValueKind.Array)
            {
                Rotation = item.Value.ToVector3();
            }
            else if (item.Name == "scale" && item.Value.ValueKind is JsonValueKind.Array)
            {
                Scale = item.Value.ToVector2();
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform operator *(Transform v, Transform other)
    {
        var strs = other.trs * v.trs;

        var res = strs.Multiply(new Vector4(1, 1, 1, 1));

        return new Transform
        {
            // TRANSLATION
            Translation = new Vector3(res.X, res.Y, res.Z),
            // ROTATION
            Rotation = v.Rotation + other.Rotation,
            // SCALE
            Scale = v.Scale * other.Scale,
            trs = strs
        };
    }
}
