using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
        CoreHelper.gl.UseProgram(shaderProgram);
    }

    public int getUniformLocation(string name)
    {
        return CoreHelper.gl.GetUniformLocation(shaderProgram, name);
    }

    public void setUniform(int uniform, bool value)
    {
        CoreHelper.gl.Uniform1(uniform, value ? 1 : 0);
    }

    public void setUniform(int uniform, int value)
    {
        CoreHelper.gl.Uniform1(uniform, value);
    }

    public void setUniform(int uniform, float value)
    {
        CoreHelper.gl.Uniform1(uniform, value);
    }

    public void setUniform(int uniform, Vector2 value)
    {
        CoreHelper.gl.Uniform2(uniform, value.X, value.Y);
    }

    public void setUniform(int uniform, Vector3 value)
    {
        CoreHelper.gl.Uniform3(uniform, value.X, value.Y, value.Z);
    }

    public void setUniform(int uniform, Vector4 value)
    {
        CoreHelper.gl.Uniform4(uniform, value.X, value.Y, value.Z, value.W);
    }

    public unsafe void setUniform(int uniform, Matrix4x4 value)
    {
        CoreHelper.gl.UniformMatrix4(uniform, 1, true, new(&value));
    }

    private void compileShaders(string vertex, string fragment)
    {
        // Compile vertex shader
        vertShader = CoreHelper.gl.CreateShader(GlApi.GL_VERTEX_SHADER);
        CoreHelper.gl.ShaderSource(vertShader, vertex);
        CoreHelper.gl.CompileShader(vertShader);
        verifyShader(vertShader);

        // Compile fragment shader
        fragShader = CoreHelper.gl.CreateShader(GlApi.GL_FRAGMENT_SHADER);
        CoreHelper.gl.ShaderSource(fragShader, fragment);
        CoreHelper.gl.CompileShader(fragShader);
        verifyShader(fragShader);

        // Attach and link them
        shaderProgram = CoreHelper.gl.CreateProgram();
        CoreHelper.gl.AttachShader(shaderProgram, vertShader);
        CoreHelper.gl.AttachShader(shaderProgram, fragShader);
        CoreHelper.gl.LinkProgram(shaderProgram);
        verifyProgram();
    }

    private void verifyShader(uint shader)
    {
        string shaderType = shader == fragShader ? "fragment" : "vertex";

        CoreHelper.gl.GetShader(shader, GlApi.GL_COMPILE_STATUS, out int compileStatus);
        if (compileStatus == 0)
        {
            // Fetch the error log
            CoreHelper.gl.GetShaderInfoLog(shader, out var log);

            throw new Exception($"Compilation error for {name}->{shaderType}:\n\n{log}");
        }
    }

    private void verifyProgram()
    {
        CoreHelper.gl.GetProgram(shaderProgram, GlApi.GL_LINK_STATUS, out var linkStatus);
        if (linkStatus == 0)
        {
            // Fetch the error log
            CoreHelper.gl.GetProgramInfoLog(shaderProgram, out var log);

            throw new Exception(log);
        }
    }

    public void Dispose()
    {
        CoreHelper.gl.DetachShader(shaderProgram, vertShader);
        CoreHelper.gl.DetachShader(shaderProgram, fragShader);
        CoreHelper.gl.DeleteProgram(shaderProgram);

        CoreHelper.gl.DeleteShader(fragShader);
        CoreHelper.gl.DeleteShader(vertShader);
    }
}
