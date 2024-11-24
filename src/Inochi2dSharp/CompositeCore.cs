using Inochi2dSharp.Core;
using Inochi2dSharp.Shaders;

namespace Inochi2dSharp;

public partial class I2dCore
{
    public Shader CShader;
    public Shader CShaderMask;

    public int Gopacity;
    public int GMultColor;
    public int GScreenColor;

    public int Mthreshold;
    public int Mopacity;

    public void InInitComposite()
    {
        CShader = new Shader("composite",
            Integration.CompositeVert,
            Integration.CompositeFrag
        );

        CShader.use();
        Gopacity = CShader.GetUniformLocation("opacity");
        GMultColor = CShader.GetUniformLocation("multColor");
        GScreenColor = CShader.GetUniformLocation("screenColor");
        CShader.setUniform(CShader.GetUniformLocation("albedo"), 0);
        CShader.setUniform(CShader.GetUniformLocation("emissive"), 1);
        CShader.setUniform(CShader.GetUniformLocation("bumpmap"), 2);

        CShaderMask = new Shader("composite (mask)",
            Integration.CompositeVert,
            Integration.CompositeMaskFrag
        );
        CShaderMask.use();
        Mthreshold = CShader.GetUniformLocation("threshold");
        Mopacity = CShader.GetUniformLocation("opacity");
    }
}
