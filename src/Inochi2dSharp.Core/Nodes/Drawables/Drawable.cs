using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Drawables;

/// <summary>
/// Nodes that are meant to render something in to the Inochi2D scene 
/// Other nodes don't have to render anything and serve mostly other 
/// purposes.<br/>
/// The main types of Drawables are Parts and Masks
/// </summary>
[TypeId("Drawable", 0x0001, true)]
public abstract class Drawable : Node, IDeformable
{
    private Mesh _mesh;
    private DeformedMesh _deformed;
    private DeformedMesh _base;

    /// <summary>
    /// The current active draw list slot for this drawable.
    /// </summary>
    protected DrawListAlloc? drawListSlot;

    /// <summary>
    /// The mesh of the drawable.
    /// </summary>
    public Mesh Mesh
    {
        get
        {
            return _mesh;
        }
        set
        {
            if (value == _mesh)
                return;
            _mesh = value;
            _deformed.Parent = value;
            _base.Parent = value;
        }
    }

    /// <summary>
    /// Local matrix of the deformable object.
    /// </summary>
    public Transform BaseTransform => Transform(true);
    /// <summary>
    /// World matrix of the deformable object.
    /// </summary>
    public Transform WorldTransform => Transform(false);
    /// <summary>
    /// The base position of the deformable's points.
    /// </summary>
    public Vector2[] BasePoints => _base.Points;
    /// <summary>
    /// The points which may be deformed by the deformer.
    /// </summary>
    public Vector2[] DeformPoints => _deformed.Points;

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="parent"></param>
    public Drawable(Node? parent = null) : base(parent)
    {

    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, Node? parent = null) : this(data, Guid.NewGuid(), parent)
    {

    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="guid"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, Guid guid, Node? parent = null) : base(guid, parent)
    {
        _deformed = new DeformedMesh();
        _base = new DeformedMesh();
        Mesh = Mesh.FromMeshData(data);
    }

    /// <summary>
    /// Deforms the IDeformable.
    /// </summary>
    /// <param name="deformed">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public void Deform(Vector2[] deformed, bool absolute = false)
    {
        _deformed.Deform(deformed);
    }

    /// <summary>
    /// Deforms a single vertex in the IDeformable
    /// </summary>
    /// <param name="offset">The offset into the point list to deform.</param>
    /// <param name="deform">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public void Deform(int offset, Vector2 deform, bool absolute = false)
    {
        _deformed.Deform(offset, deform);
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
    public void ResetDeform()
    {
        _deformed.Reset();

        _base.Reset();
        _base.PushMatrix(BaseTransform.Matrix);
    }

    /// <summary>
    /// The mesh of the model.
    /// </summary>
    /// <param name="drawList"></param>
    public override void PreUpdate(DrawList drawList)
    {
        base.PreUpdate(drawList);
        ResetDeform();
    }

    /// <summary>
    /// Updates the drawable
    /// </summary>
    /// <param name="delta"></param>
    /// <param name="drawList"></param>
    public override void Update(float delta, DrawList drawList)
    {
        base.Update(delta, drawList);
        _deformed.PushMatrix(Transform().Matrix);
    }

    /// <summary>
    /// Post-update
    /// </summary>
    /// <param name="drawList"></param>
    public override void PostUpdate(DrawList drawList)
    {
        base.PostUpdate(drawList);
        drawListSlot = drawList.Allocate(_deformed.Vertices, _deformed.Indices);
    }

    /// <summary>
    /// Draws the drawable to the screen.
    /// </summary>
    /// <param name="delta"></param>
    /// <param name="drawList"></param>
    public override void Draw(float delta, DrawList drawList)
    {
        drawList.SetMesh(drawListSlot);
    }

    /// <summary>
    /// Draws the drawable to the screen in masking mode.
    /// </summary>
    /// <param name="delta"></param>
    /// <param name="drawList"></param>
    /// <param name="mode"></param>
    public virtual void DrawAsMask(float delta, DrawList drawList, MaskingMode mode)
    {
        drawList.SetMesh(drawListSlot);
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="recursive"></param>
    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);

        MeshData data = _mesh.ToMeshData();
        var obj1 = new JsonObject();
        data.Serialize(obj1);
        obj["mesh"] = obj1;
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);

        _deformed = new DeformedMesh();
        _base = new DeformedMesh();

        if (data.TryGetProperty("mesh", out var item) && item.ValueKind != JsonValueKind.Null)
        {
            var data1 = new MeshData();
            data1.Deserialize(item);
            Mesh = Mesh.FromMeshData(data1);
        }
    }
}
