using System.Numerics;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes.Drawables;
using Inochi2dSharp.Core.Render;
using Inochi2dSharp.View;

namespace Inochi2dSharp.OpenGL;

public class Inochi2dGL : IRender
{
    private readonly GlApi _gl;

    private readonly GLFramebuffer _maskFB;
    private readonly GLFramebuffer _mainFB;
    private readonly GLShader _maskShader;
    private readonly GLShader _mainShader;

    private readonly int _maskModelViewMatrix;
    private readonly int _maskMode;
    private readonly int _mainModelViewMatrix;
    private readonly int _mainOpacity;
    private readonly List<GLFramebuffer> _compFBs = [];

    private readonly uint _vao;
    private readonly uint[] _buffers;
    private readonly uint _ubo;

    private int _sceneWidth, _sceneHeight;
    private GLFramebuffer _activeFB;

    public Inochi2dGL(GlApi gl, int width, int height)
    {
        _gl = gl;
        _sceneWidth = width;
        _sceneHeight = height;
        _maskFB = new GLFramebuffer(gl, _sceneWidth, _sceneHeight);
        _maskFB.Attach(new GLTexture(gl, GlApi.GL_RED, _sceneWidth, _sceneHeight));

        _maskShader = new GLShader(gl, GLShaderCode.MaskVert, GLShaderCode.MaskFrag);
        _maskModelViewMatrix = _maskShader.GetUniformLocation("modelViewMatrix");
        _maskMode = _maskShader.GetUniformLocation("maskMode");

        _mainFB = new GLFramebuffer(gl, _sceneWidth, _sceneHeight);
        for (int i = 0; i < 4; i++)
        {
            _mainFB.Attach(new GLTexture(gl, GlApi.GL_RGBA, _sceneWidth, _sceneHeight));
        }

        _mainShader = new GLShader(gl, GLShaderCode.BasicVert, GLShaderCode.BasicFrag);
        _mainModelViewMatrix = _mainShader.GetUniformLocation("modelViewMatrix");
        _mainOpacity = _mainShader.GetUniformLocation("opacity");

        _vao = gl.GenVertexArray();
        gl.BindVertexArray(_vao);

        _buffers = [gl.GenBuffer(), gl.GenBuffer()];
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _buffers[0]);

        gl.EnableVertexAttribArray(0);
        gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, VtxDataHelper.Size, 0);
        gl.EnableVertexAttribArray(1);
        gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, VtxDataHelper.Size, 8);

        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, _buffers[1]);

        _ubo = gl.GenBuffer();
        gl.BindBuffer(GlApi.GL_UNIFORM_BUFFER, _ubo);
        gl.BufferData(GlApi.GL_UNIFORM_BUFFER, 64, 0, GlApi.GL_DYNAMIC_DRAW);

        gl.Disable(GlApi.GL_CULL_FACE);
        gl.Disable(GlApi.GL_DEPTH_TEST);
        gl.Disable(GlApi.GL_STENCIL_TEST);
        gl.Enable(GlApi.GL_BLEND);
    }

    public void SetSize(int width, int height)
    {
        _sceneWidth = width;
        _sceneHeight = height;

        _mainFB.Resize(_sceneWidth, _sceneHeight);
        _maskFB.Resize(_sceneWidth, _sceneHeight);
        foreach (var comp in _compFBs)
        {
            comp.Resize(_sceneWidth, _sceneHeight);
        }
    }

    public void PreRender()
    {
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);
        _gl.Viewport(0, 0, _sceneWidth, _sceneHeight);

        _activeFB = _mainFB;
    }

    public unsafe void Render(Puppet puppet, Camera2D cam)
    {
        _gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _buffers[0]);
        fixed (void* ptr = puppet.DrawList.Vertices)
            _gl.BufferData(GlApi.GL_ARRAY_BUFFER, puppet.DrawList.Vertices.Length * VtxDataHelper.Size, new IntPtr(ptr), GlApi.GL_DYNAMIC_DRAW);
        _gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, _buffers[1]);
        fixed (void* ptr = puppet.DrawList.Indices)
            _gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, puppet.DrawList.Indices.Length * sizeof(uint), new IntPtr(ptr), GlApi.GL_DYNAMIC_DRAW);

        // Clear Mask
        _maskFB.Use();
        _maskShader.Use();
        _gl.ClearColor(1, 1, 1, 1);
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        _activeFB.Use();
        _mainShader.Use();
        _gl.ClearColor(0, 0, 0, 0);
        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        uint maskStep = 0;
        int compositeDepth = 0;
        foreach (var cmd in puppet.DrawList.Commands)
        {
            var temp = (PartVars*)cmd.Variables;
            _gl.BindBuffer(GlApi.GL_UNIFORM_BUFFER, _ubo);
            //_gl.BufferSubData(_ubo, 0, 64, cmd.Variables);
            _gl.BufferData(GlApi.GL_UNIFORM_BUFFER, 64, cmd.Variables, GlApi.GL_DYNAMIC_DRAW);
            _gl.BindBufferBase(GlApi.GL_UNIFORM_BUFFER, 0, _ubo);

            switch (cmd.State)
            {
                case DrawState.Normal:
                    if (maskStep > 0)
                    {
                        maskStep = 0;

                        // Disable masking.
                        _maskFB.Use();
                        _gl.ClearColor(1, 1, 1, 1);
                        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);
                        _gl.ClearColor(0, 0, 0, 0);

                        // Re-enable main FB.
                        _activeFB.Use();
                        _mainShader.Use();
                    }

                    _mainShader.SetUniform(_mainModelViewMatrix, cam.Matrix);
                    _maskFB.Textures[0].Bind(0);
                    uint i = 0;
                    foreach (var src in cmd.Sources)
                    {
                        if (src != null && src.Id is GLTexture tex1)
                        {
                            tex1.Bind(i + 1);
                        }
                        i++;
                    }

                    InSetBlendModeLegacy(cmd.BlendMode);
                    _gl.DrawElementsBaseVertex(
                        GlApi.GL_TRIANGLES,
                        (uint)cmd.ElemCount,
                        GlApi.GL_UNSIGNED_INT,
                        cmd.IdxOffset * 4,
                        cmd.VtxOffset
                    );
                    break;

                case DrawState.DefineMask:
                    if (maskStep != 1)
                    {
                        maskStep = 1;

                        // Start mask FB
                        _maskFB.Use();
                        _maskShader.Use();

                        // Clear mask buffer and set blend mode.
                        _gl.ClearColor(0, 0, 0, 0);
                        _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);
                        _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE);
                    }

                    _maskShader.SetUniform(_maskModelViewMatrix, cam.Matrix);
                    _maskShader.SetUniform(_maskMode, cmd.MaskMode == MaskingMode.Mask);
                    (cmd.Sources[0].Id as GLTexture)?.Bind(0);

                    _gl.DrawElementsBaseVertex(
                        GlApi.GL_TRIANGLES,
                        (uint)cmd.ElemCount,
                        GlApi.GL_UNSIGNED_INT,
                        cmd.IdxOffset * 4,
                        cmd.VtxOffset
                    );
                    break;

                case DrawState.MaskedDraw:
                    if (maskStep == 0)
                        continue;

                    // Start main FB
                    if (maskStep == 1)
                    {
                        maskStep = 2;
                        _activeFB.Use();
                        _mainShader.Use();
                    }

                    _mainShader.SetUniform(_mainModelViewMatrix, cam.Matrix);
                    _maskFB.Textures[0].Bind(0);
                    i = 0;
                    foreach (var src in cmd.Sources)
                    {
                        if (src != null && src.Id is GLTexture tex)
                        {
                            tex.Bind(i + 1);
                        }
                    }

                    InSetBlendModeLegacy(cmd.BlendMode);
                    _gl.DrawElementsBaseVertex(
                        GlApi.GL_TRIANGLES,
                        (uint)cmd.ElemCount,
                        GlApi.GL_UNSIGNED_INT,
                        cmd.IdxOffset * 4,
                        cmd.VtxOffset
                    );
                    break;

                case DrawState.CompositeBegin:
                    compositeDepth++;
                    if (compositeDepth >= _compFBs.Count)
                    {
                        _compFBs.Add(new GLFramebuffer(_gl, _sceneWidth, _sceneHeight));
                        for (int j = 0; j < 4; j++)
                        {
                            _compFBs[^1].Attach(new GLTexture(_gl, GlApi.GL_RGBA, _sceneWidth, _sceneHeight));
                        }
                    }

                    _activeFB = _compFBs[compositeDepth - 1];
                    _activeFB.Use();
                    _gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);
                    break;

                case DrawState.CompositeEnd:
                    compositeDepth--;
                    _activeFB = compositeDepth > 0 ? _compFBs[compositeDepth - 1] : _mainFB;
                    _activeFB.Use();
                    break;

                case DrawState.CompositeBlit:
                    _maskFB.Textures[0].Bind(0);
                    _compFBs[compositeDepth].BindAsTarget(1);

                    _activeFB.Use();
                    _mainShader.Use();
                    _mainShader.SetUniform(_mainModelViewMatrix, Matrix4x4.Identity);
                    InSetBlendModeLegacy(cmd.BlendMode);
                    _gl.DrawElementsBaseVertex(
                        GlApi.GL_TRIANGLES,
                        (uint)cmd.ElemCount,
                        GlApi.GL_UNSIGNED_INT,
                        cmd.IdxOffset * 4,
                        cmd.VtxOffset
                    );
                    break;
            }
        }
    }

    public void PostRender(uint fb)
    {
        _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);
        _mainFB.BlitTo(fb);
    }

    public void AddPuppet(Puppet puppet)
    {
        foreach (var tex in puppet.TextureCache.Cache)
        {
            tex.Id = new GLTexture(_gl, GlApi.GL_RGBA, tex.Width, tex.Height, tex.Pixels);
        }
    }

    internal void InSetBlendModeLegacy(BlendMode blendingMode)
    {
        switch (blendingMode)
        {
            // If the advanced blending extension is not supported, force to Normal blending
            default:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Normal:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Multiply:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Screen:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR); break;

            case BlendMode.Lighten:
                _gl.BlendEquation(GlApi.GL_MAX);
                _gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.ColorDodge:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.LinearDodge:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.AddGlow:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Subtract:
                _gl.BlendEquationSeparate(GlApi.GL_FUNC_REVERSE_SUBTRACT, GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.Exclusion:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFuncSeparate(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.Inverse:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.DestinationIn:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_SRC_ALPHA); break;

            case BlendMode.SourceIn:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_DST_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.SourceOut:
                _gl.BlendEquation(GlApi.GL_FUNC_ADD);
                _gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;
        }
    }
}
