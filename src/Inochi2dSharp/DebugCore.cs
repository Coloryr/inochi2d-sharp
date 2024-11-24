using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp;

public partial class I2dCore
{
    public Shader DbgShaderLine;
    public Shader DbgShaderPoint;
    public uint DbgVAO;
    public uint DbgVBO;
    public uint DbgIBO;

    private readonly bool _inDbgDrawMeshOutlines = false;
    private readonly bool _inDbgDrawMeshVertexPoints = false;
    private readonly bool _inDbgDrawMeshOrientation = false;

    private void InInitDebug()
    {
        DbgShaderLine = new Shader("debug line", Integration.DebugVert, Integration.DebugLineFrag);
        DbgShaderPoint = new Shader("debug point", Integration.DebugVert, Integration.DebugPointFrag);
        gl.GenVertexArrays(1, out DbgVAO);
        gl.GenBuffers(1, out DbgVBO);
        gl.GenBuffers(1, out DbgIBO);

        mvpId = DbgShaderLine.getUniformLocation("mvp");
        colorId = DbgShaderLine.getUniformLocation("color");
    }

    private void InUpdateDbgVerts(Vector3[] points)
    {
        // Generate bad line drawing indices
        ushort[] vts = new ushort[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
        {
            vts[i] = (ushort)i;
        }
        vts[^1] = 0;

        InUpdateDbgVerts(points, vts);
    }

    private unsafe void InUpdateDbgVerts(Vector3[] points, ushort[] indices)
    {
        gl.BindVertexArray(DbgVAO);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, DbgVBO);

        fixed (void* ptr = points)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, points.Length * Marshal.SizeOf<Vector3>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        cVBO = DbgVBO;

        IndiceCount = indices.Length;
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, DbgIBO);
        fixed (void* ptr = indices)
        {
            gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(ushort), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }
    }

    /// <summary>
    /// Size of debug points
    /// </summary>
    /// <param name="size"></param>
    public void InDbgPointsSize(float size)
    {
        gl.PointSize(size);
    }

    /// <summary>
    ///  Size of debug points
    /// </summary>
    /// <param name="size"></param>
    public void InDbgLineWidth(float size)
    {
        gl.LineWidth(size);
    }

    /// <summary>
    /// Draws points with specified color
    /// </summary>
    /// <param name="points"></param>
    public void InDbgSetBuffer(Vector3[] points)
    {
        InUpdateDbgVerts(points);
    }

    /// <summary>
    ///  Sets buffer to buffer owned by an other OpenGL object
    /// </summary>
    /// <param name="vbo"></param>
    /// <param name="ibo"></param>
    /// <param name="count"></param>
    public void InDbgSetBuffer(uint vbo, uint ibo, int count)
    {
        gl.BindVertexArray(DbgVAO);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, vbo);
        cVBO = vbo;
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, ibo);
        IndiceCount = count;
    }

    /// <summary>
    /// Draws points with specified color
    /// </summary>
    /// <param name="points"></param>
    /// <param name="indices"></param>
    public void InDbgSetBuffer(Vector3[] points, ushort[] indices)
    {
        InUpdateDbgVerts(points, indices);
    }

    public void InDbgDrawPoints(Vector4 color)
    {
        InDbgDrawPoints(color, Matrix4x4.Identity);
    }

    /// <summary>
    /// Draws current stored vertices as points with specified color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="transform"></param>
    public void InDbgDrawPoints(Vector4 color, Matrix4x4 transform)
    {
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFuncSeparate(GlApi.GL_SRC_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);

        gl.BindVertexArray(DbgVAO);

        DbgShaderPoint.use();
        DbgShaderPoint.setUniform(mvpId, InCamera.Matrix() * transform);
        DbgShaderPoint.setUniform(colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_POINTS, IndiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
        gl.DisableVertexAttribArray(0);

        gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);
    }

    public void InDbgDrawLines(Vector4 color)
    {
        InDbgDrawLines(color, Matrix4x4.Identity);
    }

    /// <summary>
    /// Draws current stored vertices as lines with specified color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="transform"></param>
    public void InDbgDrawLines(Vector4 color, Matrix4x4 transform)
    {
        gl.Enable(GlApi.GL_LINE_SMOOTH);
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFuncSeparate(GlApi.GL_SRC_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);

        gl.BindVertexArray(DbgVAO);

        DbgShaderLine.use();
        DbgShaderLine.setUniform(mvpId, InCamera.Matrix() * transform);
        DbgShaderLine.setUniform(colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_LINES, IndiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
        gl.DisableVertexAttribArray(0);

        gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);
        gl.Disable(GlApi.GL_LINE_SMOOTH);
    }
}
