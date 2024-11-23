using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes;

public abstract class Drawable : Node
{
    /// <summary>
    /// OpenGL Index Buffer Object
    /// </summary>
    protected uint ibo;

    /// <summary>
    /// OpenGL Vertex Buffer Object
    /// </summary>
    protected uint vbo;

    /// <summary>
    /// OpenGL Vertex Buffer Object for deformation
    /// </summary>
    protected uint dbo;

    /// <summary>
    /// The mesh data of this part
    /// 
    /// NOTE: DO NOT MODIFY!
    /// The data in here is only to be used for reference.
    /// </summary>
    protected MeshData data;

    /// <summary>
    /// Deformation offset to apply
    /// </summary>
    public Vector2[] deformation;

    /// <summary>
    /// The bounds of this drawable
    /// </summary>
    public Vector4 bounds;

    /// <summary>
    /// Deformation stack
    /// </summary>
    public DeformationStack deformStack;

    public List<Vector2> Vertices { get; set; }

    public abstract void renderMask(bool dodge = false);

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="parent"></param>
    public Drawable(Node? parent = null) : base(parent)
    {
        // Generate the buffers
        CoreHelper.gl.GenBuffers(1, out vbo);
        CoreHelper.gl.GenBuffers(1, out ibo);
        CoreHelper.gl.GenBuffers(1, out dbo);

        // Create deformation stack
        this.deformStack = new DeformationStack(this);
    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, Node? parent = null) : this(data, NodeHelper.InCreateUUID(), parent) {

    }

    /// <summary>
    /// Constructs a new drawable surface
    /// </summary>
    /// <param name="data"></param>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Drawable(MeshData data, uint uuid, Node? parent = null) : base(uuid, parent)
    {
        this.data = data;
        this.deformStack = new DeformationStack(this);

        // Set the deformable points to their initial position
        Vertices = [.. data.Vertices];

        // Generate the buffers
        CoreHelper.gl.GenBuffers(1, out vbo);
        CoreHelper.gl.GenBuffers(1, out ibo);
        CoreHelper.gl.GenBuffers(1, out dbo);

        // Update indices and vertices
        updateIndices();
        updateVertices();
    }

    private unsafe void updateIndices()
    {
        CoreHelper.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, ibo);
        var temp = data.Indices.ToArray();
        fixed (void* ptr = temp)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, temp.Length * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
    }


    private unsafe void updateVertices()
    {
        // Important check since the user can change this every frame
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, vbo);
        var temp = Vertices.ToArray();
        fixed (void* ptr = temp)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, temp.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        // Zero-fill the deformation delta
        deformation = new Vector2[Vertices.Count];
        for (int i = 0; i < deformation.Length; i++)
        {
            deformation[i] = new(0, 0);
        }
        updateDeform();
    }

    protected unsafe void updateDeform()
    {
        // Important check since the user can change this every frame
        if (deformation.Length != Vertices.Count)
        {
            throw new Exception($"Data length mismatch for {name}, deformation length={deformation.Length} whereas vertices.length={Vertices.Count}, if you want to change the mesh you need to change its data with Part.rebuffer.");
        }

        PostProcess();

        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, dbo);

        fixed (void* ptr = deformation)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, deformation.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        updateBounds();
    }

    /// <summary>
    /// Binds Index Buffer for rendering
    /// </summary>
    protected void bindIndex()
    {
        // Bind element array and draw our mesh
        CoreHelper.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, ibo);
        CoreHelper.gl.DrawElements(GlApi.GL_TRIANGLES, data.Indices.Count, GlApi.GL_UNSIGNED_SHORT, 0);
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelf(JObject serializer)
    {
        base.SerializeSelf(serializer);
        var obj = new JObject();
        data.Serialize(obj);
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

        data = new();
        data.Deserialize(obj1);

        Vertices = [.. data.Vertices];

        // Update indices and vertices
        updateIndices();
        updateVertices();
    }

    protected void onDeformPushed(ref Deformation deform)
    {

    }

    protected override void PreProcess()
    {
        if (preProcessed)
            return;
        preProcessed = true;
        if (preProcessFilter != null)
        {
            overrideTransformMatrix = null;
            var matrix = this.Transform().Matrix;
            var filterResult = preProcessFilter?.Invoke(Vertices, deformation, ref matrix);
            if (filterResult?.Item1 is { } item1 && item1.Length != 0)
            {
                deformation = item1;
            }
            if (filterResult?.Item2 is { } item2)
            {
                overrideTransformMatrix = new MatrixHolder(item2);
            }
        }
    }

    protected override void PostProcess()
    {
        if (postProcessed)
            return;
        postProcessed = true;
        if (postProcessFilter != null)
        {
            overrideTransformMatrix = null;
            var matrix = Transform().Matrix;
            var filterResult = postProcessFilter(Vertices, deformation, ref matrix);
            if (filterResult.Item1 is { } item1 && item1.Length != 0)
            {
                deformation = item1;
            }
            if (filterResult.Item2 is { } item2)
            {
                overrideTransformMatrix = new MatrixHolder(item2);
            }
        }
    }

    protected void notifyDeformPushed(ref Deformation deform)
    {
        onDeformPushed(ref deform);
    }

    /// <summary>
    /// Refreshes the drawable, updating its vertices
    /// </summary>
    public void refresh()
    {
        this.updateVertices();
    }

    /// <summary>
    /// Refreshes the drawable, updating its deformation deltas
    /// </summary>
    public void refreshDeform()
    {
        this.updateDeform();
    }

    public override void beginUpdate()
    {
        deformStack.PreUpdate();
        base.beginUpdate();
    }

    /// <summary>
    /// Updates the drawable
    /// </summary>
    public override void update()
    {
        PreProcess();
        deformStack.Update();
        base.update();
        updateDeform();
    }

    /// <summary>
    /// Draws the drawable
    /// </summary>
    public override void drawOne()
    {
        base.drawOne();
    }

    /**
        Draws the drawable without any processing
    */
    public virtual void drawOneDirect(bool forMasking) { }

    public override string TypeId()
    {
        return "Drawable";
    }

    /// <summary>
    /// Updates the drawable's bounds
    /// </summary>
    public void updateBounds()
    {
        if (!NodeHelper.doGenerateBounds) return;

        // Calculate bounds
        var wtransform = Transform();
        var temp = wtransform.Translation;
        bounds = new(temp.X, temp.Y, temp.X, temp.Y);
        var matrix = getDynamicMatrix();
        for (int i = 0; i < Vertices.Count; i++)
        {
            var vertex = Vertices[i];
            var temp1 = matrix.Multiply(new Vector4(vertex + deformation[i], 0, 1));
            var vertOriented = new Vector2(temp1.X, temp1.Y);
            if (vertOriented.X < bounds.X) bounds.X = vertOriented.X;
            if (vertOriented.Y < bounds.Y) bounds.Y = vertOriented.Y;
            if (vertOriented.X > bounds.Z) bounds.Z = vertOriented.X;
            if (vertOriented.Y > bounds.W) bounds.W = vertOriented.Y;
        }
    }

    /// <summary>
    /// Draws bounds
    /// </summary>
    public override void drawBounds()
    {
        if (!NodeHelper.doGenerateBounds) return;
        if (Vertices.Count == 0) return;

        float width = bounds.Z - bounds.X;
        float height = bounds.W - bounds.Y;
        CoreHelper.inDbgSetBuffer([
            new Vector3(bounds.X, bounds.Y, 0),
            new Vector3(bounds.X + width, bounds.Y, 0),

            new Vector3(bounds.X + width, bounds.Y, 0),
            new Vector3(bounds.X + width, bounds.Y+height, 0),

            new Vector3(bounds.X + width, bounds.Y+height, 0),
            new Vector3(bounds.X, bounds.Y+height, 0),

            new Vector3(bounds.X, bounds.Y+height, 0),
            new Vector3(bounds.X, bounds.Y, 0),
        ]);
        CoreHelper.inDbgLineWidth(3);
        if (oneTimeTransform is { } mat)
            CoreHelper.inDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), mat);
        else
            CoreHelper.inDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1));
        CoreHelper.inDbgLineWidth(1);
    }

    /// <summary>
    /// Draws line of mesh
    /// </summary>
    public void drawMeshLines()
    {
        if (Vertices.Count == 0) return;

        var trans = getDynamicMatrix();

        var indices = data.Indices;

        var points = new Vector3[indices.Count * 2];
        for (int i = 0; i < indices.Count / 3; i++)
        {
            var ix = i * 3;
            var iy = ix * 2;
            var indice = indices[ix];

            points[iy + 0] = new Vector3(Vertices[indice] - data.Origin + deformation[indice], 0);
            points[iy + 1] = new Vector3(Vertices[indices[ix + 1]] - data.Origin + deformation[indices[ix + 1]], 0);

            points[iy + 2] = new Vector3(Vertices[indices[ix + 1]] - data.Origin + deformation[indices[ix + 1]], 0);
            points[iy + 3] = new Vector3(Vertices[indices[ix + 2]] - data.Origin + deformation[indices[ix + 2]], 0);

            points[iy + 4] = new Vector3(Vertices[indices[ix + 2]] - data.Origin + deformation[indices[ix + 2]], 0);
            points[iy + 5] = new Vector3(Vertices[indice] - data.Origin + deformation[indice], 0);
        }

        CoreHelper.inDbgSetBuffer(points);
        CoreHelper.inDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), trans);
    }

    /// <summary>
    /// Draws the points of the mesh
    /// </summary>
    public void drawMeshPoints()
    {
        if (Vertices.Count == 0) return;

        var trans = getDynamicMatrix();
        var points = new Vector3[Vertices.Count];
        for (int i = 0; i < Vertices.Count; i++)
        {
            var point = Vertices[i];
            points[i] = new Vector3(point - data.Origin + deformation[i], 0);
        }

        CoreHelper.inDbgSetBuffer(points);
        CoreHelper.inDbgPointsSize(8);
        CoreHelper.inDbgDrawPoints(new Vector4(0, 0, 0, 1), trans);
        CoreHelper.inDbgPointsSize(4);
        CoreHelper.inDbgDrawPoints(new Vector4(1, 1, 1, 1), trans);
    }

    /// <summary>
    /// Returns the mesh data for this Part.
    /// </summary>
    /// <returns></returns>
    public MeshData getMesh()
    {
        return data;
    }

    /// <summary>
    /// Changes this mesh's data
    /// </summary>
    /// <param name="data"></param>
    public virtual void rebuffer(MeshData data)
    {
        this.data = data;
        this.updateIndices();
        this.updateVertices();
    }

    /// <summary>
    /// Resets the vertices of this drawable
    /// </summary>
    public void reset()
    {
        Vertices = [.. data.Vertices];
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
    public void inBeginMask(bool hasMasks)
    {
        // Enable and clear the stencil buffer so we can write our mask to it
        CoreHelper.gl.Enable(GlApi.GL_STENCIL_TEST);
        CoreHelper.gl.ClearStencil(hasMasks ? 0 : 1);
        CoreHelper.gl.Clear(GlApi.GL_STENCIL_BUFFER_BIT);
    }

    /// <summary>
    /// End masking
    /// 
    /// Once masking is ended content will no longer be masked by the defined mask.
    /// </summary>
    public void inEndMask()
    {
        // We're done stencil testing, disable it again so that we don't accidentally mask more stuff out
        CoreHelper.gl.StencilMask(0xFF);
        CoreHelper.gl.StencilFunc(GlApi.GL_ALWAYS, 1, 0xFF);
        CoreHelper.gl.Disable(GlApi.GL_STENCIL_TEST);
    }

    /// <summary>
    /// Starts masking content
    /// 
    /// NOTE: This have to be run within a inBeginMask and inEndMask block!
    /// </summary>
    public void inBeginMaskContent()
    {
        CoreHelper.gl.StencilFunc(GlApi.GL_EQUAL, 1, 0xFF);
        CoreHelper.gl.StencilMask(0x00);
    }
}