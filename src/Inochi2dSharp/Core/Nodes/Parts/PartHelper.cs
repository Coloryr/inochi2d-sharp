using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp.Core.Nodes.Parts;

public static class PartHelper
{
    public const uint NO_TEXTURE = uint.MaxValue;

    public static Texture boundAlbedo;

    public static Shader partShader;
    public static Shader partShaderStage1;
    public static Shader partShaderStage2;
    public static Shader partMaskShader;

    /* GLSL Uniforms (Normal) */
    public static int mvpModel;
    public static int mvpViewProjection;
    public static int offset;
    public static int gopacity;
    public static int gMultColor;
    public static int gScreenColor;
    public static int gEmissionStrength;


    /* GLSL Uniforms (Stage 1) */
    public static int gs1MvpModel;
    public static int gs1MvpViewProjection;
    public static int gs1offset;
    public static int gs1opacity;
    public static int gs1MultColor;
    public static int gs1ScreenColor;


    /* GLSL Uniforms (Stage 2) */
    public static int gs2MvpModel;
    public static int gs2MvpViewProjection;
    public static int gs2offset;
    public static int gs2opacity;
    public static int gs2EmissionStrength;
    public static int gs2MultColor;
    public static int gs2ScreenColor;

    /* GLSL Uniforms (Masks) */
    public static int mMvpModel;
    public static int mMvpViewProjection;
    public static int mthreshold;

    private static uint sVertexBuffer;
    private static uint sUVBuffer;
    private static uint sElementBuffer;

    private static void inInitPart()
    {
        NodeHelper.RegisterNodeType<Part>();

        partShader = new Shader("part", Integration.inBasicVert, Integration.inBasicFrag);
        partShaderStage1 = new Shader("part (stage 1)", Integration.inBasicVert, Integration.inBasicStage1);
        partShaderStage2 = new Shader("part (stage 2)", Integration.inBasicVert, Integration.inBasicStage2);
        partMaskShader = new Shader("part (mask)", Integration.inBasicVert, Integration.inBasicMask);

        NodeHelper.incDrawableBindVAO();

        partShader.use();
        partShader.setUniform(partShader.getUniformLocation("albedo"), 0);
        partShader.setUniform(partShader.getUniformLocation("emissive"), 1);
        partShader.setUniform(partShader.getUniformLocation("bumpmap"), 2);
        mvpModel = partShader.getUniformLocation("mvpModel");
        mvpViewProjection = partShader.getUniformLocation("mvpViewProjection");
        offset = partShader.getUniformLocation("offset");
        gopacity = partShader.getUniformLocation("opacity");
        gMultColor = partShader.getUniformLocation("multColor");
        gScreenColor = partShader.getUniformLocation("screenColor");
        gEmissionStrength = partShader.getUniformLocation("emissionStrength");

        partShaderStage1.use();
        partShaderStage1.setUniform(partShader.getUniformLocation("albedo"), 0);
        gs1MvpModel = partShaderStage1.getUniformLocation("mvpModel");
        gs1MvpViewProjection = partShaderStage1.getUniformLocation("mvpViewProjection");
        gs1offset = partShaderStage1.getUniformLocation("offset");
        gs1opacity = partShaderStage1.getUniformLocation("opacity");
        gs1MultColor = partShaderStage1.getUniformLocation("multColor");
        gs1ScreenColor = partShaderStage1.getUniformLocation("screenColor");

        partShaderStage2.use();
        partShaderStage2.setUniform(partShaderStage2.getUniformLocation("emissive"), 1);
        partShaderStage2.setUniform(partShaderStage2.getUniformLocation("bumpmap"), 2);
        gs2MvpModel = partShaderStage1.getUniformLocation("mvpModel");
        gs2MvpViewProjection = partShaderStage1.getUniformLocation("mvpViewProjection");
        gs2offset = partShaderStage2.getUniformLocation("offset");
        gs2opacity = partShaderStage2.getUniformLocation("opacity");
        gs2MultColor = partShaderStage2.getUniformLocation("multColor");
        gs2ScreenColor = partShaderStage2.getUniformLocation("screenColor");
        gs2EmissionStrength = partShaderStage2.getUniformLocation("emissionStrength");

        partMaskShader.use();
        partMaskShader.setUniform(partMaskShader.getUniformLocation("albedo"), 0);
        partMaskShader.setUniform(partMaskShader.getUniformLocation("emissive"), 1);
        partMaskShader.setUniform(partMaskShader.getUniformLocation("bumpmap"), 2);
        mMvpModel = partMaskShader.getUniformLocation("mvpModel");
        mMvpViewProjection = partMaskShader.getUniformLocation("mvpViewProjection");
        mthreshold = partMaskShader.getUniformLocation("threshold");

        CoreHelper.gl.GenBuffers(1, out sVertexBuffer);
        CoreHelper.gl.GenBuffers(1, out sUVBuffer);
        CoreHelper.gl.GenBuffers(1, out sElementBuffer);
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
    public static Part inCreateSimplePart(string file, Node? parent = null)
    {
        return inCreateSimplePart(new ShallowTexture(file), parent, file);
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
    public static Part inCreateSimplePart(ShallowTexture texture, Node? parent = null, string name = "New Part")
    {
        return inCreateSimplePart(new Texture(texture), parent, name);
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
    public static Part inCreateSimplePart(Texture tex, Node? parent = null, string name = "New Part")
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

        var p = new Part(data, [tex], parent)
        {
            name = name
        };
        return p;
    }

    /// <summary>
    /// Draws a texture at the transform of the specified part
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="part"></param>
    public static unsafe void inDrawTextureAtPart(Texture texture, Part part)
    {
        float texWidthP = texture.Width / 2;
        float texHeightP = texture.Height / 2;

        // Bind the vertex array
        NodeHelper.incDrawableBindVAO();

        partShader.use();
        var temp = part.Transform().Matrix.Multiply(new Vector4(1, 1, 1, 1));
        partShader.setUniform(mvpModel,
            Matrix4x4.CreateTranslation(new Vector3(temp.X, temp.Y, temp.Z))
        );
        partShader.setUniform(mvpViewProjection,
            CoreHelper. inCamera.Matrix()
        );
        partShader.setUniform(gopacity, part.opacity);
        partShader.setUniform(gMultColor, part.tint);
        partShader.setUniform(gScreenColor, part.screenTint);

        // Bind the texture
        texture.Bind();

        // Enable points array
        CoreHelper.gl.EnableVertexAttribArray(0);
        CoreHelper.gl.BindBuffer(GlApi. GL_ARRAY_BUFFER, sVertexBuffer);
        float[] temp1 = 
        [
            -texWidthP, -texHeightP,
            texWidthP, -texHeightP,
            -texWidthP, texHeightP,
            texWidthP, texHeightP,
        ];
        fixed (void * ptr = temp1)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        CoreHelper.gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        CoreHelper.gl.EnableVertexAttribArray(1); // uvs
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sUVBuffer);
        temp1 = 
        [
            0, 0,
            1, 0,
            0, 1,
            1, 1,
        ];
        fixed (void* ptr = temp1)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        CoreHelper.gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind element array and draw our mesh
        CoreHelper.gl.BindBuffer(GlApi.GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        ushort[] temp2 = 
        [
            0, 1, 2,
            2, 1, 3
        ];
        fixed (void* ptr = temp2)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ELEMENT_ARRAY_BUFFER, 6 * sizeof(ushort), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
        CoreHelper.gl.DrawElements(GlApi.GL_TRIANGLES, 6, GlApi.GL_UNSIGNED_SHORT, 0);

        // Disable the vertex attribs after use
        CoreHelper.gl.DisableVertexAttribArray(0);
        CoreHelper.gl.DisableVertexAttribArray(1);
    }

    public static void inDrawTextureAtPosition(Texture texture, Vector2 position, float opacity = 1)
    {
        inDrawTextureAtPosition(texture, position, opacity, new(1), new(0));
    }

    /// <summary>
    /// Draws a texture at the transform of the specified part
    /// </summary>
    /// <param name="texture"></param>
    /// <param name="position"></param>
    /// <param name="opacity"></param>
    /// <param name="color"></param>
    /// <param name="screenColor"></param>
    public static void inDrawTextureAtPosition(Texture texture, Vector2 position, float opacity, Vector3 color, Vector3 screenColor)
    {
        float texWidthP = texture.Width / 2;
        float texHeightP = texture.Height / 2;

        // Bind the vertex array
        NodeHelper.incDrawableBindVAO();

        partShader.use();
        partShader.setUniform(mvpModel,
            Matrix4x4.CreateScale(1, 1, 1) * Matrix4x4.CreateTranslation(new Vector3(position, 0))
        );
        partShader.setUniform(mvpViewProjection,
            CoreHelper.inCamera.Matrix()
        );
        partShader.setUniform(gopacity, opacity);
        partShader.setUniform(gMultColor, color);
        partShader.setUniform(gScreenColor, screenColor);

        // Bind the texture
        texture.Bind();

        // Enable points array
        CoreHelper.gl.EnableVertexAttribArray(0);
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, sVertexBuffer);
        float[] temp = 
        [
            -texWidthP, -texHeightP,
            texWidthP, -texHeightP,
            -texWidthP, texHeightP,
            texWidthP, texHeightP,
        ];
        CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, 4 * Marshal.SizeOf<Vector2>(), , GL_STATIC_DRAW);
        glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Enable UVs array
        glEnableVertexAttribArray(1); // uvs
        glBindBuffer(GL_ARRAY_BUFFER, sUVBuffer);
        glBufferData(GL_ARRAY_BUFFER, 4 * vec2.sizeof, (cast(float[])[
            0, 0,
            1, 0,
            0, 1,
            1, 1,
    
        ]).ptr, GL_STATIC_DRAW);
        glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Bind element array and draw our mesh
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, 6 * ushort.sizeof, (cast(ushort[])[
            0u, 1u, 2u,
            2u, 1u, 3u
        ]).ptr, GL_STATIC_DRAW);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_SHORT, null);

        // Disable the vertex attribs after use
        glDisableVertexAttribArray(0);
        glDisableVertexAttribArray(1);
    }

    /**
        Draws a texture at the transform of the specified part
*/
    void inDrawTextureAtRect(Texture texture, rect area, rect uvs = rect(0, 0, 1, 1), float opacity = 1, vec3 color = vec3(1, 1, 1), vec3 screenColor = vec3(0, 0, 0), Shader s = null, Camera cam = null)
    {

        // Bind the vertex array
        incDrawableBindVAO();

        if (!s) s = partShader;
        if (!cam) cam = inGetCamera();
        s.use();
        s.setUniform(s.getUniformLocation("mvp"),
            cam.matrix *
            mat4.scaling(1, 1, 1)
        );
        s.setUniform(s.getUniformLocation("opacity"), opacity);
        s.setUniform(s.getUniformLocation("multColor"), color);
        s.setUniform(s.getUniformLocation("screenColor"), screenColor);

        // Bind the texture
        texture.bind();

        // Enable points array
        glEnableVertexAttribArray(0);
        glBindBuffer(GL_ARRAY_BUFFER, sVertexBuffer);
        glBufferData(GL_ARRAY_BUFFER, 4 * vec2.sizeof, [
            area.left, area.top,
        area.right, area.top,
        area.left, area.bottom,
        area.right, area.bottom,
    ].ptr, GL_STATIC_DRAW);
        glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Enable UVs array
        glEnableVertexAttribArray(1); // uvs
        glBindBuffer(GL_ARRAY_BUFFER, sUVBuffer);
        glBufferData(GL_ARRAY_BUFFER, 4 * vec2.sizeof, (cast(float[])[
            uvs.x, uvs.y,
            uvs.width, uvs.y,
            uvs.x, uvs.height,
            uvs.width, uvs.height,
    
        ]).ptr, GL_STATIC_DRAW);
        glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Bind element array and draw our mesh
        glBindBuffer(GL_ELEMENT_ARRAY_BUFFER, sElementBuffer);
        glBufferData(GL_ELEMENT_ARRAY_BUFFER, 6 * ushort.sizeof, (cast(ushort[])[
            0u, 1u, 2u,
            2u, 1u, 3u
        ]).ptr, GL_STATIC_DRAW);
        glDrawElements(GL_TRIANGLES, 6, GL_UNSIGNED_SHORT, null);

        // Disable the vertex attribs after use
        glDisableVertexAttribArray(0);
        glDisableVertexAttribArray(1);
    }
}
