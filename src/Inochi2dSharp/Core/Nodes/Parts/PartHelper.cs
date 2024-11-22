using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes.Parts;

public static class PartHelper
{
    private static Texture boundAlbedo;

    private static Shader partShader;
    private static Shader partShaderStage1;
    private static Shader partShaderStage2;
    private static Shader partMaskShader;

    /* GLSL Uniforms (Normal) */
    private static GLint mvpModel;
    private static GLint mvpViewProjection;
    private static GLint offset;
    private static GLint gopacity;
    private static GLint gMultColor;
    private static GLint gScreenColor;
    private static GLint gEmissionStrength;


    /* GLSL Uniforms (Stage 1) */
    private static GLint gs1MvpModel;
    private static GLint gs1MvpViewProjection;
    private static GLint gs1offset;
    private static GLint gs1opacity;
    private static GLint gs1MultColor;
    private static GLint gs1ScreenColor;


    /* GLSL Uniforms (Stage 2) */
    private static GLint gs2MvpModel;
    private static GLint gs2MvpViewProjection;
    private static GLint gs2offset;
    private static GLint gs2opacity;
    private static GLint gs2EmissionStrength;
    private static GLint gs2MultColor;
    private static GLint gs2ScreenColor;

    /* GLSL Uniforms (Masks) */
    private static GLint mMvpModel;
    private static GLint mMvpViewProjection;
    private static GLint mthreshold;

    private static GLuint sVertexBuffer;
    private static GLuint sUVBuffer;
    private static GLuint sElementBuffer;

    private static void inInitPart()
    {
        NodeHelper.RegisterNodeType<Part>();

        version(InDoesRender) {
            partShader = new Shader("part", import("basic/basic.vert"), import("basic/basic.frag"));
            partShaderStage1 = new Shader("part (stage 1)", import("basic/basic.vert"), import("basic/basic-stage1.frag"));
            partShaderStage2 = new Shader("part (stage 2)", import("basic/basic.vert"), import("basic/basic-stage2.frag"));
            partMaskShader = new Shader("part (mask)", import("basic/basic.vert"), import("basic/basic-mask.frag"));

            incDrawableBindVAO();

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

            glGenBuffers(1, &sVertexBuffer);
            glGenBuffers(1, &sUVBuffer);
            glGenBuffers(1, &sElementBuffer);
        }
    }

    /**
        Creates a simple part that is sized after the texture given
        part is created based on file path given.
        Supported file types are: png, tga and jpeg

        This is unoptimal for normal use and should only be used
        for real-time use when you want to add/remove parts on the fly
*/
    public static Part inCreateSimplePart(string file, Node parent = null)
    {
        return inCreateSimplePart(ShallowTexture(file), parent, file);
    }

    /**
        Creates a simple part that is sized after the texture given

        This is unoptimal for normal use and should only be used
        for real-time use when you want to add/remove parts on the fly
*/
    public static Part inCreateSimplePart(ShallowTexture texture, Node parent = null, string name = "New Part")
    {
        return inCreateSimplePart(new Texture(texture), parent, name);
    }

    /**
        Creates a simple part that is sized after the texture given

        This is unoptimal for normal use and should only be used
        for real-time use when you want to add/remove parts on the fly
*/
    public static Part inCreateSimplePart(Texture tex, Node parent = null, string name = "New Part")
    {
        MeshData data = MeshData([
            vec2(-(tex.width/2), -(tex.height/2)),
        vec2(-(tex.width/2), tex.height/2),
        vec2(tex.width/2, -(tex.height/2)),
        vec2(tex.width/2, tex.height/2),
    ],
        [
            vec2(0, 0),
        vec2(0, 1),
        vec2(1, 0),
        vec2(1, 1),
    ],
        [
            0, 1, 2,
        2, 1, 3
        ]);
        Part p = new Part(data, [tex], parent);
        p.name = name;
        return p;
    }
}
