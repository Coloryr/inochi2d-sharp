namespace Inochi2dSharp.Core;

public class PostProcessingShader
{
    private readonly Dictionary<string, int> _uniformCache = [];

    public Shader Shader;

    public PostProcessingShader(Shader shader)
    {
        Shader = shader;

        shader.Use();
        shader.SetUniform(shader.GetUniformLocation("albedo"), 0);
        shader.SetUniform(shader.GetUniformLocation("emissive"), 1);
        shader.SetUniform(shader.GetUniformLocation("bumpmap"), 2);
    }

    /// <summary>
    /// Gets the location of the specified uniform
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int GetUniform(string name)
    {
        if (HasUniform(name)) return _uniformCache[name];
        int element = Shader.GetUniformLocation(name);
        _uniformCache[name] = element;
        return element;
    }

    /// <summary>
    /// Returns true if the uniform is present in the shader cache 
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public bool HasUniform(string name)
    {
        return _uniformCache.ContainsKey(name);
    }
}
