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
        CShader = new Shader(this, "composite",
            ShaderCode.CompositeVert,
            ShaderCode.CompositeFrag
        );

        CShader.Use();
        Gopacity = CShader.GetUniformLocation("opacity");
        GMultColor = CShader.GetUniformLocation("multColor");
        GScreenColor = CShader.GetUniformLocation("screenColor");
        CShader.SetUniform(CShader.GetUniformLocation("albedo"), 0);
        CShader.SetUniform(CShader.GetUniformLocation("emissive"), 1);
        CShader.SetUniform(CShader.GetUniformLocation("bumpmap"), 2);

        CShaderMask = new Shader(this, "composite (mask)",
            ShaderCode.CompositeVert,
            ShaderCode.CompositeMaskFrag
        );
        CShaderMask.Use();
        Mthreshold = CShader.GetUniformLocation("threshold");
        Mopacity = CShader.GetUniformLocation("opacity");
    }
}
