using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp.Core.Nodes.Composite;

public static class CompositeHelper
{
    private static Shader cShader;
    private static Shader cShaderMask;

    private static int gopacity;
    private static int gMultColor;
    private static int gScreenColor;

    private static int mthreshold;
    private static int mopacity;

    public static void inInitComposite()
    {
        NodeHelper.RegisterNodeType<Composite>();

        cShader = new Shader("composite",
            Integration.inCompositeVert,
            Integration.inCompositeFrag
        );

        cShader.use();
        gopacity = cShader.getUniformLocation("opacity");
        gMultColor = cShader.getUniformLocation("multColor");
        gScreenColor = cShader.getUniformLocation("screenColor");
        cShader.setUniform(cShader.getUniformLocation("albedo"), 0);
        cShader.setUniform(cShader.getUniformLocation("emissive"), 1);
        cShader.setUniform(cShader.getUniformLocation("bumpmap"), 2);

        cShaderMask = new Shader("composite (mask)",
            Integration.inCompositeVert,
            Integration.inCompositeMaskFrag
        );
        cShaderMask.use();
        mthreshold = cShader.getUniformLocation("threshold");
        mopacity = cShader.getUniformLocation("opacity");
    }
}
