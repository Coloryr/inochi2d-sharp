using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes;

public abstract class Drawable : Node
{
    /// <summary>
    /// OpenGL Index Buffer Object
    /// </summary>
    private readonly uint _ibo;

    /// <summary>
    /// OpenGL Vertex Buffer Object
    /// </summary>
    protected uint Vbo;

    /// <summary>
    /// OpenGL Vertex Buffer Object for deformation
    /// </summary>
    protected uint Dbo;

    /// <summary>
    /// The mesh data of this part
    /// 
    /// NOTE: DO NOT MODIFY!
    /// The data in here is only to be used for reference.
    /// </summary>
    protected MeshData Data;

    /// <summary>
    /// Deformation offset to apply
    /// </summary>
    public Vector2[] Deformation;

    /// <summary>
    /// The bounds of this drawable
    /// </summary>
    public Vector4 Bounds;

    /// <summary>
    /// Deformation stack
    /// </summary>
    public DeformationStack DeformStack;

    public List<Vector2> Vertices;

    public abstract void RenderMask(bool dodge = false);

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="parent"></param>
    public Drawable(I2dCore core, Node? parent = null) : base(core, parent)
    {
        // Generate the buffers
        Vbo = core.gl.GenBuffer();
        _ibo = core.gl.GenBuffer();
        Dbo = core.gl.GenBuffer();

        // Create deformation stack
        DeformStack = new DeformationStack(this);
    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="parent"></param>
    public Drawable(I2dCore core, MeshData data, Node? parent = null) : this(core, data, core.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Drawable(I2dCore core, MeshData data, uint uuid, Node? parent = null) : base(core, uuid, parent)
    {
        Data = data;
        DeformStack = new DeformationStack(this);

        // Set the deformable points to their initial position
        Vertices = [.. data.Vertices];

        // Generate the buffers
        Vbo = core.gl.GenBuffer();
        _ibo = core.gl.GenBuffer();
        Dbo = core.gl.GenBuffer();

        // Update indices and vertices
        UpdateIndices();
        UpdateVertices();
    }

    private unsafe void UpdateIndices()
    {
        _core.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, _ibo);
        var temp = Data.Indices.ToArray();
        fixed (void* ptr = temp)
        {
            _core.gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, temp.Length * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
    }

    private unsafe void UpdateVertices()
    {
        // Important check since the user can change this every frame
        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Vbo);
        var temp = Vertices.ToArray();
        fixed (void* ptr = temp)
        {
            _core.gl.BufferData(GlApi.GL_ARRAY_BUFFER, temp.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        // Zero-fill the deformation delta
        Deformation = new Vector2[Vertices.Count];
        for (int i = 0; i < Deformation.Length; i++)
        {
            Deformation[i] = new(0, 0);
        }
        UpdateDeform();
    }

    protected unsafe void UpdateDeform()
    {
        // Important check since the user can change this every frame
        if (Deformation.Length != Vertices.Count)
        {
            throw new Exception($"Data length mismatch for {Name}, deformation length={Deformation.Length} whereas vertices.length={Vertices.Count}, if you want to change the mesh you need to change its data with Part.rebuffer.");
        }

        PostProcess();

        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Dbo);

        fixed (void* ptr = Deformation)
        {
            _core.gl.BufferData(GlApi.GL_ARRAY_BUFFER, Deformation.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        UpdateBounds();
    }

    /// <summary>
    /// Binds Index Buffer for rendering
    /// </summary>
    protected void BindIndex()
    {
        // Bind element array and draw our mesh
        _core.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, _ibo);
        _core.gl.DrawElements(GlApi.GL_TRIANGLES, Data.Indices.Count, GlApi.GL_UNSIGNED_SHORT, 0);
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelfImpl(JsonObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);
        var obj = new JsonObject();
        Data.Serialize(obj);
        serializer.Add("mesh", obj);
    }

    public override void Deserialize(JsonElement obj)
    {
        base.Deserialize(obj);
        if (!obj.TryGetProperty("mesh", out var temp) || temp.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        Data = new();
        Data.Deserialize(temp);

        Vertices = [.. Data.Vertices];

        // Update indices and vertices
        UpdateIndices();
        UpdateVertices();
    }

    protected void OnDeformPushed(Deformation deform)
    {

    }

    public override void PreProcess()
    {
        if (PreProcessed)
            return;
        PreProcessed = true;
        if (PreProcessFilter != null)
        {
            OverrideTransformMatrix = null;
            var matrix = this.Transform().Matrix;
            var filterResult = PreProcessFilter?.Invoke(Vertices, Deformation, ref matrix);
            if (filterResult?.Item1 is { } item1 && item1.Length != 0)
            {
                Deformation = item1;
            }
            if (filterResult?.Item2 is { } item2)
            {
                OverrideTransformMatrix = new MatrixHolder(item2);
            }
        }
    }

    public override void PostProcess()
    {
        if (PostProcessed)
            return;
        PostProcessed = true;
        if (PostProcessFilter != null)
        {
            OverrideTransformMatrix = null;
            var matrix = Transform().Matrix;
            var filterResult = PostProcessFilter(Vertices, Deformation, ref matrix);
            if (filterResult.Item1 is { } item1 && item1.Length != 0)
            {
                Deformation = item1;
            }
            if (filterResult.Item2 is { } item2)
            {
                OverrideTransformMatrix = new MatrixHolder(item2);
            }
        }
    }

    public void NotifyDeformPushed(Deformation deform)
    {
        OnDeformPushed(deform);
    }

    /// <summary>
    /// Refreshes the drawable, updating its vertices
    /// </summary>
    public void Refresh()
    {
        UpdateVertices();
    }

    /// <summary>
    /// Refreshes the drawable, updating its deformation deltas
    /// </summary>
    public void RefreshDeform()
    {
        UpdateDeform();
    }

    public override void BeginUpdate()
    {
        DeformStack.PreUpdate();
        base.BeginUpdate();
    }

    /// <summary>
    /// Updates the drawable
    /// </summary>
    public override void Update()
    {
        PreProcess();
        DeformStack.Update();
        base.Update();
        UpdateDeform();
    }

    /// <summary>
    /// Draws the drawable
    /// </summary>
    public override void DrawOne()
    {
        base.DrawOne();
    }

    /// <summary>
    /// Draws the drawable without any processing
    /// </summary>
    /// <param name="forMasking"></param>
    public virtual void DrawOneDirect(bool forMasking) { }

    public override string TypeId()
    {
        return "Drawable";
    }

    /// <summary>
    /// Updates the drawable's bounds
    /// </summary>
    public void UpdateBounds()
    {
        if (!_core.DoGenerateBounds) return;

        // Calculate bounds
        var wtransform = Transform();
        var temp = wtransform.Translation;
        Bounds = new(temp.X, temp.Y, temp.X, temp.Y);
        var matrix = GetDynamicMatrix();
        for (int i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            var temp1 = matrix.Multiply(new Vector4(vertex + Deformation[i], 0, 1));
            var vertOriented = new Vector2(temp1.X, temp1.Y);
            if (vertOriented.X < Bounds.X) Bounds.X = vertOriented.X;
            if (vertOriented.Y < Bounds.Y) Bounds.Y = vertOriented.Y;
            if (vertOriented.X > Bounds.Z) Bounds.Z = vertOriented.X;
            if (vertOriented.Y > Bounds.W) Bounds.W = vertOriented.Y;
        }
    }

    /// <summary>
    /// Draws bounds
    /// </summary>
    public override void DrawBounds()
    {
        if (!_core.DoGenerateBounds) return;
        if (Vertices.Count == 0) return;

        float width = Bounds.Z - Bounds.X;
        float height = Bounds.W - Bounds.Y;
        _core.InDbgSetBuffer([
            new Vector3(Bounds.X, Bounds.Y, 0),
            new Vector3(Bounds.X + width, Bounds.Y, 0),

            new Vector3(Bounds.X + width, Bounds.Y, 0),
            new Vector3(Bounds.X + width, Bounds.Y+height, 0),

            new Vector3(Bounds.X + width, Bounds.Y+height, 0),
            new Vector3(Bounds.X, Bounds.Y+height, 0),

            new Vector3(Bounds.X, Bounds.Y+height, 0),
            new Vector3(Bounds.X, Bounds.Y, 0),
        ]);
        _core.InDbgLineWidth(3);
        if (OneTimeTransform is { } mat)
            _core.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), mat);
        else
            _core.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1));
        _core.InDbgLineWidth(1);
    }

    /// <summary>
    /// Draws line of mesh
    /// </summary>
    public void DrawMeshLines()
    {
        if (Vertices.Count == 0) return;

        var trans = GetDynamicMatrix();

        var indices = Data.Indices;

        var points = new Vector3[indices.Count * 2];
        for (int i = 0; i < indices.Count / 3; i++)
        {
            var ix = i * 3;
            var iy = ix * 2;
            var indice = indices[ix];

            points[iy + 0] = new Vector3(Vertices[indice] - Data.Origin + Deformation[indice], 0);
            points[iy + 1] = new Vector3(Vertices[indices[ix + 1]] - Data.Origin + Deformation[indices[ix + 1]], 0);

            points[iy + 2] = new Vector3(Vertices[indices[ix + 1]] - Data.Origin + Deformation[indices[ix + 1]], 0);
            points[iy + 3] = new Vector3(Vertices[indices[ix + 2]] - Data.Origin + Deformation[indices[ix + 2]], 0);

            points[iy + 4] = new Vector3(Vertices[indices[ix + 2]] - Data.Origin + Deformation[indices[ix + 2]], 0);
            points[iy + 5] = new Vector3(Vertices[indice] - Data.Origin + Deformation[indice], 0);
        }

        _core.InDbgSetBuffer(points);
        _core.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), trans);
    }

    /// <summary>
    /// Draws the points of the mesh
    /// </summary>
    public void DrawMeshPoints()
    {
        if (Vertices.Count == 0) return;

        var trans = GetDynamicMatrix();
        var points = new Vector3[Vertices.Count];
        for (int i = 0; i < Vertices.Count; i++)
        {
            var point = Vertices[i];
            points[i] = new Vector3(point - Data.Origin + Deformation[i], 0);
        }

        _core.InDbgSetBuffer(points);
        _core.InDbgPointsSize(8);
        _core.InDbgDrawPoints(new Vector4(0, 0, 0, 1), trans);
        _core.InDbgPointsSize(4);
        _core.InDbgDrawPoints(new Vector4(1, 1, 1, 1), trans);
    }

    /// <summary>
    /// Returns the mesh data for this Part.
    /// </summary>
    /// <returns></returns>
    public MeshData GetMesh()
    {
        return Data;
    }

    /// <summary>
    /// Changes this mesh's data
    /// </summary>
    /// <param name="data"></param>
    public virtual void Rebuffer(MeshData data)
    {
        Data = data;
        UpdateIndices();
        UpdateVertices();
    }

    /// <summary>
    /// Resets the vertices of this drawable
    /// </summary>
    public void Reset()
    {
        Vertices = [.. Data.Vertices];
    }

    protected void UpdateNode()
    {
        base.Update();
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_ibo > 0)
        {
            _core.gl.DeleteBuffer(_ibo);
        }
        if (Vbo > 0)
        {
            _core.gl.DeleteBuffer(Vbo);
        }
        if (Dbo > 0)
        {
            _core.gl.DeleteBuffer(Dbo);
        }
    }
}