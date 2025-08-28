using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Core;
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
        DbgShaderLine = new Shader(this, "debug line", ShaderCode.DebugVert, ShaderCode.DbgFrag);
        DbgShaderPoint = new Shader(this, "debug point", ShaderCode.DebugVert, ShaderCode.DbgFragPoint);
        DbgVAO = gl.GenVertexArray();
        DbgVBO = gl.GenBuffer();
        DbgIBO = gl.GenBuffer();

        _mvpId = DbgShaderLine.GetUniformLocation("mvp");
        _colorId = DbgShaderLine.GetUniformLocation("color");
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

        _cVBO = DbgVBO;

        _indiceCount = indices.Length;
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
        _cVBO = vbo;
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, ibo);
        _indiceCount = count;
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

        DbgShaderPoint.Use();
        DbgShaderPoint.SetUniform(_mvpId, InCamera.Matrix() * transform);
        DbgShaderPoint.SetUniform(_colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_POINTS, _indiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
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

        DbgShaderLine.Use();
        DbgShaderLine.SetUniform(_mvpId, InCamera.Matrix() * transform);
        DbgShaderLine.SetUniform(_colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_LINES, _indiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
        gl.DisableVertexAttribArray(0);

        gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);
        gl.Disable(GlApi.GL_LINE_SMOOTH);
    }
}
