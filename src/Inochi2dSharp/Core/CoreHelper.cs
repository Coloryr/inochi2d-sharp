using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Inochi2dSharp.Shaders;
using SkiaSharp;

namespace Inochi2dSharp.Core;

public static class CoreHelper
{
    public static GlApi gl;

    // Viewport
    private static int inViewportWidth;
    private static int inViewportHeight;

    private static uint sceneVAO;
    private static uint sceneVBO;

    private static uint fBuffer;
    private static uint fAlbedo;
    private static uint fEmissive;
    private static uint fBump;
    private static uint fStencil;

    private static uint cfBuffer;
    private static uint cfAlbedo;
    private static uint cfEmissive;
    private static uint cfBump;
    private static uint cfStencil;

    private static Vector4 inClearColor;

    private static PostProcessingShader basicSceneShader;
    private static PostProcessingShader basicSceneLighting;

    public static List<PostProcessingShader> postProcessingStack { get; private set; } = [];

    // Camera
    public static Camera inCamera { get; set; }

    private static bool isCompositing;

    /// <summary>
    /// Ambient light value
    /// </summary>
    public static Vector3 inSceneAmbientLight = new(0.1f, 0.1f, 0.2f);

    /// <summary>
    /// Color of the light shining into the scene
    /// </summary>
    public static Vector3 inSceneLightColor = new(0.65f, 0.54f, 0.54f);

    /// <summary>
    /// Unit vector describing the direction of the light
    /// </summary>
    public static Vector3 inSceneLightDirection = new(0, 0, 1);

    public static int indiceCount;

    public static Shader dbgShaderLine;
    public static Shader dbgShaderPoint;
    public static uint dbgVAO;
    public static uint dbgVBO;
    public static uint dbgIBO;

    public static uint cVBO;

    public static int mvpId;
    public static int colorId;

    private static bool inDbgDrawMeshOutlines = false;
    private static bool inDbgDrawMeshVertexPoints = false;
    private static bool inDbgDrawMeshOrientation = false;

    private static List<Texture> textureBindings = [];
    private static bool started = false;

    public static void renderScene(Vector4 area, PostProcessingShader shaderToUse, uint albedo, uint emissive, uint bump)
    {
        gl.Viewport(0, 0, (int)area.Z, (int)area.W);

        // Bind our vertex array
        gl.BindVertexArray(sceneVAO);

        gl.Disable(GlApi.GL_CULL_FACE);
        gl.Disable(GlApi.GL_DEPTH_TEST);
        gl.Enable(GlApi.GL_BLEND);
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

        shaderToUse.shader.use();
        shaderToUse.shader.setUniform(shaderToUse.getUniform("mvpModel"),
            Matrix4x4.Identity
        );
        shaderToUse.shader.setUniform(shaderToUse.getUniform("mvpView"),
            Matrix4x4.CreateTranslation(area.X, area.Y, 0)
        );
        shaderToUse.shader.setUniform(shaderToUse.getUniform("mvpProjection"),
           Matrix4x4.CreateOrthographicOffCenter(0, area.Z, area.W, 0, 0, float.Max(area.Z, area.W))
        );

        // Ambient light
        int ambientLightUniform = shaderToUse.getUniform("ambientLight");
        if (ambientLightUniform != -1) shaderToUse.shader.setUniform(ambientLightUniform, inSceneAmbientLight);


        // Light direction
        int lightDirectionUniform = shaderToUse.getUniform("lightDirection");
        if (lightDirectionUniform != -1) shaderToUse.shader.setUniform(lightDirectionUniform, inSceneLightDirection);

        // Colored light
        int lightColorUniform = shaderToUse.getUniform("lightColor");
        if (lightColorUniform != -1) shaderToUse.shader.setUniform(lightColorUniform, inSceneLightColor);

        // framebuffer size
        int fbSizeUniform = shaderToUse.getUniform("fbSize");
        if (fbSizeUniform != -1) shaderToUse.shader.setUniform(fbSizeUniform, new Vector2(inViewportWidth, inViewportHeight));

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
    /// Initializes the renderer
    /// </summary>
    public static void initRenderer()
    {
        // Set the viewport and by extension set the textures
        inSetViewport(640, 480);

        // Initialize dynamic meshes
        inInitBlending();
        inInitNodes();
        inInitDrawable();
        inInitPart();
        inInitComposite();
        inInitMeshGroup();
        inInitDebug();

        // Some defaults that should be changed by app writer
        inCamera = new Camera();

        inClearColor = new(0, 0, 0, 0);
        // Shader for scene
        basicSceneShader = new PostProcessingShader(new Shader("scene", Integration.inScencVert, Integration.inSceneFrag));
        gl.GenVertexArrays(1, out sceneVAO);
        gl.GenBuffers(1, out sceneVBO);

        // Generate the framebuffer we'll be using to render the model and composites
        gl.GenFramebuffers(1, out fBuffer);
        gl.GenFramebuffers(1, out cfBuffer);

        // Generate the color and stencil-depth textures needed
        // Note: we're not using the depth buffer but OpenGL 3.4 does not support stencil-only buffers
        gl.GenTextures(1, out fAlbedo);
        gl.GenTextures(1, out fEmissive);
        gl.GenTextures(1, out fBump);
        gl.GenTextures(1, out fStencil);

        gl.GenTextures(1, out cfAlbedo);
        gl.GenTextures(1, out cfEmissive);
        gl.GenTextures(1, out cfBump);
        gl.GenTextures(1, out cfStencil);

        // Attach textures to framebuffer
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, fBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, fAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, fEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, fBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, fStencil, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, cfBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, cfAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, cfEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, cfBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, cfStencil, 0);

        // go back to default fb
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);
    }

    /// <summary>
    /// Sets the light direction in 2D.
    /// </summary>
    /// <param name="radians"></param>
    public static void inSceneSetLightDirection(float radians)
    {
        inSceneLightDirection = new(MathF.Cos(radians), MathF.Sin(radians), inSceneLightDirection.Z);
    }

    /// <summary>
    /// Begins rendering to the framebuffer
    /// </summary>
    public static void inBeginScene()
    {
        gl.BindVertexArray(sceneVAO);
        gl.Enable(GlApi.GL_BLEND);
        gl.Enable(GlApi.GL_BLEND, 0);
        gl.Enable(GlApi.GL_BLEND, 1);
        gl.Enable(GlApi.GL_BLEND, 2);
        gl.Disable(GlApi.GL_DEPTH_TEST);
        gl.Disable(GlApi.GL_CULL_FACE);

        // Make sure to reset our viewport if someone has messed with it
        gl.Viewport(0, 0, inViewportWidth, inViewportHeight);

        // Bind and clear composite framebuffer
        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, cfBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(0, 0, 0, 0);

        // Bind our framebuffer
        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, fBuffer);

        // First clear buffer 0
        gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);
        gl.ClearColor(inClearColor.X, inClearColor.Y, inClearColor.Z, inClearColor.W);
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
    public static void inBeginComposite()
    {
        // We don't allow recursive compositing
        if (isCompositing) return;
        isCompositing = true;

        gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, cfBuffer);
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
    public static void inEndComposite()
    {
        // We don't allow recursive compositing
        if (!isCompositing) return;
        isCompositing = false;

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, fBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.Flush();
    }

    /// <summary>
    /// Ends rendering to the framebuffer
    /// </summary>
    public static void inEndScene()
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
    public unsafe static void inPostProcessScene()
    {
        if (postProcessingStack.Count == 0) return;

        bool targetBuffer  = false;

        // These are passed to glSetClearColor for transparent export
        inGetClearColor(out float r, out float g, out float b, out float a);

        // Render area
        var area = new Vector4(
            0, 0,
            inViewportWidth, inViewportHeight
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
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sceneVBO);
        fixed (void* ptr = data)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 24 * sizeof(float), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        gl.ActiveTexture(GlApi.GL_TEXTURE1);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, fEmissive);
        gl.GenerateMipmap(GlApi.GL_TEXTURE_2D);

        // We want to be able to post process all the attachments
        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, cfBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        gl.ClearColor(r, g, b, a);
        gl.Clear(GlApi.GL_COLOR_BUFFER_BIT);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, fBuffer);
        gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

        foreach (var shader in postProcessingStack) 
        {
            targetBuffer = !targetBuffer;

            if (targetBuffer)
            {
                // Main buffer -> Composite buffer
                gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, cfBuffer); // dst
                renderScene(area, shader, fAlbedo, fEmissive, fBump); // src
            }
            else
            {
                // Composite buffer -> Main buffer 
                gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, fBuffer); // dst
                renderScene(area, shader, cfAlbedo, cfEmissive, cfBump); // src
            }
        }

        if (targetBuffer)
        {
            gl.BindFramebuffer(GlApi.GL_READ_FRAMEBUFFER, cfBuffer);
            gl.BindFramebuffer(GlApi.GL_DRAW_FRAMEBUFFER, fBuffer);
            gl.BlitFramebuffer(
                0, 0, inViewportWidth, inViewportHeight, // src rect
                0, 0, inViewportWidth, inViewportHeight, // dst rect
                GlApi.GL_COLOR_BUFFER_BIT, // blit mask
                GlApi.GL_LINEAR // blit filter
            );
        }

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);
    }

    /// <summary>
    /// Add basic lighting shader to processing stack
    /// </summary>
    public static void inPostProcessingAddBasicLighting()
    {
        postProcessingStack.Add(new PostProcessingShader(
            new Shader("scene+lighting",
                Integration.inScencVert,
                Integration.inLighingFrag
            )
        ));
    }

    /// <summary>
    /// Draw scene to area
    /// </summary>
    /// <param name="area"></param>
    public unsafe static void inDrawScene(Vector4 area)
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

        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sceneVBO);
        fixed (void * ptr = data)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 24 * sizeof(float), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }
        renderScene(area, basicSceneShader, fAlbedo, fEmissive, fBump);
    }

    public static void incCompositePrepareRender()
    {
        gl.ActiveTexture(GlApi.GL_TEXTURE0);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfAlbedo);
        gl.ActiveTexture(GlApi.GL_TEXTURE1);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfEmissive);
        gl.ActiveTexture(GlApi.GL_TEXTURE2);
        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfBump);
    }

    /// <summary>
    /// Gets the Inochi2D framebuffer 
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public static uint inGetFramebuffer()
    {
        return fBuffer;
    }

    /// <summary>
    /// Gets the Inochi2D framebuffer render image
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public static uint inGetRenderImage()
    {
        return fAlbedo;
    }

    /// <summary>
    /// Gets the Inochi2D composite render image
    /// 
    /// DO NOT MODIFY THIS IMAGE!
    /// </summary>
    /// <returns></returns>
    public static uint inGetCompositeImage()
    {
        return cfAlbedo;
    }

    /// <summary>
    /// Sets the viewport area to render to
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void inSetViewport(int width, int height)
    {
        // Skip resizing when not needed.
        if (width == inViewportWidth && height == inViewportHeight) return;

        inViewportWidth = width;
        inViewportHeight = height;

        // Render Framebuffer
        gl.BindTexture(GlApi.GL_TEXTURE_2D, fAlbedo);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, fEmissive);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_FLOAT, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, fBump);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, fStencil);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_DEPTH24_STENCIL8, width, height, 0, GlApi.GL_DEPTH_STENCIL, GlApi.GL_UNSIGNED_INT_24_8, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, fBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, fAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, fEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, fBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, fStencil, 0);

        // Composite framebuffer
        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfAlbedo);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfEmissive);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_FLOAT, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfBump);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, width, height, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, 0);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MIN_FILTER, GlApi.GL_LINEAR);
        gl.TexParameterI(GlApi.GL_TEXTURE_2D, GlApi.GL_TEXTURE_MAG_FILTER, GlApi.GL_LINEAR);

        gl.BindTexture(GlApi.GL_TEXTURE_2D, cfStencil);
        gl.TexImage2D(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_DEPTH24_STENCIL8, width, height, 0, GlApi.GL_DEPTH_STENCIL, GlApi.GL_UNSIGNED_INT_24_8, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, cfBuffer);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_TEXTURE_2D, cfAlbedo, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_TEXTURE_2D, cfEmissive, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_COLOR_ATTACHMENT2, GlApi.GL_TEXTURE_2D, cfBump, 0);
        gl.FramebufferTexture2D(GlApi.GL_FRAMEBUFFER, GlApi.GL_DEPTH_STENCIL_ATTACHMENT, GlApi.GL_TEXTURE_2D, cfStencil, 0);

        gl.BindFramebuffer(GlApi.GL_FRAMEBUFFER, 0);

        gl.Viewport(0, 0, width, height);
    }

    /// <summary>
    /// Gets the viewport
    /// </summary>
    /// <param name="width"></param>
    /// <param name="height"></param>
    public static void inGetViewport(out int width, out int height)
    {
        width = inViewportWidth;
        height = inViewportHeight;
    }

    /// <summary>
    /// Returns length of viewport data for extraction
    /// </summary>
    /// <returns></returns>
    public static int inViewportDataLength()
    {
        return inViewportWidth * inViewportHeight * 4;
    }

    /// <summary>
    /// Dumps viewport data to texture stream
    /// </summary>
    /// <param name="dumpTo"></param>
    public unsafe static void inDumpViewport(byte[] dumpTo)
    {
        if (dumpTo.Length >= inViewportDataLength())
        {
            throw new Exception("Invalid data destination length for inDumpViewport");
        }
        gl.BindTexture(GlApi.GL_TEXTURE_2D, fAlbedo);
        fixed (void* ptr = dumpTo)
        {
            gl.GetTexImage(GlApi.GL_TEXTURE_2D, 0, GlApi.GL_RGBA, GlApi.GL_UNSIGNED_BYTE, new nint(ptr));
        }

        // We need to flip it because OpenGL renders stuff with a different coordinate system
        byte[] tmpLine = new byte[inViewportWidth * 4];
        int ri = 0;
        for (int i = inViewportHeight / 2; i < inViewportHeight; i++)
        {
            int lineSize = inViewportWidth * 4;
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
    private static void inSetClearColor(float r, float g, float b, float a)
    {
        inClearColor = new(r, g, b, a);
    }

    private static void inGetClearColor(out float r, out float g, out float b, out float a)
    {
        r = inClearColor.X;
        g = inClearColor.Y;
        b = inClearColor.Z;
        a = inClearColor.W;
    }

    private static void inInitDebug()
    {
        dbgShaderLine = new Shader("debug line", Integration.inDebugVert, Integration.inDebugLineFrag);
        dbgShaderPoint = new Shader("debug point", Integration.inDebugVert, Integration.inDebugPointFrag);
        gl.GenVertexArrays(1, out dbgVAO);
        gl.GenBuffers(1, out dbgVBO);
        gl.GenBuffers(1, out dbgIBO);

        mvpId = dbgShaderLine.getUniformLocation("mvp");
        colorId = dbgShaderLine.getUniformLocation("color");
    }

    private static void inUpdateDbgVerts(Vector3[] points)
    {
        // Generate bad line drawing indices
        ushort[] vts = new ushort[points.Length + 1];
        for (int i = 0; i < points.Length; i++)
        {
            vts[i] = (ushort)i;
        }
        vts[^1] = 0;

        inUpdateDbgVerts(points, vts);
    }

    private static unsafe void inUpdateDbgVerts(Vector3[] points, ushort[] indices)
    {
        gl.BindVertexArray(dbgVAO);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, dbgVBO);

        fixed (void* ptr = points)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, points.Length * Marshal.SizeOf<Vector3>(), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }

        cVBO = dbgVBO;

        indiceCount = indices.Length;
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, dbgIBO);
        fixed (void* ptr = indices)
        {
            gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, indices.Length * sizeof(ushort), new nint(ptr), GlApi.GL_DYNAMIC_DRAW);
        }
    }

    /// <summary>
    /// Size of debug points
    /// </summary>
    /// <param name="size"></param>
    public static void inDbgPointsSize(float size)
    {
        gl.PointSize(size);
    }

    /// <summary>
    ///  Size of debug points
    /// </summary>
    /// <param name="size"></param>
    public static void inDbgLineWidth(float size)
    {
        gl.LineWidth(size);
    }

    /// <summary>
    /// Draws points with specified color
    /// </summary>
    /// <param name="points"></param>
    public static void inDbgSetBuffer(Vector3[] points)
    {
        inUpdateDbgVerts(points);
    }

    /// <summary>
    ///  Sets buffer to buffer owned by an other OpenGL object
    /// </summary>
    /// <param name="vbo"></param>
    /// <param name="ibo"></param>
    /// <param name="count"></param>
    public static void inDbgSetBuffer(uint vbo, uint ibo, int count)
    {
        gl.BindVertexArray(dbgVAO);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, vbo);
        cVBO = vbo;
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, ibo);
        indiceCount = count;
    }

    /// <summary>
    /// Draws points with specified color
    /// </summary>
    /// <param name="points"></param>
    /// <param name="indices"></param>
    public static void inDbgSetBuffer(Vector3[] points, ushort[] indices)
    {
        inUpdateDbgVerts(points, indices);
    }

    public static void inDbgDrawPoints(Vector4 color)
    {
        inDbgDrawPoints(color, Matrix4x4.Identity);
    }

    /// <summary>
    /// Draws current stored vertices as points with specified color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="transform"></param>
    public static void inDbgDrawPoints(Vector4 color, Matrix4x4 transform)
    {
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFuncSeparate(GlApi.GL_SRC_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);

        gl.BindVertexArray(dbgVAO);

        dbgShaderPoint.use();
        dbgShaderPoint.setUniform(mvpId, inCamera.Matrix() * transform);
        dbgShaderPoint.setUniform(colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_POINTS, indiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
        gl.DisableVertexAttribArray(0);

        gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);
    }

    public static void inDbgDrawLines(Vector4 color)
    {
        inDbgDrawLines(color, Matrix4x4.Identity);
    }

    /// <summary>
    /// Draws current stored vertices as lines with specified color
    /// </summary>
    /// <param name="color"></param>
    /// <param name="transform"></param>
    public static void inDbgDrawLines(Vector4 color, Matrix4x4 transform)
    {
        gl.Enable(GlApi.GL_LINE_SMOOTH);
        gl.BlendEquation(GlApi.GL_FUNC_ADD);
        gl.BlendFuncSeparate(GlApi.GL_SRC_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);

        gl.BindVertexArray(dbgVAO);

        dbgShaderLine.use();
        dbgShaderLine.setUniform(mvpId, inCamera.Matrix() * transform);
        dbgShaderLine.setUniform(colorId, color);

        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, cVBO);
        gl.VertexAttribPointer(0, 3, GlApi.GL_FLOAT, false, 0, 0);

        gl.DrawElements(GlApi.GL_LINES, indiceCount, GlApi.GL_UNSIGNED_SHORT, 0);
        gl.DisableVertexAttribArray(0);

        gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA, GlApi.GL_ONE, GlApi.GL_ONE);
        gl.Disable(GlApi.GL_LINE_SMOOTH);
    }

    public static int GetChannel(this SKBitmap bitmap)
    {
        var type = bitmap.ColorType;
        if (type == SKColorType.Rgba8888 || type == SKColorType.Bgra8888)
        {
            return 4;
        }

        return 0;
    }

    public static float Clamp(float a, float b, float c)
    {
        return MathF.Min(MathF.Max(a, b), c);
    }

    /// <summary>
    /// Gets the maximum level of anisotropy
    /// </summary>
    /// <returns></returns>
    public static float IncGetMaxAnisotropy()
    {
        gl.GetFloat(GlApi.GL_MAX_TEXTURE_MAX_ANISOTROPY, out float max);
        return max;
    }

    /// <summary>
    /// Begins a texture loading pass
    /// </summary>
    /// <exception cref="Exception"></exception>
    public static void inBeginTextureLoading()
    {
        if (started)
        {
            throw new Exception("Texture loading pass already started!");
        }
        started = true;
    }

    /// <summary>
    /// Returns a texture from the internal texture list
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static Texture inGetTextureFromId(uint id)
    {
        if (!started)
        {
            throw new Exception("Texture loading pass not started!");
        }
        return textureBindings[(int)id];
    }

    /// <summary>
    /// Gets the latest texture from the internal texture list
    /// </summary>
    /// <returns></returns>
    public static Texture inGetLatestTexture()
    {
        return textureBindings[^1];
    }

    /// <summary>
    /// Adds binary texture
    /// </summary>
    /// <param name="data"></param>
    public static void inAddTextureBinary(ShallowTexture data)
    {
        textureBindings.Add(new Texture(data));
    }

    /// <summary>
    /// Ends a texture loading pass
    /// </summary>
    /// <param name="checkErrors"></param>
    public static void inEndTextureLoading(bool checkErrors = true)
    {
        if (checkErrors && !started)
        {
            throw new Exception("Texture loading pass not started!");
        }
        started = false;
        textureBindings.Clear();
    }

    public static void inTexPremultiply(byte[] data, int channels = 4)
    {
        if (channels < 4) return;

        for (int i = 0; i < data.Length / channels; i++) 
        {
            var offsetPixel = i * channels;
            data[offsetPixel + 0] = (byte)(data[offsetPixel + 0] * data[offsetPixel + 3] / 255);
            data[offsetPixel + 1] = (byte)(data[offsetPixel + 1] * data[offsetPixel + 3] / 255);
            data[offsetPixel + 2] = (byte)(data[offsetPixel + 2] * data[offsetPixel + 3] / 255);
        }
    }

    public static void inTexUnPremuliply(IntPtr ptr, long size)
    {
        unsafe
        {
            byte* data = (byte*)ptr;
            for (int i = 0; i < size / 4; i++)
            {
                if (data[(i * 4) + 3] == 0) continue;

                data[(i * 4) + 0] = (byte)(data[(i * 4) + 0] * 255 / data[(i * 4) + 3]);
                data[(i * 4) + 1] = (byte)(data[(i * 4) + 1] * 255 / data[(i * 4) + 3]);
                data[(i * 4) + 2] = (byte)(data[(i * 4) + 2] * 255 / data[(i * 4) + 3]);
            }
        }
    }

    public static void Save(this SKBitmap bitmap, string file)
    {
        byte[] temp;
        if (file.EndsWith(".png"))
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Png, 100).AsSpan().ToArray();
        }
        else if (file.EndsWith(".jpg"))
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Jpeg, 100).AsSpan().ToArray();
        }
        else
        {
            temp = bitmap.Encode(SKEncodedImageFormat.Bmp, 100).AsSpan().ToArray();
        }

        File.WriteAllBytes(file, temp);
    }
}
