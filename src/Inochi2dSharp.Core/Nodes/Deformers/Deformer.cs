using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.Core.Nodes.Deformers;

/// <summary>
/// A node which deforms the vertex data of nodes beneath it.<br/>
///  Deformations happen in world space
/// </summary>
[TypeId("Deformer", 0x0002, true)]
public abstract class Deformer : Node, IDeformable
{
    /// <summary>
    /// A list of the nodes to deform.
    /// </summary>
    protected readonly List<IDeformable> ToDeform = [];

    /// <summary>
    /// The control points of the deformer.
    /// </summary>
    public abstract Vector2[] ControlPoints { get; set; }

    /// <summary>
    /// The base position of the deformable's points.
    /// </summary>
    /// <returns></returns>
    public abstract Vector2[] BasePoints { get; }

    /// <summary>
    /// Local matrix of the deformable object.
    /// </summary>
    public Transform BaseTransform => Transform(true);

    /// <summary>
    /// World matrix of the deformable object.
    /// </summary>
    public Transform WorldTransform => Transform(false);

    /// <summary>
    /// The points which may be deformed by the deformer.
    /// </summary>
    public virtual Vector2[] DeformPoints => ControlPoints;

    public Deformer()
    {

    }

    /// <summary>
    /// Constructs a new MeshGroup node
    /// </summary>
    /// <param name="parent"></param>
    public Deformer(Node? parent = null) : base(parent)
    {

    }

    /// <summary>
    /// Deforms the IDeformable.
    /// </summary>
    /// <param name="deformed">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public virtual void Deform(Vector2[] deformed, bool absolute)
    {
        var m = int.Min(DeformPoints.Length, deformed.Length);
        if (absolute)
        {
            Array.Copy(deformed, DeformPoints, m);
        }
        else
        {
            for (int a = 0; a < m; a++)
            {
                DeformPoints[a] += deformed[a];
            }
        }
    }

    /// <summary>
    /// Deforms a single vertex in the IDeformable
    /// </summary>
    /// <param name="offset">The offset into the point list to deform.</param>
    /// <param name="deform">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public void Deform(int offset, Vector2 deform, bool absolute = false)
    {
        if (offset >= DeformPoints.Length)
            return;

        if (absolute)
            DeformPoints[offset] = deform;
        else
            DeformPoints[offset] += deform;
    }

    /// <summary>
    /// Applies an offset to the Node's transform.
    /// </summary>
    /// <param name="other">The transform to offset the current global transform by.</param>
    public override void OffsetTransform(Transform other)
    {
        base.OffsetTransform(other);
    }

    /// <summary>
    /// Resets the deformation for the IDeformable.
    /// </summary>
    public abstract void ResetDeform();

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name=""></param>
    /// <param name=""></param>
    /// <param name="recursive"></param>
    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);
    }

    public virtual void Rescan()
    {
        ToDeform.Clear();
        foreach (var child in Children)
        {
            ScanPartsRecurse(child);
        }
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);
    }

    /// <summary>
    /// Finalizes the deformer.
    /// </summary>
    public override void Finalized()
    {
        base.Finalized();
        Rescan();
    }

    private void ScanPartsRecurse(Node node)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Do the main check
        if (node is IDeformable deformable)
        {
            ToDeform.Add(deformable);
        }

        // Deformers already deform their children, and we deform
        // them first, so don't exaggerate it through their children
        if (node is not Deformer)
        {
            foreach (var child in node.Children)
            {
                ScanPartsRecurse(child);
            }
        }
    }
}