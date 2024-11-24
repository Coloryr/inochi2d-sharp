using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes;

public abstract class Drawable : Node
{
    /// <summary>
    /// OpenGL Index Buffer Object
    /// </summary>
    protected uint Ibo;

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

    public List<Vector2> Vertices { get; set; }

    public abstract void RenderMask(bool dodge = false);

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="parent"></param>
    public Drawable(Node? parent = null) : base(parent)
    {
        // Generate the buffers
        I2dCore.gl.GenBuffers(1, out Vbo);
        I2dCore.gl.GenBuffers(1, out Ibo);
        I2dCore.gl.GenBuffers(1, out Dbo);

        // Create deformation stack
        DeformStack = new DeformationStack(this);
    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, Node? parent = null) : this(data, NodeHelper.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, uint uuid, Node? parent = null) : base(uuid, parent)
    {
        Data = data;
        DeformStack = new DeformationStack(this);

        // Set the deformable points to their initial position
        Vertices = [.. data.Vertices];

        // Generate the buffers
        I2dCore.gl.GenBuffers(1, out Vbo);
        I2dCore.gl.GenBuffers(1, out Ibo);
        I2dCore.gl.GenBuffers(1, out Dbo);

        // Update indices and vertices
        UpdateIndices();
        UpdateVertices();
    }

    private unsafe void UpdateIndices()
    {
        I2dCore.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, Ibo);
        var temp = Data.Indices.ToArray();
        fixed (void* ptr = temp)
        {
            I2dCore.gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, temp.Length * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
    }

    private unsafe void UpdateVertices()
    {
        // Important check since the user can change this every frame
        I2dCore.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Vbo);
        var temp = Vertices.ToArray();
        fixed (void* ptr = temp)
        {
            I2dCore.gl.BufferData(GlApi.GL_ARRAY_BUFFER, temp.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
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
            throw new Exception($"Data length mismatch for {name}, deformation length={Deformation.Length} whereas vertices.length={Vertices.Count}, if you want to change the mesh you need to change its data with Part.rebuffer.");
        }

        PostProcess();

        I2dCore.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Dbo);

        fixed (void* ptr = Deformation)
        {
            I2dCore.gl.BufferData(GlApi.GL_ARRAY_BUFFER, Deformation.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        UpdateBounds();
    }

    /// <summary>
    /// Binds Index Buffer for rendering
    /// </summary>
    protected void BindIndex()
    {
        // Bind element array and draw our mesh
        I2dCore.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, Ibo);
        I2dCore.gl.DrawElements(GlApi.GL_TRIANGLES, Data.Indices.Count, GlApi.GL_UNSIGNED_SHORT, 0);
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelfImpl(JObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);
        var obj = new JObject();
        Data.Serialize(obj);
        serializer.Add("mesh", obj);
    }

    public override void Deserialize(JObject obj)
    {
        base.Deserialize(obj);
        var temp = obj["mesh"];
        if (temp is not JObject obj1)
        {
            return;
        }

        Data = new();
        Data.Deserialize(obj1);

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
        if (preProcessed)
            return;
        preProcessed = true;
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
        if (postProcessed)
            return;
        postProcessed = true;
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
        if (!NodeHelper.doGenerateBounds) return;

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
        if (!NodeHelper.doGenerateBounds) return;
        if (Vertices.Count == 0) return;

        float width = Bounds.Z - Bounds.X;
        float height = Bounds.W - Bounds.Y;
        I2dCore.InDbgSetBuffer([
            new Vector3(Bounds.X, Bounds.Y, 0),
            new Vector3(Bounds.X + width, Bounds.Y, 0),

            new Vector3(Bounds.X + width, Bounds.Y, 0),
            new Vector3(Bounds.X + width, Bounds.Y+height, 0),

            new Vector3(Bounds.X + width, Bounds.Y+height, 0),
            new Vector3(Bounds.X, Bounds.Y+height, 0),

            new Vector3(Bounds.X, Bounds.Y+height, 0),
            new Vector3(Bounds.X, Bounds.Y, 0),
        ]);
        I2dCore.InDbgLineWidth(3);
        if (OneTimeTransform is { } mat)
            I2dCore.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), mat);
        else
            I2dCore.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1));
        I2dCore.InDbgLineWidth(1);
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

        I2dCore.InDbgSetBuffer(points);
        I2dCore.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), trans);
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

        I2dCore.InDbgSetBuffer(points);
        I2dCore.InDbgPointsSize(8);
        I2dCore.InDbgDrawPoints(new Vector4(0, 0, 0, 1), trans);
        I2dCore.InDbgPointsSize(4);
        I2dCore.InDbgDrawPoints(new Vector4(1, 1, 1, 1), trans);
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
        this.Data = data;
        this.UpdateIndices();
        this.UpdateVertices();
    }

    /// <summary>
    /// Resets the vertices of this drawable
    /// </summary>
    public void Reset()
    {
        Vertices = [.. Data.Vertices];
    }

    /// <summary>
    /// Begins a mask
    /// 
    /// This causes the next draw calls until inBeginMaskContent/inBeginDodgeContent or inEndMask
    /// to be written to the current mask.
    /// 
    /// This also clears whatever old mask there was.
    /// </summary>
    /// <param name="hasMasks"></param>
    public void InBeginMask(bool hasMasks)
    {
        // Enable and clear the stencil buffer so we can write our mask to it
        I2dCore.gl.Enable(GlApi.GL_STENCIL_TEST);
        I2dCore.gl.ClearStencil(hasMasks ? 0 : 1);
        I2dCore.gl.Clear(GlApi.GL_STENCIL_BUFFER_BIT);
    }

    /// <summary>
    /// End masking
    /// 
    /// Once masking is ended content will no longer be masked by the defined mask.
    /// </summary>
    public void InEndMask()
    {
        // We're done stencil testing, disable it again so that we don't accidentally mask more stuff out
        I2dCore.gl.StencilMask(0xFF);
        I2dCore.gl.StencilFunc(GlApi.GL_ALWAYS, 1, 0xFF);
        I2dCore.gl.Disable(GlApi.GL_STENCIL_TEST);
    }

    /// <summary>
    /// Starts masking content
    /// 
    /// NOTE: This have to be run within a inBeginMask and inEndMask block!
    /// </summary>
    public void InBeginMaskContent()
    {
        I2dCore.gl.StencilFunc(GlApi.GL_EQUAL, 1, 0xFF);
        I2dCore.gl.StencilMask(0x00);
    }
}