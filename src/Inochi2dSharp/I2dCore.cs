using System.Numerics;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Nodes.MeshGroups;
using Inochi2dSharp.Math;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp;

public partial class I2dCore : IDisposable
{
    internal readonly I2dTime I2dTime;

    // Viewport
    private int _inViewportWidth;
    private int _inViewportHeight;

    private readonly uint _sceneVAO;
    private readonly uint _sceneVBO;

    private readonly uint _fBuffer;
    private readonly uint _fAlbedo;
    private readonly uint _fEmissive;
    private readonly uint _fBump;
    private readonly uint _fStencil;

    private readonly uint _cfBuffer;
    private readonly uint _cfAlbedo;
    private readonly uint _cfEmissive;
    private readonly uint _cfBump;
    private readonly uint _cfStencil;

    private Vector4 _inClearColor;

    private readonly PostProcessingShader _basicSceneShader;
    private readonly PostProcessingShader _basicSceneLighting;

    public List<PostProcessingShader> PostProcessingStack { get; private set; } = [];

    public GlApi gl;

    // Camera
    public Camera InCamera { get; set; }

    private bool _isCompositing;

    /// <summary>
    /// Ambient light value
    /// </summary>
    public Vector3 InSceneAmbientLight = new(0.1f, 0.1f, 0.2f);

    /// <summary>
    /// Color of the light shining into the scene
    /// </summary>
    public Vector3 InSceneLightColor = new(0.65f, 0.54f, 0.54f);

    /// <summary>
    /// Unit vector describing the direction of the light
    /// </summary>
    public Vector3 InSceneLightDirection = new(0, 0, 1);

    private int _indiceCount;
    private uint _cVBO;
    private int _mvpId;
    private int _colorId;

    /// <summary>
    /// Initializes the renderer
    /// </summary>
    public I2dCore(GlApi gl, Func<float>? func)
    {
        TypeList.Init();

        this.gl = gl;
        I2dTime = new(func);

        // Initialize dynamic meshes
        InInitBlending();
        InInitDrawable();
        InInitPart();
        InInitComposite();
        InInitDebug();

        // Some defaults that should be changed by app writer
        InCamera = new Camera(this);

        _inClearColor = new(0, 0, 0, 0);
        // Shader for scene
        _basicSceneShader = new PostProcessingShader(new Shader(this, "scene", Integration.ScencVert, Integration.SceneFrag));
        _sceneVAO = gl.GenVertexArray();
        _sceneVBO = gl.GenBuffer();

        // Generate the framebuffer we'll be using to render the model and composites
        _fBuffer = gl.GenFramebuffer();
        _cfBuffer = gl.GenFramebuffer();

        // Generate the color and stencil-depth textures needed
        // Note: we're not using the depth buffer but OpenGL 3.4 does not support stencil-only buffers
        _fAlbedo = gl.GenTexture();
        _fEmissive = gl.GenTexture();
        _fBump = gl.GenTexture();
        _fStencil = gl.GenTexture();

        _cfAlbedo = gl.GenTexture();
        _cfEmissive = gl.GenTexture();
        _cfBump = gl.GenTexture();
        _cfStencil = gl.GenTexture();

        // Set the viewport and by extension set the textures
        InSetViewport(640, 480);

        // Attach textures to framebuffer
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _fBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, _fAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, _fEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, _fBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, _fStencil, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _cfBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, _cfAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, _cfEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, _cfBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, _cfStencil, 0);

        // go back to default fb
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);
    }

    public void RenderScene(Vector4 area, PostProcessingShader shaderToUse, uint albedo, uint emissive, uint bump)
    {
        gl.Viewport(0, 0, (int)area.Z, (int)area.W);

        // Bind our vertex array
        gl.BindVertexArray(_sceneVAO);

        gl.Disable(GlApi.GL_CULL_FACE);
        gl.Disable(GlApi.GL_DEPTH_TEST);
        gl.Enable(GlApi.GL_BLEND);
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

        shaderToUse.Shader.Use();
        shaderToUse.Shader.SetUniform(shaderToUse.GetUniform("mvpModel"),
            Matrix4x4.Identity
        );
        shaderToUse.Shader.SetUniform(shaderToUse.GetUniform("mvpView"),
            Matrix4x4.CreateTranslation(area.X, area.Y, 0)
        );
        shaderToUse.Shader.SetUniform(shaderToUse.GetUniform("mvpProjection"),
           Matrix4x4.CreateOrthographicOffCenter(0, area.Z, area.W, 0, 0, float.Max(area.Z, area.W))
        );

        // Ambient light
        int ambientLightUniform = shaderToUse.GetUniform("ambientLight");
        if (ambientLightUniform != -1) shaderToUse.Shader.SetUniform(ambientLightUniform, InSceneAmbientLight);


        // Light direction
        int lightDirectionUniform = shaderToUse.GetUniform("lightDirection");
        if (lightDirectionUniform != -1) shaderToUse.Shader.SetUniform(lightDirectionUniform, InSceneLightDirection);

        // Colored light
        int lightColorUniform = shaderToUse.GetUniform("lightColor");
        if (lightColorUniform != -1) shaderToUse.Shader.SetUniform(lightColorUniform, InSceneLightColor);

        // framebuffer size
        int fbSizeUniform = shaderToUse.GetUniform("fbSize");
        if (fbSizeUniform != -1) shaderToUse.Shader.SetUniform(fbSizeUniform, new Vector2(_inViewportWidth, _inViewportHeight));

        // Bind the texture
        gl.ActiveTexture(GlApi.GL_TEXTURE0);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, albedo);
        gl.ActiveTexture(GlApi.GL_TEXTURE1);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, emissive);
        gl.ActiveTexture(GlApi.GL_TEXTURE2);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, bump);

        // Enable points array
        gl.EnableVertexAttribArray(0); // verts
        gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 4 * sizeof(float), 0);

        // Enable UVs array
        gl.EnableVertexAttribArray(1); // uvs
        gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 4 * sizeof(float), 2 * sizeof(float));

        // Draw
        gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);

        // Disable the vertex attribs after use
        gl.DisableVertexAttribArray(0);
        gl.DisableVertexAttribArray(1);

        gl.Disable(GlApi.GL_BLEND);
    }

    /// <summary>
    /// Sets the light direction in 2D.
    /// </summary>
    /// <param name="radians"></param>
    public void InSceneSetLightDirection(float radians)
    {
        InSceneLightDirection = new(MathF.Cos(radians), MathF.Sin(radians), InSceneLightDirection.Z);
    }

    /// <summary>
    /// Begins rendering to the framebuffer
    /// </summary>
    public void InBeginScene()
    {
        gl.BindVertexArray(_sceneVAO);
        gl.Enable(GlApi.GL_BLEND);
        gl.Enable(GlApi.GL_BLEND, 0);
        gl.Enable(GlApi.GL_BLEND, 1);
        gl.Enable(GlApi.GL_BLEND, 2);
        gl.Disable(GlApi.GL_DEPTH_TEST);
        gl.Disable(GlApi.GL_CULL_FACE);

        // Make sure to reset our viewport if someone has messed with it
        gl.Viewport(0, 0, _inViewportWidth, _inViewportHeight);

        // Bind and clear composite framebuffer
        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, _cfBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(0, 0, 0, 0);

        // Bind our framebuffer
        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, _fBuffer);

        // First clear buffer 0
        gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);
        gl.ClearColor(_inClearColor.X, _inClearColor.Y, _inClearColor.Z, _inClearColor.W);
        gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        // Then clear others with black
        gl.DrawBuffers(2, [GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(0, 0, 0, 1);
        gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        // Everything else is the actual texture used by the meshes at id 0
        gl.ActiveTexture(GlApi.GL_TEXTURE0);

        // Finally we render to all buffers
        gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
    }

    /// <summary>
    /// Begins a composition step
    /// </summary>
    public void InBeginComposite()
    {
        // We don't allow recursive compositing
        if (_isCompositing) return;
        _isCompositing = true;

        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, _cfBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(0, 0, 0, 0);
        gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        // Everything else is the actual texture used by the meshes at id 0
        gl.ActiveTexture(GlApi.GL_TEXTURE0);
        gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);
    }

    /// <summary>
    /// Ends a composition step, re-binding the internal framebuffer
    /// </summary>
    public void InEndComposite()
    {
        // We don't allow recursive compositing
        if (!_isCompositing) return;
        _isCompositing = false;

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _fBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.Flush();
    }

    /// <summary>
    /// Ends rendering to the framebuffer
    /// </summary>
    public void InEndScene()
    {
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);

        gl.Disable(GlApi.GL_BLEND, 0);
        gl.Disable(GlApi.GL_BLEND, 1);
        gl.Disable(GlApi.GL_BLEND, 2);
        gl.Enable(GlApi.GL_DEPTH_TEST);
        gl.Enable(GlApi.GL_CULL_FACE);
        gl.Disable(GlApi.GL_BLEND);
        gl.Flush();
        gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);
    }

    /// <summary>
    /// Runs post processing on the scene
    /// </summary>
    public unsafe void InPostProcessScene()
    {
        if (PostProcessingStack.Count == 0) return;

        bool targetBuffer = false;

        // These are passed to glSetClearColor for transparent export
        InGetClearColor(out float r, out float g, out float b, out float a);

        // Render area
        var area = new Vector4(
            0, 0,
            _inViewportWidth, _inViewportHeight
        );

        // Tell OpenGL the resolution to render at
        float[] data =
        [
            area.X,         area.Y+area.W,   0, 0,
            area.X,         area.Y,          0, 1,
            area.X+area.Z,  area.Y+area.W,   1, 0,
            area.X+area.Z,  area.Y+area.W,   1, 0,
            area.X,         area.Y,          0, 1,
            area.X+area.Z,  area.Y,          1, 1,
        ];
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _sceneVBO);
        fixed (void* ptr = data)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 24 * sizeof(float), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        gl.ActiveTexture(GlApi.GL_TEXTURE1);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fEmissive);
        gl.GenerateMipmap(GlApi.GL_TEXTURE_2D);

        // We want to be able to post process all the attachments
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _cfBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(r, g, b, a);
        gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _fBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

        foreach (var shader in PostProcessingStack)
        {
            targetBuffer = !targetBuffer;

            if (targetBuffer)
            {
                // Main buffer -> Composite buffer
                gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _cfBuffer); // dst
                RenderScene(area, shader, _fAlbedo, _fEmissive, _fBump); // src
            }
            else
            {
                // Composite buffer -> Main buffer 
                gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _fBuffer); // dst
                RenderScene(area, shader, _cfAlbedo, _cfEmissive, _cfBump); // src
            }
        }

        if (targetBuffer)
        {
            gl.BindFramebuffer(GlApi.GL_READ_FRAMEBUFFER, _cfBuffer);
            gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, _fBuffer);
            gl.BlitFramebuffer(
                0, 0, _inViewportWidth, _inViewportHeight, // src rect
                0, 0, _inViewportWidth, _inViewportHeight, // dst rect
                GlApi.GL_COLOR_BUFFER_BIT, // blit mask
                GlApi.GL_LINEAR // blit filter
            );
        }

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);
    }

    /// <summary>
    /// Add basic lighting shader to processing stack
    /// </summary>
    public void InPostProcessingAddBasicLighting()
    {
        PostProcessingStack.Add(new PostProcessingShader(
            new Shader(this, "scene+lighting",
                Integration.ScencVert,
                Integration.LighingFrag
            )
        ));
    }

    /// <summary>
    /// Draw scene to area
    /// </summary>
    /// <param name="area"></param>
    public unsafe void InDrawScene(Vector4 area)
    {
        float[] data =
        [
            area.X,         area.Y+area.W,  0, 0,
            area.X,         area.Y,         0, 1,
            area.X+area.Z,  area.Y+area.W,  1, 0,
            area.X+area.Z,  area.Y+area.W,  1, 0,
            area.X,         area.Y,         0, 1,
            area.X+area.Z,  area.Y,         1, 1,
        ];

        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _sceneVBO);
        fixed (void* ptr = data)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 24 * sizeof(float), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }
        RenderScene(area, _basicSceneShader, _fAlbedo, _fEmissive, _fBump);
    }

    public void IncCompositePrepareRender()
    {
        gl.ActiveTexture(GlApi.GL_TEXTURE0);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfAlbedo);
        gl.ActiveTexture(GlApi.GL_TEXTURE1);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfEmissive);
        gl.ActiveTexture(GlApi.GL_TEXTURE2);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfBump);
    }

    /// <summary>
    /// Gets the Inochi2D framebuffer 
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public uint InGetFramebuffer()
    {
        return _fBuffer;
    }

    /// <summary>
    /// Gets the Inochi2D framebuffer render image
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public uint InGetRenderImage()
    {
        return _fAlbedo;
    }

    /// <summary>
    /// Gets the Inochi2D composite render image
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public uint InGetCompositeImage()
    {
        return _cfAlbedo;
    }

    /// <summary>
    /// Sets the viewport area to render to
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void InSetViewport(int width, int height)
    {
        // Skip resizing when not needed.
        if (width == _inViewportWidth && height == _inViewportHeight) return;

        _inViewportWidth = width;
        _inViewportHeight = height;

        // Render Framebuffer
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fAlbedo);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fEmissive);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_FLOAT, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fBump);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fStencil);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_DEPTH24_STENCIL8, width, height, 0, GlApi.GL_DEPTH_STENCIL, GlApi.GL_UNSIGNED_INT_24_8, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _fBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, _fAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, _fEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, _fBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, _fStencil, 0);

        // Composite framebuffer
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfAlbedo);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfEmissive);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_FLOAT, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfBump);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, _cfStencil);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_DEPTH24_STENCIL8, width, height, 0, GlApi.GL_DEPTH_STENCIL, GlApi.GL_UNSIGNED_INT_24_8, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, _cfBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, _cfAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, _cfEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, _cfBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, _cfStencil, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);

        gl.Viewport(0, 0, width, height);
    }

    /// <summary>
    /// Gets the viewport
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public void InGetViewport(out int width, out int height)
    {
        width = _inViewportWidth;
        height = _inViewportHeight;
    }

    /// <summary>
    /// Returns length of viewport data for extraction
    /// </summary>
    /// <returns></returns>
    public int InViewportDataLength()
    {
        return _inViewportWidth * _inViewportHeight * 4;
    }

    /// <summary>
    /// Dumps viewport data to texture stream
    /// </summary>
    /// <param name="dumpTo"></param>
    public unsafe void InDumpViewport(byte[] dumpTo)
    {
        if (dumpTo.Length >= InViewportDataLength())
        {
            throw new Exception("Invalid data destination length for inDumpViewport");
        }
        gl.BindTexture(GlApi.GL_TEXTURE_2D, _fAlbedo);
        fixed (void* ptr = dumpTo)
        {
            gl.GetTexImage(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
        }

        // We need to flip it because OpenGL renders stuff with a different coordinate system
        byte[] tmpLine = new byte[_inViewportWidth * 4];
        int ri = 0;
        for (int i = _inViewportHeight / 2; i < _inViewportHeight; i++)
        {
            int lineSize = _inViewportWidth * 4;
            int oldLineStart = lineSize * ri;
            int newLineStart = lineSize * i;

            // Copy the line data
            Array.Copy(dumpTo, oldLineStart, tmpLine, 0, lineSize);
            Array.Copy(dumpTo, newLineStart, dumpTo, oldLineStart, lineSize);
            Array.Copy(tmpLine, 0, dumpTo, newLineStart, lineSize);

            ri++;
        }
    }
    /// <summary>
    /// Sets the background clear color
    /// </summary>
    /// <param name="r"></param>
    /// <param name="g"></param>
    /// <param name="b"></param>
    /// <param name="a"></param>
    private void InSetClearColor(float r, float g, float b, float a)
    {
        _inClearColor = new(r, g, b, a);
    }

    private void InGetClearColor(out float r, out float g, out float b, out float a)
    {
        r = _inClearColor.X;
        g = _inClearColor.Y;
        b = _inClearColor.Z;
        a = _inClearColor.W;
    }

    public void InUpdate()
    {
        I2dTime.InUpdate();
    }

    public void TickTime(float time)
    {
        I2dTime.AddTime(time);
    }

    public void Dispose()
    {
        
    }
}
