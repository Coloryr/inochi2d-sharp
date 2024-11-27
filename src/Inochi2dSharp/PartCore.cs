using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Math;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp;

public partial class I2dCore
{
    public const uint NO_TEXTURE = uint.MaxValue;

    public Texture boundAlbedo;

    public Shader partShader;
    public Shader partShaderStage1;
    public Shader partShaderStage2;
    public Shader partMaskShader;

    /* GLSL Uniforms (Normal) */
    public int mvpModel;
    public int mvpViewProjection;
    public int offset;
    public int gopacity;
    public int gMultColor;
    public int gScreenColor;
    public int gEmissionStrength;

    /* GLSL Uniforms (Stage 1) */
    public int gs1MvpModel;
    public int gs1MvpViewProjection;
    public int gs1offset;
    public int gs1opacity;
    public int gs1MultColor;
    public int gs1ScreenColor;

    /* GLSL Uniforms (Stage 2) */
    public int gs2MvpModel;
    public int gs2MvpViewProjection;
    public int gs2offset;
    public int gs2opacity;
    public int gs2EmissionStrength;
    public int gs2MultColor;
    public int gs2ScreenColor;

    /* GLSL Uniforms (Masks) */
    public int mMvpModel;
    public int mMvpViewProjection;
    public int mthreshold;

    private uint sVertexBuffer;
    private uint sUVBuffer;
    private uint sElementBuffer;

    public void InInitPart()
    {
        partShader = new Shader(this, "part", ShaderCode.BasicVert, ShaderCode.BasicFrag);
        partShaderStage1 = new Shader(this, "part (stage 1)", ShaderCode.BasicVert, ShaderCode.BasicStage1);
        partShaderStage2 = new Shader(this, "part (stage 2)", ShaderCode.BasicVert, ShaderCode.BasicStage2);
        partMaskShader = new Shader(this, "part (mask)", ShaderCode.BasicVert, ShaderCode.BasicMask);

        IncDrawableBindVAO();

        partShader.Use();
        partShader.SetUniform(partShader.GetUniformLocation("albedo"), 0);
        partShader.SetUniform(partShader.GetUniformLocation("emissive"), 1);
        partShader.SetUniform(partShader.GetUniformLocation("bumpmap"), 2);
        mvpModel = partShader.GetUniformLocation("mvpModel");
        mvpViewProjection = partShader.GetUniformLocation("mvpViewProjection");
        offset = partShader.GetUniformLocation("offset");
        gopacity = partShader.GetUniformLocation("opacity");
        gMultColor = partShader.GetUniformLocation("multColor");
        gScreenColor = partShader.GetUniformLocation("screenColor");
        gEmissionStrength = partShader.GetUniformLocation("emissionStrength");

        partShaderStage1.Use();
        partShaderStage1.SetUniform(partShader.GetUniformLocation("albedo"), 0);
        gs1MvpModel = partShaderStage1.GetUniformLocation("mvpModel");
        gs1MvpViewProjection = partShaderStage1.GetUniformLocation("mvpViewProjection");
        gs1offset = partShaderStage1.GetUniformLocation("offset");
        gs1opacity = partShaderStage1.GetUniformLocation("opacity");
        gs1MultColor = partShaderStage1.GetUniformLocation("multColor");
        gs1ScreenColor = partShaderStage1.GetUniformLocation("screenColor");

        partShaderStage2.Use();
        partShaderStage2.SetUniform(partShaderStage2.GetUniformLocation("emissive"), 1);
        partShaderStage2.SetUniform(partShaderStage2.GetUniformLocation("bumpmap"), 2);
        gs2MvpModel = partShaderStage1.GetUniformLocation("mvpModel");
        gs2MvpViewProjection = partShaderStage1.GetUniformLocation("mvpViewProjection");
        gs2offset = partShaderStage2.GetUniformLocation("offset");
        gs2opacity = partShaderStage2.GetUniformLocation("opacity");
        gs2MultColor = partShaderStage2.GetUniformLocation("multColor");
        gs2ScreenColor = partShaderStage2.GetUniformLocation("screenColor");
        gs2EmissionStrength = partShaderStage2.GetUniformLocation("emissionStrength");

        partMaskShader.Use();
        partMaskShader.SetUniform(partMaskShader.GetUniformLocation("albedo"), 0);
        partMaskShader.SetUniform(partMaskShader.GetUniformLocation("emissive"), 1);
        partMaskShader.SetUniform(partMaskShader.GetUniformLocation("bumpmap"), 2);
        mMvpModel = partMaskShader.GetUniformLocation("mvpModel");
        mMvpViewProjection = partMaskShader.GetUniformLocation("mvpViewProjection");
        mthreshold = partMaskShader.GetUniformLocation("threshold");

        sVertexBuffer = gl.GenBuffer();
        sUVBuffer = gl.GenBuffer();
        sElementBuffer = gl.GenBuffer();
    }

    /// <summary>
    /// Creates a simple part that is sized after the texture given
    /// part is created based on file path given.
    /// Supported file types are: png, tga and jpeg
    /// 
    /// This is unoptimal for normal use and should only be used
    /// for real-time use when you want to add/remove parts on the fly
    /// </summary>
    /// <param name="file"></param>
    /// <param name="parent"></param>
    /// <returns></returns>
    public Part InCreateSimplePart(string file, Node? parent = null)
    {
        return InCreateSimplePart(new ShallowTexture(file), parent, file);
    }

    /// <summary>
    /// Creates a simple part that is sized after the texture given
    /// 
    /// This is unoptimal for normal use and should only be used
    /// for real-time use when you want to add/remove parts on the fly
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public Part InCreateSimplePart(ShallowTexture texture, Node? parent = null, string name = "New Part")
    {
        return InCreateSimplePart(new Texture(this, texture), parent, name);
    }

    /// <summary>
    /// Creates a simple part that is sized after the texture given
    /// 
    /// This is unoptimal for normal use and should only be used
    /// for real-time use when you want to add/remove parts on the fly
    /// </summary>
    /// <param name="tex"></param>
    /// <param name="parent"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public Part InCreateSimplePart(Texture tex, Node? parent = null, string name = "New Part")
    {
        var data = new MeshData()
        {
            Vertices =
            [
                new Vector2(-(tex.Width/2), -(tex.Height/2)),
                new Vector2(-(tex.Width/2), tex.Height/2),
                new Vector2(tex.Width/2, -(tex.Height/2)),
                new Vector2(tex.Width/2, tex.Height/2),
            ],
            Uvs =
            [
                new Vector2(0, 0),
                new Vector2(0, 1),
                new Vector2(1, 0),
                new Vector2(1, 1),
            ],
            Indices =
            [
                0, 1, 2,
                2, 1, 3
            ]
        };

        var p = new Part(this, data, [tex], parent)
        {
            Name = name
        };
        return p;
    }

    /// <summary>
    /// Draws a texture at the transform of the specified part
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="part"></param>
    public unsafe void InDrawTextureAtPart(Texture texture, Part part)
    {
        float texWidthP = texture.Width / 2;
        float texHeightP = texture.Height / 2;

        // Bind the vertex array
        IncDrawableBindVAO();

        partShader.Use();
        var temp = part.Transform().Matrix.Multiply(new Vector4(1, 1, 1, 1));
        partShader.SetUniform(mvpModel,
            MathHelper.Translation(temp.X, temp.Y, temp.Z)
        );
        partShader.SetUniform(mvpViewProjection,
            InCamera.Matrix()
        );
        partShader.SetUniform(gopacity, part.Opacity);
        partShader.SetUniform(gMultColor, part.Tint);
        partShader.SetUniform(gScreenColor, part.ScreenTint);

        // Bind the texture
        texture.Bind();

        // Enable points array
        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sVertexBuffer);
        float[] temp1 =
        [
            -texWidthP, -texHeightP,
            texWidthP, -texHeightP,
            -texWidthP, texHeightP,
            texWidthP, texHeightP,
        ];
        fixed (void* ptr = temp1)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        gl.EnableVertexAttribArray(1); // uvs
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sUVBuffer);
        temp1 =
        [
            0, 0,
            1, 0,
            0, 1,
            1, 1,
        ];
        fixed (void* ptr = temp1)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind element array and draw our mesh
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        ushort[] temp2 =
        [
            0, 1, 2,
            2, 1, 3
        ];
        fixed (void* ptr = temp2)
        {
            gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, 6 * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.DrawElements(GlApi.GL_TRIANGLES, 6, GlApi.GL_UNSIGNED_SHORT, 0);

        // Disable the vertex attribs after use
        gl.DisableVertexAttribArray(0);
        gl.DisableVertexAttribArray(1);
    }

    public void InDrawTextureAtPosition(Texture texture, Vector2 position, float opacity = 1)
    {
        InDrawTextureAtPosition(texture, position, opacity, new(1), new(0));
    }

    /// <summary>
    /// Draws a texture at the transform of the specified part
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="position"></param>
    /// <param name="opacity"></param>
    /// <param name="color"></param>
    /// <param name="screenColor"></param>
    public unsafe void InDrawTextureAtPosition(Texture texture, Vector2 position, float opacity, Vector3 color, Vector3 screenColor)
    {
        float texWidthP = texture.Width / 2;
        float texHeightP = texture.Height / 2;

        // Bind the vertex array
        IncDrawableBindVAO();

        partShader.Use();
        partShader.SetUniform(mvpModel,
            MathHelper.Scaling(1, 1, 1) * MathHelper.Translation(new Vector3(position, 0))
        );
        partShader.SetUniform(mvpViewProjection,
            InCamera.Matrix()
        );
        partShader.SetUniform(gopacity, opacity);
        partShader.SetUniform(gMultColor, color);
        partShader.SetUniform(gScreenColor, screenColor);

        // Bind the texture
        texture.Bind();

        // Enable points array
        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sVertexBuffer);
        float[] temp =
        [
            -texWidthP, -texHeightP,
            texWidthP, -texHeightP,
            -texWidthP, texHeightP,
            texWidthP, texHeightP,
        ];
        fixed (void* ptr = temp)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        gl.EnableVertexAttribArray(1); // uvs
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sUVBuffer);
        temp =
        [
            0, 0,
            1, 0,
            0, 1,
            1, 1,
        ];
        fixed (void* ptr = temp)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind element array and draw our mesh
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        ushort[] temp1 =
        [
            0, 1, 2,
            2, 1, 3
        ];
        fixed (void* ptr = temp1)
        {
            gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, 6 * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.DrawElements(GlApi.GL_TRIANGLES, 6, GlApi.GL_UNSIGNED_SHORT, 0);

        // Disable the vertex attribs after use
        gl.DisableVertexAttribArray(0);
        gl.DisableVertexAttribArray(1);
    }

    public void InDrawTextureAtRect(Texture texture, Rect area)
    {
        InDrawTextureAtRect(texture, area, new Rect(0, 0, 1, 1), 1, new Vector3(1, 1, 1), new Vector3(0, 0, 0), null, null);
    }

    /// <summary>
    /// Draws a texture at the transform of the specified part
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="area"></param>
    /// <param name="uvs"></param>
    /// <param name="opacity"></param>
    /// <param name="color"></param>
    /// <param name="screenColor"></param>
    /// <param name="s"></param>
    /// <param name="cam"></param>
    public unsafe void InDrawTextureAtRect(Texture texture, Rect area, Rect uvs, float opacity, Vector3 color, Vector3 screenColor, Shader? s, Camera? cam)
    {
        // Bind the vertex array
        IncDrawableBindVAO();

        s ??= partShader;
        cam ??= InCamera;
        s.Use();
        s.SetUniform(s.GetUniformLocation("mvp"),
            cam.Matrix() *
            MathHelper.Scaling(1, 1, 1)
        );
        s.SetUniform(s.GetUniformLocation("opacity"), opacity);
        s.SetUniform(s.GetUniformLocation("multColor"), color);
        s.SetUniform(s.GetUniformLocation("screenColor"), screenColor);

        // Bind the texture
        texture.Bind();

        // Enable points array
        gl.EnableVertexAttribArray(0);
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sVertexBuffer);
        float[] temp =
        [
            area.Left(), area.Top(),
            area.Right(), area.Top(),
            area.Left(), area.Bottom(),
            area.Right(), area.Bottom(),
        ];
        fixed (void* ptr = temp)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        gl.EnableVertexAttribArray(1); // uvs
        gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sUVBuffer);
        temp =
        [
            uvs.X, uvs.Y,
            uvs.Width, uvs.Y,
            uvs.X, uvs.Height,
            uvs.Width, uvs.Height
        ];
        fixed (void* ptr = temp)
        {
            gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind element array and draw our mesh
        gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        ushort[] temp1 =
        [
            0, 1, 2,
            2, 1, 3
        ];
        fixed (void* ptr = temp)
        {
            gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, 6 * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        gl.DrawElements(GlApi.GL_TRIANGLES, 6, GlApi.GL_UNSIGNED_SHORT, 0);

        // Disable the vertex attribs after use
        gl.DisableVertexAttribArray(0);
        gl.DisableVertexAttribArray(1);
    }
}
