using Inochi2dSharp.Shaders;

namespace Inochi2dSharp.Core.Nodes.Composites;

public static class CompositeHelper
{
    public static Shader CShader;
    public static Shader CShaderMask;

    public static int Gopacity;
    public static int GMultColor;
    public static int GScreenColor;

    public static int Mthreshold;
    public static int Mopacity;

    public static void inInitComposite()
    {
        NodeHelper.RegisterNodeType<Composite>();

        CShader = new Shader("composite",
            Integration.inCompositeVert,
            Integration.inCompositeFrag
        );

        CShader.use();
        Gopacity = CShader.getUniformLocation("opacity");
        GMultColor = CShader.getUniformLocation("multColor");
        GScreenColor = CShader.getUniformLocation("screenColor");
        CShader.setUniform(CShader.getUniformLocation("albedo"), 0);
        CShader.setUniform(CShader.getUniformLocation("emissive"), 1);
        CShader.setUniform(CShader.getUniformLocation("bumpmap"), 2);

        CShaderMask = new Shader("composite (mask)",
            Integration.inCompositeVert,
            Integration.inCompositeMaskFrag
        );
        CShaderMask.use();
        Mthreshold = CShader.getUniformLocation("threshold");
        Mopacity = CShader.getUniformLocation("opacity");
    }
}
