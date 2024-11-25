namespace Inochi2dSharp;

public abstract class GlApi
{
    public const uint GL_POINTS = 0x0000;
    public const uint GL_LINES = 0x0001;
    public const uint GL_ZERO = 0;
    public const uint GL_ONE = 1;
    public const uint GL_TRIANGLES = 0x0004;
    public const uint GL_DEPTH_BUFFER_BIT = 0x0100;
    public const uint GL_EQUAL = 0x0202;
    public const uint GL_ALWAYS = 0x0207;
    public const uint GL_STENCIL_BUFFER_BIT = 0x0400;
    public const uint GL_STENCIL_TEST = 0x0B90;
    public const uint GL_UNPACK_ALIGNMENT = 0x0CF5;
    public const uint GL_PACK_ALIGNMENT = 0x0D05;
    public const uint GL_TEXTURE_BORDER_COLOR = 0x1004;
    public const uint GL_UNSIGNED_BYTE = 0x1401;
    public const uint GL_FLOAT = 0x1406;
    public const uint GL_RED = 0x1903;
    public const uint GL_RGB = 0x1907;
    public const uint GL_RGBA = 0x1908;
    public const uint GL_KEEP = 0x1E00;
    public const uint GL_REPLACE = 0x1E01;
    public const uint GL_NEAREST = 0x2600;
    public const uint GL_LINEAR = 0x2601;
    public const uint GL_LINEAR_MIPMAP_LINEAR = 0x2703;
    public const uint GL_TEXTURE_MAG_FILTER = 0x2800;
    public const uint GL_TEXTURE_MIN_FILTER = 0x2801;
    public const uint GL_TEXTURE_WRAP_S = 0x2802;
    public const uint GL_TEXTURE_WRAP_T = 0x2803;
    public const uint GL_REPEAT = 0x2901;
    public const uint GL_ONE_MINUS_SRC_COLOR = 0x0301;
    public const uint GL_SRC_ALPHA = 0x0302;
    public const uint GL_ONE_MINUS_SRC_ALPHA = 0x0303;
    public const uint GL_DST_ALPHA = 0x0304;
    public const uint GL_DST_COLOR = 0x0306;
    public const uint GL_ONE_MINUS_DST_COLOR = 0x0307;
    public const uint GL_LINE_SMOOTH = 0x0B20;
    public const uint GL_CULL_FACE = 0x0B44;
    public const uint GL_DEPTH_TEST = 0x0B71;
    public const uint GL_BLEND = 0x0BE2;
    public const uint GL_TEXTURE_2D = 0x0DE1;
    public const uint GL_UNSIGNED_SHORT = 0x1403;
    public const uint GL_COLOR_BUFFER_BIT = 0x4000;
    public const uint GL_FUNC_ADD = 0x8006;
    public const uint GL_MAX = 0x8008;
    public const uint GL_FUNC_REVERSE_SUBTRACT = 0x800B;
    public const uint GL_CLAMP_TO_BORDER = 0x812D;
    public const uint GL_DEPTH_STENCIL_ATTACHMENT = 0x821A;
    public const uint GL_RG = 0x8227;
    public const uint GL_MIRRORED_REPEAT = 0x8370;
    public const uint GL_TEXTURE0 = 0x84C0;
    public const uint GL_TEXTURE1 = 0x84C1;
    public const uint GL_TEXTURE2 = 0x84C2;
    public const uint GL_DEPTH_STENCIL = 0x84F9;
    public const uint GL_UNSIGNED_INT_24_8 = 0x84FA;
    public const uint GL_TEXTURE_MAX_ANISOTROPY = 0x84FE;
    public const uint GL_MAX_TEXTURE_MAX_ANISOTROPY = 0x84FF;
    public const uint GL_STATIC_DRAW = 0x88E4;
    public const uint GL_DYNAMIC_DRAW = 0x88E8;
    public const uint GL_ARRAY_BUFFER = 0x8892;
    public const uint GL_DEPTH24_STENCIL8 = 0x88F0;
    public const uint GL_ELEMENT_ARRAY_BUFFER = 0x8893;
    public const uint GL_FRAGMENT_SHADER = 0x8B30;
    public const uint GL_VERTEX_SHADER = 0x8B31;
    public const uint GL_COMPILE_STATUS = 0x8B81;
    public const uint GL_LINK_STATUS = 0x8B82;
    public const uint GL_READ_FRAMEBUFFER = 0x8CA8;
    public const uint GL_DRAW_FRAMEBUFFER = 0x8CA9;
    public const uint GL_COLOR_ATTACHMENT0 = 0x8CE0;
    public const uint GL_COLOR_ATTACHMENT1 = 0x8CE1;
    public const uint GL_COLOR_ATTACHMENT2 = 0x8CE2;
    public const uint GL_FRAMEBUFFER = 0x8D40;
    public const uint GL_BLEND_ADVANCED_COHERENT_KHR = 0x9285;
    public const uint GL_MULTIPLY_KHR = 0x9294;
    public const uint GL_SCREEN_KHR = 0x9295;
    public const uint GL_OVERLAY_KHR = 0x9296;
    public const uint GL_DARKEN_KHR = 0x9297;
    public const uint GL_LIGHTEN_KHR = 0x9298;
    public const uint GL_COLORDODGE_KHR = 0x9299;
    public const uint GL_COLORBURN_KHR = 0x929A;
    public const uint GL_HARDLIGHT_KHR = 0x929B;
    public const uint GL_SOFTLIGHT_KHR = 0x929C;
    public const uint GL_DIFFERENCE_KHR = 0x929E;
    public const uint GL_EXCLUSION_KHR = 0x92A0;

    public abstract uint CreateShader(uint type);
    public abstract void ShaderSource(uint shader, string code);
    public abstract void CompileShader(uint shaderObj);
    public abstract int GetShader(uint shader, uint pname);
    public abstract string GetShaderInfoLog(uint shader);
    public abstract int GetProgram(uint program, uint pname);
    public abstract string GetProgramInfoLog(uint program);
    public abstract uint CreateProgram();
    public abstract void AttachShader(uint program, uint shader);
    public abstract void LinkProgram(uint program);
    public abstract void DetachShader(uint program, uint shader);
    public abstract void DeleteProgram(uint programs);
    public abstract void DeleteShader(uint shader);
    public abstract void UseProgram(uint program);
    public abstract int GetUniformLocation(uint programObj, string name);
    /// <summary>
    /// Uniform1i
    /// </summary>
    /// <param name="location"></param>
    /// <param name="v0"></param>
    public abstract void Uniform1(int location, int v0);
    /// <summary>
    /// Uniform1f
    /// </summary>
    /// <param name="location"></param>
    /// <param name="v0"></param>
    public abstract void Uniform1(int location, float v0);
    /// <summary>
    /// Uniform2f
    /// </summary>
    /// <param name="location"></param>
    /// <param name="v0"></param>
    /// <param name="v1"></param>
    public abstract void Uniform2(int location, float v0, float v1);
    /// <summary>
    /// Uniform3f
    /// </summary>
    /// <param name="location"></param>
    /// <param name="v0"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    public abstract void Uniform3(int location, float v0, float v1, float v2);
    /// <summary>
    /// Uniform4f
    /// </summary>
    /// <param name="location"></param>
    /// <param name="v0"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="v3"></param>
    public abstract void Uniform4(int location, float v0, float v1, float v2, float v3);
    /// <summary>
    /// UniformMatrix4fv
    /// </summary>
    /// <param name="location"></param>
    /// <param name="count"></param>
    /// <param name="transpose"></param>
    /// <param name="value"></param>
    public abstract void UniformMatrix4(int location, int count, bool transpose, nint value);
    public abstract void Viewport(int x, int y, int width, int height);
    public abstract void BindVertexArray(uint array);
    public abstract void Disable(uint cap);
    public abstract void Enable(uint cap);
    public abstract void BlendEquation(uint mode);
    public abstract void BlendFunc(uint sfactor, uint dfactor);
    public abstract void ActiveTexture(uint texture);
    public abstract void BindTexture(uint target, uint texture);
    public abstract void EnableVertexAttribArray(int index);
    public abstract void VertexAttribPointer(int index, int size, uint type, bool normalized, int stride, nint pointer);
    public abstract void DrawArrays(uint mode, int first, int count);
    public abstract void DisableVertexAttribArray(int index);
    public abstract void BlendFuncSeparate(uint sfactorRGB, uint dfactorRGB, uint sfactorAlpha, uint dfactorAlpha);
    public abstract void BlendEquationSeparate(uint modeRGB, uint modeAlpha);
    public abstract bool HasKHRBlendEquationAdvanced();
    public abstract bool HasKHRBlendEquationAdvancedCoherent();
    public abstract void BindBuffer(uint target, uint buffer);
    public abstract void BufferData(uint target, int size, nint data, uint usage);
    public abstract uint GenBuffer();
    public abstract void DrawElements(uint mode, int count, uint type, int indices);
    public abstract uint GenVertexArray();
    public abstract uint GenFramebuffer();
    public abstract uint GenTexture();
    public abstract void BindFramebuffer(uint target, uint framebuffer);
    public abstract void FramebufferTexture2D(uint target, uint attachment, uint textarget, uint texture, int level);
    /// <summary>
    /// Enablei
    /// </summary>
    /// <param name="target"></param>
    /// <param name="index"></param>
    public abstract void Enable(uint target, uint index);
    public abstract void DrawBuffers(int n, uint[] bufs);
    public abstract void ClearColor(float red, float green, float blue, float alpha);
    public abstract void Clear(uint mask);
    public abstract void Flush();
    /// <summary>
    /// Disablei
    /// </summary>
    /// <param name="target"></param>
    /// <param name="index"></param>
    public abstract void Disable(uint target, int index);
    public abstract void GenerateMipmap(uint target);
    public abstract void BlitFramebuffer(int srcX0, int srcY0, int srcX1, int srcY1, int dstX0, int dstY0, int dstX1, int dstY1, uint mask, uint filter);
    public abstract void TexImage2D(uint target, int level, uint internalformat, int width, int height, int border, uint format, uint type, nint pixels);
    public abstract void TexParameterI(uint target, uint pname, uint arg);
    public abstract void GetTexImage(uint target, int level, uint format, uint type, nint pixels);
    public abstract void PointSize(float size);
    public abstract void LineWidth(float width);
    public abstract void DeleteTexture(uint textures);
    /// <summary>
    /// TexParameterf
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pname"></param>
    /// <param name="param"></param>
    public abstract void TexParameter(uint target, uint pname, float param);
    /// <summary>
    /// PixelStorei
    /// </summary>
    /// <param name="pname"></param>
    /// <param name="param"></param>
    public abstract void PixelStore(uint pname, int param);
    /// <summary>
    /// GetFloatv
    /// </summary>
    /// <param name="pname"></param>
    /// <param name="index"></param>
    /// <param name="res"></param>
    public abstract float GetFloat(uint pname);
    /// <summary>
    /// TexParameterfv
    /// </summary>
    /// <param name="target"></param>
    /// <param name="pname"></param>
    /// <param name="arg"></param>
    public abstract void TexParameter(uint target, uint pname, float[] arg);
    public abstract void TexSubImage2D(uint target, int level, int xoffset, int yoffset, int width, int height, uint format, uint type, nint pixels);
    public abstract void ClearStencil(int s);
    public abstract void StencilMask(int mask);
    public abstract void StencilFunc(uint func, int arg, int mask);
    public abstract void BlendBarrierKHR();
    public abstract void ColorMask(bool red, bool green, bool blue, bool alpha);
    public abstract void StencilOp(uint fail, uint zfail, uint zpass);
    public abstract int GetError();
}
