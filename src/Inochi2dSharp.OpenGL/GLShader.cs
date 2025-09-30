using System.Numerics;

namespace Inochi2dSharp.OpenGL;

public record GLShader : IDisposable
{
    private readonly GlApi _gl;

    private uint _prog = uint.MaxValue;

    public unsafe GLShader(GlApi gl, string vertex, string fragment)
    {
        _gl = gl;
        // Compile vertex shader
        var vertShader = gl.CreateShader(GlApi.GL_VERTEX_SHADER);
        gl.ShaderSource(vertShader, vertex);
        gl.CompileShader(vertShader);
        VerifyShader(false, vertShader);

        // Compile fragment shader
        var fragShader = gl.CreateShader(GlApi.GL_FRAGMENT_SHADER);
        gl.ShaderSource(fragShader, fragment);
        gl.CompileShader(fragShader);
        VerifyShader(true, fragShader);

        // Attach and link them
        _prog = gl.CreateProgram();
        gl.AttachShader(_prog, vertShader);
        gl.AttachShader(_prog, fragShader);
        gl.LinkProgram(_prog);
        VerifyProgram();

        gl.DeleteShader(vertShader);
        gl.DeleteShader(fragShader);
    }

    private void VerifyShader(bool frag, uint shader)
    {
        string shaderType = frag ? "fragment" : "vertex";

        var compileStatus = _gl.GetShader(shader, GlApi.GL_COMPILE_STATUS);
        if (compileStatus == 0)
        {
            // Fetch the error log
            var log = _gl.GetShaderInfoLog(shader);

            throw new Exception($"Compilation error for {shaderType}:\n{log}");
        }
    }

    private void VerifyProgram()
    {
        var linkStatus = _gl.GetProgram(_prog, GlApi.GL_LINK_STATUS);
        if (linkStatus == 0)
        {
            // Fetch the error log
            var log = _gl.GetProgramInfoLog(_prog);

            throw new Exception(log);
        }
    }

    /// <summary>
    /// Use the shader
    /// </summary>
    public void Use()
    {
        _gl.UseProgram(_prog);
    }

    public int GetUniformLocation(string name)
    {
        return _gl.GetUniformLocation(_prog, name);
    }

    public void SetUniform(int uniform, bool value)
    {
        _gl.Uniform1i(uniform, value ? 1 : 0);
    }

    public void SetUniform(int uniform, int value)
    {
        _gl.Uniform1i(uniform, value);
    }

    public void SetUniform(int uniform, float value)
    {
        _gl.Uniform1f(uniform, value);
    }

    public void SetUniform(int uniform, Vector2 value)
    {
        _gl.Uniform2f(uniform, value.X, value.Y);
    }

    public void SetUniform(int uniform, Vector3 value)
    {
        _gl.Uniform3f(uniform, value.X, value.Y, value.Z);
    }

    public void SetUniform(int uniform, Vector4 value)
    {
        _gl.Uniform4f(uniform, value.X, value.Y, value.Z, value.W);
    }

    public unsafe void SetUniform(int uniform, Matrix4x4 value)
    {
        _gl.UniformMatrix4fv(uniform, 1, true, new(&value));
    }

    public void Dispose()
    {
        if (_prog != uint.MinValue)
        {
            _gl.DeleteProgram(_prog);
            _prog = uint.MaxValue;
        }
    }
}
