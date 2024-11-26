using System.Numerics;

namespace Inochi2dSharp.Core;

public class Shader : IDisposable
{
    private readonly string _name;

    private uint _shaderProgram;
    private uint _fragShader;
    private uint _vertShader;

    private readonly I2dCore _core;

    /// <summary>
    /// Creates a new shader object from source
    /// </summary>
    /// <param name="gl"></param>
    /// <param name="name"></param>
    /// <param name="vertex"></param>
    /// <param name="fragment"></param>
    public Shader(I2dCore core, string name, string vertex, string fragment)
    {
        _core = core;
        _name = name;
        CompileShaders(vertex, fragment);
    }

    /// <summary>
    /// Use the shader
    /// </summary>
    public void Use()
    {
        _core.gl.UseProgram(_shaderProgram);
    }

    public int GetUniformLocation(string name)
    {
        return _core.gl.GetUniformLocation(_shaderProgram, name);
    }

    public void SetUniform(int uniform, bool value)
    {
        _core.gl.Uniform1i(uniform, value ? 1 : 0);
    }

    public void SetUniform(int uniform, int value)
    {
        _core.gl.Uniform1i(uniform, value);
    }

    public void SetUniform(int uniform, float value)
    {
        _core.gl.Uniform1f(uniform, value);
    }

    public void SetUniform(int uniform, Vector2 value)
    {
        _core.gl.Uniform2f(uniform, value.X, value.Y);
    }

    public void SetUniform(int uniform, Vector3 value)
    {
        _core.gl.Uniform3f(uniform, value.X, value.Y, value.Z);
    }

    public void SetUniform(int uniform, Vector4 value)
    {
        _core.gl.Uniform4f(uniform, value.X, value.Y, value.Z, value.W);
    }

    public unsafe void SetUniform(int uniform, Matrix4x4 value)
    {
        _core.gl.UniformMatrix4fv(uniform, 1, true, new(&value));
    }

    private void CompileShaders(string vertex, string fragment)
    {
        // Compile vertex shader
        _vertShader = _core.gl.CreateShader(GlApi.GL_VERTEX_SHADER);
        _core.gl.ShaderSource(_vertShader, vertex);
        _core.gl.CompileShader(_vertShader);
        VerifyShader(_vertShader);

        // Compile fragment shader
        _fragShader = _core.gl.CreateShader(GlApi.GL_FRAGMENT_SHADER);
        _core.gl.ShaderSource(_fragShader, fragment);
        _core.gl.CompileShader(_fragShader);
        VerifyShader(_fragShader);

        // Attach and link them
        _shaderProgram = _core.gl.CreateProgram();
        _core.gl.AttachShader(_shaderProgram, _vertShader);
        _core.gl.AttachShader(_shaderProgram, _fragShader);
        _core.gl.LinkProgram(_shaderProgram);
        VerifyProgram();
    }

    private void VerifyShader(uint shader)
    {
        string shaderType = shader == _fragShader ? "fragment" : "vertex";

        var compileStatus = _core.gl.GetShader(shader, GlApi.GL_COMPILE_STATUS);
        if (compileStatus == 0)
        {
            // Fetch the error log
            var log = _core.gl.GetShaderInfoLog(shader);

            throw new Exception($"Compilation error for {_name}->{shaderType}:\n\n{log}");
        }
    }

    private void VerifyProgram()
    {
        var linkStatus = _core.gl.GetProgram(_shaderProgram, GlApi.GL_LINK_STATUS);
        if (linkStatus == 0)
        {
            // Fetch the error log
            var log = _core.gl.GetProgramInfoLog(_shaderProgram);

            throw new Exception(log);
        }
    }

    public void Dispose()
    {
        _core.gl.DetachShader(_shaderProgram, _vertShader);
        _core.gl.DetachShader(_shaderProgram, _fragShader);
        _core.gl.DeleteProgram(_shaderProgram);

        _core.gl.DeleteShader(_fragShader);
        _core.gl.DeleteShader(_vertShader);
    }
}
