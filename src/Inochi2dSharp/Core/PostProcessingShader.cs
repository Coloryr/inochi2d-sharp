namespace Inochi2dSharp.Core;

public class PostProcessingShader
{
    private Dictionary<string, int> uniformCache = [];

    public Shader shader;

    public PostProcessingShader(Shader shader)
    {
        this.shader = shader;

        shader.use();
        shader.setUniform(shader.getUniformLocation("albedo"), 0);
        shader.setUniform(shader.getUniformLocation("emissive"), 1);
        shader.setUniform(shader.getUniformLocation("bumpmap"), 2);
    }

    /// <summary>
    /// Gets the location of the specified uniform
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int getUniform(string name)
    {
        if (hasUniform(name)) return uniformCache[name];
        int element = shader.getUniformLocation(name);
        uniformCache[name] = element;
        return element;
    }

    /// <summary>
    /// Returns true if the uniform is present in the shader cache 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool hasUniform(string name)
    {
        return uniformCache.ContainsKey(name);
    }
}
