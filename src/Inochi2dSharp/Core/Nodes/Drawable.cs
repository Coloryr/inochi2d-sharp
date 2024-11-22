using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        data.Vertices = [.. data.Vertices];

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
        var temp = data.Vertices.ToArray();
        fixed (void* ptr = temp)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, temp.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        // Zero-fill the deformation delta
        deformation = new Vector2[data.Vertices.Count];
        for (int i = 0; i < deformation.Length; i++)
        {
            deformation[i] = new(0, 0);
        }
        updateDeform();
    }

    protected unsafe void updateDeform()
    {
        // Important check since the user can change this every frame
        if (deformation.Length != data.Vertices.Count)
        {
            throw new Exception($"Data length mismatch for {name}, deformation length={deformation.Length} whereas vertices.length={data.Vertices.Count}, if you want to change the mesh you need to change its data with Part.rebuffer.");
        }

        postProcess();

        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, dbo);

        fixed (void* ptr = deformation)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, deformation.Length * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        this.updateBounds();
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
    protected override void SerializeSelf(JObject serializer, bool recursive = true)
    {
        base.SerializeSelf(serializer, recursive);
        var obj = new JObject();
        data.Serialize(obj);
        serializer.Add("mesh", obj);
    }

    protected override void Deserialize(JObject obj)
    {
        base.Deserialize(obj);
        var temp = obj["mesh"];
        if (temp is not JObject obj1)
        {
            return;
        }

        data = new();
        data.Deserialize(obj1);

        data.Vertices = [.. data.Vertices];

        // Update indices and vertices
        updateIndices();
        updateVertices();
    }

    protected void onDeformPushed(ref Deformation deform)
    {

    }

    protected override void preProcess()
    {
        if (preProcessed)
            return;
        preProcessed = true;
        if (preProcessFilter! is null)
        {
            overrideTransformMatrix = null;
            mat4 matrix = this.transform.matrix;
            auto filterResult = preProcessFilter(vertices, deformation, &matrix);
            if (filterResult[0]! is null)
            {
                deformation = filterResult[0];
            }
            if (filterResult[1]! is null)
            {
                overrideTransformMatrix = new MatrixHolder(*filterResult[1]);
            }
        }
    }

    protected override void postProcess()
    {
        if (postProcessed)
            return;
        postProcessed = true;
        if (postProcessFilter! is null)
        {
            overrideTransformMatrix = null;
            mat4 matrix = this.transform.matrix;
            auto filterResult = postProcessFilter(vertices, deformation, &matrix);
            if (filterResult[0]! is null)
            {
                deformation = filterResult[0];
            }
            if (filterResult[1]! is null)
            {
                overrideTransformMatrix = new MatrixHolder(*filterResult[1]);
            }
        }
    }

    protected void notifyDeformPushed(ref Deformation deform)
    {
        onDeformPushed(deform);
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
        deformStack.preUpdate();
        base.beginUpdate();
    }

    /// <summary>
    /// Updates the drawable
    /// </summary>
    public override void update()
    {
        preProcess();
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
    public void drawOneDirect(bool forMasking) { }

    public override string typeId()
    {
        return "Drawable";
    }

    /// <summary>
    /// Updates the drawable's bounds
    /// </summary>
    public void updateBounds()
    {
        if (!doGenerateBounds) return;

        // Calculate bounds
        Transform wtransform = transform;
        bounds = vec4(wtransform.translation.xyxy);
        mat4 matrix = getDynamicMatrix();
        foreach (i, vertex; vertices) {
            vec2 vertOriented = vec2(matrix * vec4(vertex + deformation[i], 0, 1));
            if (vertOriented.x < bounds.x) bounds.x = vertOriented.x;
            if (vertOriented.y < bounds.y) bounds.y = vertOriented.y;
            if (vertOriented.x > bounds.z) bounds.z = vertOriented.x;
            if (vertOriented.y > bounds.w) bounds.w = vertOriented.y;
        }
    }

    /**
        Draws bounds
    */
    override
    void drawBounds()
    {
        if (!doGenerateBounds) return;
        if (vertices.length == 0) return;

        float width = bounds.z - bounds.x;
        float height = bounds.w - bounds.y;
        inDbgSetBuffer([
            vec3(bounds.x, bounds.y, 0),
            vec3(bounds.x + width, bounds.y, 0),

            vec3(bounds.x + width, bounds.y, 0),
            vec3(bounds.x + width, bounds.y+height, 0),

            vec3(bounds.x + width, bounds.y+height, 0),
            vec3(bounds.x, bounds.y+height, 0),

            vec3(bounds.x, bounds.y+height, 0),
            vec3(bounds.x, bounds.y, 0),
        ]);
        inDbgLineWidth(3);
        if (oneTimeTransform! is null)
            inDbgDrawLines(vec4(.5, .5, .5, 1), (*oneTimeTransform));
        else
            inDbgDrawLines(vec4(.5, .5, .5, 1));
        inDbgLineWidth(1);
    }

    version(InDoesRender)
    {
        /**
            Draws line of mesh
        */
        void drawMeshLines()
        {
            if (vertices.length == 0) return;

            auto trans = getDynamicMatrix();

            ushort[] indices = data.indices;

            vec3[] points = new vec3[indices.length * 2];
            foreach (i; 0..indices.length / 3) {
                size_t ix = i * 3;
                size_t iy = ix * 2;
                auto indice = indices[ix];

                points[iy + 0] = vec3(vertices[indice] - data.origin + deformation[indice], 0);
                points[iy + 1] = vec3(vertices[indices[ix + 1]] - data.origin + deformation[indices[ix + 1]], 0);

                points[iy + 2] = vec3(vertices[indices[ix + 1]] - data.origin + deformation[indices[ix + 1]], 0);
                points[iy + 3] = vec3(vertices[indices[ix + 2]] - data.origin + deformation[indices[ix + 2]], 0);

                points[iy + 4] = vec3(vertices[indices[ix + 2]] - data.origin + deformation[indices[ix + 2]], 0);
                points[iy + 5] = vec3(vertices[indice] - data.origin + deformation[indice], 0);
            }

            inDbgSetBuffer(points);
            inDbgDrawLines(vec4(.5, .5, .5, 1), trans);
        }

        /**
            Draws the points of the mesh
        */
        void drawMeshPoints()
        {
            if (vertices.length == 0) return;

            auto trans = getDynamicMatrix();
            vec3[] points = new vec3[vertices.length];
            foreach (i, point; vertices) {
                points[i] = vec3(point - data.origin + deformation[i], 0);
            }

            inDbgSetBuffer(points);
            inDbgPointsSize(8);
            inDbgDrawPoints(vec4(0, 0, 0, 1), trans);
            inDbgPointsSize(4);
            inDbgDrawPoints(vec4(1, 1, 1, 1), trans);
        }
    }

    /// <summary>
    /// Returns the mesh data for this Part.
    /// </summary>
    /// <returns></returns>
    public MeshData getMesh()
    {
        return this.data;
    }

    /**
        Changes this mesh's data
    */
    public virtual void rebuffer(MeshData data)
    {
        this.data = data;
        this.updateIndices();
        this.updateVertices();
    }

    /**
        Resets the vertices of this drawable
    */
    final void reset()
    {
        vertices[] = data.vertices;
    }
}

version(InDoesRender)
    {
        /**
            Begins a mask

            This causes the next draw calls until inBeginMaskContent/inBeginDodgeContent or inEndMask 
            to be written to the current mask.

            This also clears whatever old mask there was.
        */
        void inBeginMask(bool hasMasks)
        {

            // Enable and clear the stencil buffer so we can write our mask to it
            glEnable(GL_STENCIL_TEST);
            glClearStencil(hasMasks ? 0 : 1);
            glClear(GL_STENCIL_BUFFER_BIT);
        }

        /**
            End masking

            Once masking is ended content will no longer be masked by the defined mask.
        */
        void inEndMask()
        {

            // We're done stencil testing, disable it again so that we don't accidentally mask more stuff out
            glStencilMask(0xFF);
            glStencilFunc(GL_ALWAYS, 1, 0xFF);
            glDisable(GL_STENCIL_TEST);
        }

        /**
            Starts masking content

            NOTE: This have to be run within a inBeginMask and inEndMask block!
        */
        void inBeginMaskContent()
        {

            glStencilFunc(GL_EQUAL, 1, 0xFF);
            glStencilMask(0x00);
        }
    }
}