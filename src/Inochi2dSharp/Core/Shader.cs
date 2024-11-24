using System.Numerics;

namespace Inochi2dSharp.Core;

public class Shader : IDisposable
{
    private string name;
    private uint shaderProgram;
    private uint fragShader;
    private uint vertShader;

    /// <summary>
    /// Creates a new shader object from source
    /// </summary>
    /// <param name="gl"></param>
    /// <param name="name"></param>
    /// <param name="vertex"></param>
    /// <param name="fragment"></param>
    public Shader(string name, string vertex, string fragment)
    {
        this.name = name;
        compileShaders(vertex, fragment);
    }

    /// <summary>
    /// Use the shader
    /// </summary>
    public void use()
    {
        I2dCore.gl.UseProgram(shaderProgram);
    }

    public int GetUniformLocation(string name)
    {
        return I2dCore.gl.GetUniformLocation(shaderProgram, name);
    }

    public void setUniform(int uniform, bool value)
    {
        I2dCore.gl.Uniform1(uniform, value ? 1 : 0);
    }

    public void setUniform(int uniform, int value)
    {
        I2dCore.gl.Uniform1(uniform, value);
    }

    public void setUniform(int uniform, float value)
    {
        I2dCore.gl.Uniform1(uniform, value);
    }

    public void setUniform(int uniform, Vector2 value)
    {
        I2dCore.gl.Uniform2(uniform, value.X, value.Y);
    }

    public void setUniform(int uniform, Vector3 value)
    {
        I2dCore.gl.Uniform3(uniform, value.X, value.Y, value.Z);
    }

    public void setUniform(int uniform, Vector4 value)
    {
        I2dCore.gl.Uniform4(uniform, value.X, value.Y, value.Z, value.W);
    }

    public unsafe void setUniform(int uniform, Matrix4x4 value)
    {
        I2dCore.gl.UniformMatrix4(uniform, 1, true, new(&value));
    }

    private void compileShaders(string vertex, string fragment)
    {
        // Compile vertex shader
        vertShader = I2dCore.gl.CreateShader(GlApi.GL_VERTEX_SHADER);
        I2dCore.gl.ShaderSource(vertShader, vertex);
        I2dCore.gl.CompileShader(vertShader);
        verifyShader(vertShader);

        // Compile fragment shader
        fragShader = I2dCore.gl.CreateShader(GlApi.GL_FRAGMENT_SHADER);
        I2dCore.gl.ShaderSource(fragShader, fragment);
        I2dCore.gl.CompileShader(fragShader);
        verifyShader(fragShader);

        // Attach and link them
        shaderProgram = I2dCore.gl.CreateProgram();
        I2dCore.gl.AttachShader(shaderProgram, vertShader);
        I2dCore.gl.AttachShader(shaderProgram, fragShader);
        I2dCore.gl.LinkProgram(shaderProgram);
        verifyProgram();
    }

    private void verifyShader(uint shader)
    {
        string shaderType = shader == fragShader ? "fragment" : "vertex";

        I2dCore.gl.GetShader(shader, GlApi.GL_COMPILE_STATUS, out int compileStatus);
        if (compileStatus == 0)
        {
            // Fetch the error log
            I2dCore.gl.GetShaderInfoLog(shader, out var log);

            throw new Exception($"Compilation error for {name}->{shaderType}:\n\n{log}");
        }
    }

    private void verifyProgram()
    {
        I2dCore.gl.GetProgram(shaderProgram, GlApi.GL_LINK_STATUS, out var linkStatus);
        if (linkStatus == 0)
        {
            // Fetch the error log
            I2dCore.gl.GetProgramInfoLog(shaderProgram, out var log);

            throw new Exception(log);
        }
    }

    public void Dispose()
    {
        I2dCore.gl.DetachShader(shaderProgram, vertShader);
        I2dCore.gl.DetachShader(shaderProgram, fragShader);
        I2dCore.gl.DeleteProgram(shaderProgram);

        I2dCore.gl.DeleteShader(fragShader);
        I2dCore.gl.DeleteShader(vertShader);
    }
}
