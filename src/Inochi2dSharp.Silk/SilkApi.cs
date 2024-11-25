using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.KHR;

namespace Inochi2dSharp.Silk;

public class SilkApi(GL gl, KhrBlendEquationAdvanced khr) : GlApi
{
    public override void ActiveTexture(uint texture)
    {
        gl.ActiveTexture((GLEnum)texture);
    }

    public override void AttachShader(uint program, uint shader)
    {
        gl.AttachShader(program, shader);
    }

    public override void BindBuffer(uint target, uint buffer)
    {
        gl.BindBuffer((GLEnum)target, buffer);
    }

    public override void BindFramebuffer(uint target, uint framebuffer)
    {
        gl.BindFramebuffer((GLEnum)target, framebuffer);
    }

    public override void BindTexture(uint target, uint texture)
    {
        gl.BindTexture((GLEnum)target, texture);
    }

    public override void BindVertexArray(uint array)
    {
        gl.BindVertexArray(array);
    }

    public override void BlendBarrierKHR()
    {
        khr.BlendBarrier();
    }

    public override void BlendEquation(uint mode)
    {
        gl.BlendEquation((GLEnum)mode);
    }

    public override void BlendEquationSeparate(uint modeRGB, uint modeAlpha)
    {
        gl.BlendEquationSeparate((GLEnum)modeRGB, (GLEnum)modeAlpha);
    }

    public override void BlendFunc(uint sfactor, uint dfactor)
    {
        gl.BlendFunc((GLEnum)sfactor, (GLEnum)dfactor);
    }

    public override void BlendFuncSeparate(uint sfactorRGB, uint dfactorRGB, uint sfactorAlpha, uint dfactorAlpha)
    {
        gl.BlendFuncSeparate((GLEnum)sfactorRGB, (GLEnum)dfactorRGB, (GLEnum)sfactorAlpha, (GLEnum)dfactorAlpha);
    }

    public override void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter)
    {
        gl.BlitFramebuffer(srcX0, srcY0, srcX1, srcY1, dstX0, dstY0, dstX1, dstY1, mask, (GLEnum)filter);
    }

    public override unsafe void BufferData(uint target, int size, nint data, uint usage)
    {
        gl.BufferData((GLEnum)target, (nuint)size, (void*)data, (GLEnum)usage);
    }

    public override void Clear(uint mask)
    {
        gl.Clear(mask);
    }

    public override void ClearColor(float red, float green, float blue, float alpha)
    {
        gl.ClearColor(red, green, blue, alpha);
    }

    public override void ClearStencil(int s)
    {
        gl.ClearStencil(s);
    }

    public override void ColorMask(bool red, bool green, bool blue, bool alpha)
    {
        gl.ColorMask(red, green, blue, alpha);
    }

    public override void CompileShader(uint shaderObj)
    {
        gl.CompileShader(shaderObj);
    }

    public override uint CreateProgram()
    {
        return gl.CreateProgram();
    }

    public override uint CreateShader(uint type)
    {
        return gl.CreateShader((GLEnum)type);
    }

    public override void DeleteProgram(uint programs)
    {
        gl.DeleteProgram(programs);
    }

    public override void DeleteShader(uint shader)
    {
        gl.DeleteShader(shader);
    }

    public override void DeleteTexture(uint texture)
    {
        gl.DeleteTexture(texture);
    }

    public override void DetachShader(uint program, uint shader)
    {
        gl.DetachShader(program, shader);
    }

    public override void Disable(uint cap)
    {
        gl.Disable((GLEnum)cap);
    }

    public override void Disable(uint target, int index)
    {
        gl.Disable((GLEnum)target, (uint)index);
    }

    public override void DisableVertexAttribArray(int index)
    {
        gl.DisableVertexAttribArray((uint)index);
    }

    public override void DrawArrays(uint mode, int first, int count)
    {
        gl.DrawArrays((GLEnum)mode, first, (uint)count);
    }

    public override unsafe void DrawBuffers(int n, uint[] bufs)
    {
        fixed (void* ptr = bufs)
        {
            gl.DrawBuffers((uint)n, (GLEnum*)ptr);
        }
    }

    public override unsafe void DrawElements(uint mode, int count, uint type, int indices)
    {
        gl.DrawElements((GLEnum)mode, (uint)count, (GLEnum)type, (void*)indices);
    }

    public override void Enable(uint cap)
    {
        gl.Enable((GLEnum)cap);
    }

    public override void Enable(uint target, uint index)
    {
        gl.Enable((GLEnum)target, index);
    }

    public override void EnableVertexAttribArray(int index)
    {
        gl.EnableVertexAttribArray((uint)index);
    }

    public override void Flush()
    {
        gl.Flush();
    }

    public override void FramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level)
    {
        gl.FramebufferTexture2D((GLEnum)target, (GLEnum)attachment, (GLEnum)textarget, texture, level);
    }

    public override uint GenBuffer()
    {
        return gl.GenBuffer();
    }

    public override void GenerateMipmap(uint target)
    {
        gl.GenerateMipmap((GLEnum)target);
    }

    public override uint GenFramebuffer()
    {
        return gl.GenFramebuffer();
    }

    public override uint GenTexture()
    {
        return gl.GenTexture();
    }

    public override uint GenVertexArray()
    {
        return gl.GenVertexArray();
    }

    public override int GetError()
    {
        return (int)gl.GetError();
    }

    public override float GetFloat(uint pname)
    {
        return gl.GetFloat((GLEnum)pname);
    }

    public override int GetProgram(uint program, uint pname)
    {
        return gl.GetProgram(program, (GLEnum)pname);
    }

    public override string GetProgramInfoLog(uint program)
    {
        return gl.GetProgramInfoLog(program);
    }

    public override int GetShader(uint shader, uint pname)
    {
        return gl.GetShader(shader, (GLEnum)pname);
    }

    public override string GetShaderInfoLog(uint shader)
    {
        return gl.GetShaderInfoLog(shader);
    }

    public override unsafe void GetTexImage(uint target, int level, uint format, uint type, nint pixels)
    {
        gl.GetTexImage((GLEnum)target, level, (GLEnum)format, (GLEnum)type, (void*)pixels);
    }

    public override int GetUniformLocation(uint programObj, string name)
    {
        return gl.GetUniformLocation(programObj, name);
    }

    public override bool HasKHRBlendEquationAdvanced()
    {
        return false;
    }

    public override bool HasKHRBlendEquationAdvancedCoherent()
    {
        return false;
    }

    public override void LineWidth(float width)
    {
        gl.LineWidth(width);
    }

    public override void LinkProgram(uint program)
    {
        gl.LinkProgram(program);
    }

    public override void PixelStore(uint pname, int param)
    {
        gl.PixelStore((GLEnum)pname, param);
    }

    public override void PointSize(float size)
    {
        gl.PointSize(size);
    }

    public override void ShaderSource(uint shader, string code)
    {
        gl.ShaderSource(shader, code);
    }

    public override void StencilFunc(uint func, int arg, int mask)
    {
        gl.StencilFunc((GLEnum)func, arg, (uint)mask);
    }

    public override void StencilMask(int mask)
    {
        gl.StencilMask((uint)mask);
    }

    public override void StencilOp(uint fail, uint zfail, uint zpass)
    {
        gl.StencilOp((GLEnum)fail, (GLEnum)zfail, (GLEnum)zpass);
    }

    public override unsafe void TexImage2D(uint target, int level, uint internalformat, int width, int height, int border, uint format, uint type, nint pixels)
    {
        gl.TexImage2D((GLEnum)target, level, (int)internalformat, (uint)width, (uint)height, border, (GLEnum)format, (GLEnum)type, (void*)pixels);
    }

    public override void TexParameter(uint target, uint pname, float param)
    {
        gl.TexParameter((GLEnum)target, (GLEnum)pname, param);
    }

    public override void TexParameter(uint target, uint pname, float[] arg)
    {
        gl.TexParameter((GLEnum)target, (GLEnum)pname, arg);
    }

    public override unsafe void TexParameterI(uint target, uint pname, uint arg)
    {
        gl.TexParameterI((GLEnum)target, (GLEnum)pname, (int)arg);
    }

    public override unsafe void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, nint pixels)
    {
        gl.TexSubImage2D((GLEnum)target, level, xoffset, yoffset, (uint)width, (uint)height, (GLEnum)format, (GLEnum)type, (void*)pixels);
    }

    public override void Uniform1(int location, int v0)
    {
        gl.Uniform1(location, v0);
    }

    public override void Uniform1(int location, float v0)
    {
        gl.Uniform1(location, v0);
    }

    public override void Uniform2(int location, float v0, float v1)
    {
        gl.Uniform2(location, v0, v1);
    }

    public override void Uniform3(int location, float v0, float v1, float v2)
    {
        gl.Uniform3(location, v0, v1, v2);
    }

    public override void Uniform4(int location, float v0, float v1, float v2, float v3)
    {
        gl.Uniform4(location, v0, v1, v2, v3);
    }

    public override unsafe void UniformMatrix4(int location, int count, bool transpose, nint value)
    {
        gl.UniformMatrix4(location, (uint)count, transpose, (float*)value);
    }

    public override void UseProgram(uint program)
    {
        gl.UseProgram(program);
    }

    public override void VertexAttribPointer(int index, int size, uint type, bool normalized, int stride, nint pointer)
    {
        gl.VertexAttribPointer((uint)index, size, (GLEnum)type, normalized, (uint)stride, pointer);
    }

    public override void Viewport(int x, int y, int width, int height)
    {
        gl.Viewport(x, y, (uint)width, (uint)height);
    }
}
