using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp;

public partial class I2dCore
{
    public const uint InInvalidUUID = uint.MaxValue;

    public bool DoGenerateBounds { get; set; }

    private uint _drawableVAO;

    private readonly List<uint> _takenUUIDs = [];

    private bool _inAdvancedBlending;
    private bool _inAdvancedBlendingCoherent;

    public void InSetBlendModeLegacy(BlendMode blendingMode)
    {
        switch (blendingMode)
        {
            // If the advanced blending extension is not supported, force to Normal blending
            default:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Normal:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Multiply:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Screen:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR); break;

            case BlendMode.Lighten:
                gl.BlendEquation(GlApi.GL_MAX);
                gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.ColorDodge:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.LinearDodge:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.AddGlow:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Subtract:
                gl.BlendEquationSeparate(GlApi.GL_FUNC_REVERSE_SUBTRACT, GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.Exclusion:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFuncSeparate(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.Inverse:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.DestinationIn:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_SRC_ALPHA); break;

            case BlendMode.ClipToLower:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_DST_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.SliceFromLower:
                gl.BlendEquation(GlApi.GL_FUNC_ADD);
                gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;
        }
    }

    public bool InUseMultistageBlending(BlendMode blendingMode)
    {
        return blendingMode switch
        {
            BlendMode.Normal or
            BlendMode.LinearDodge or
            BlendMode.AddGlow or
            BlendMode.Subtract or
            BlendMode.Inverse or
            BlendMode.DestinationIn or
            BlendMode.ClipToLower or
            BlendMode.SliceFromLower
                => false,
            _ => gl.HasKHRBlendEquationAdvanced()
        };
    }

    public void InInitBlending()
    {
        if (gl.HasKHRBlendEquationAdvanced()) _inAdvancedBlending = true;
        if (gl.HasKHRBlendEquationAdvancedCoherent()) _inAdvancedBlendingCoherent = true;
        if (_inAdvancedBlendingCoherent) gl.Enable(GlApi.GL_BLEND_ADVANCED_COHERENT_KHR);
    }

    public bool InIsAdvancedBlendMode(BlendMode mode)
    {
        if (!_inAdvancedBlending) return false;
        return mode switch
        {
            BlendMode.Multiply or BlendMode.Screen or BlendMode.Overlay or BlendMode.Darken or BlendMode.Lighten or BlendMode.ColorDodge or BlendMode.ColorBurn or BlendMode.HardLight or BlendMode.SoftLight or BlendMode.Difference or BlendMode.Exclusion => true,
            // Fallback to legacy
            _ => false,
        };
    }

    public void InSetBlendMode(BlendMode blendingMode, bool legacyOnly = false)
    {
        if (!_inAdvancedBlending || legacyOnly) InSetBlendModeLegacy(blendingMode);
        else
        {
            switch (blendingMode)
            {
                case BlendMode.Multiply: gl.BlendEquation(GlApi.GL_MULTIPLY_KHR); break;
                case BlendMode.Screen: gl.BlendEquation(GlApi.GL_SCREEN_KHR); break;
                case BlendMode.Overlay: gl.BlendEquation(GlApi.GL_OVERLAY_KHR); break;
                case BlendMode.Darken: gl.BlendEquation(GlApi.GL_DARKEN_KHR); break;
                case BlendMode.Lighten: gl.BlendEquation(GlApi.GL_LIGHTEN_KHR); break;
                case BlendMode.ColorDodge: gl.BlendEquation(GlApi.GL_COLORDODGE_KHR); break;
                case BlendMode.ColorBurn: gl.BlendEquation(GlApi.GL_COLORBURN_KHR); break;
                case BlendMode.HardLight: gl.BlendEquation(GlApi.GL_HARDLIGHT_KHR); break;
                case BlendMode.SoftLight: gl.BlendEquation(GlApi.GL_SOFTLIGHT_KHR); break;
                case BlendMode.Difference: gl.BlendEquation(GlApi.GL_DIFFERENCE_KHR); break;
                case BlendMode.Exclusion: gl.BlendEquation(GlApi.GL_EXCLUSION_KHR); break;

                // Fallback to legacy
                default: InSetBlendModeLegacy(blendingMode); break;
            }
        }
    }

    public void InBlendModeBarrier(BlendMode mode)
    {
        if (_inAdvancedBlending && !_inAdvancedBlendingCoherent && InIsAdvancedBlendMode(mode))
            gl.BlendBarrierKHR();
    }

    /// <summary>
    /// Binds the internal vertex array for rendering
    /// </summary>
    public void IncDrawableBindVAO()
    {
        // Bind our vertex array
        gl.BindVertexArray(_drawableVAO);
    }

    public void InInitDrawable()
    {
        gl.GenVertexArrays(1, out _drawableVAO);
    }

    /// <summary>
    /// Creates a new UUID for a node
    /// </summary>
    /// <returns></returns>
    public uint InCreateUUID()
    {
        uint id;
        var random = new Random();
        do
        {
            // Make sure the ID is actually unique in the current context
            id = (uint)random.NextInt64(uint.MinValue, InInvalidUUID);
        }
        while (_takenUUIDs.Contains(id));

        return id;
    }

    /// <summary>
    /// Unloads a single UUID from the internal listing, freeing it up for reuse
    /// </summary>
    /// <param name="id"></param>
    public void InUnloadUUID(uint id)
    {
        _takenUUIDs.Remove(id);
    }

    /// <summary>
    /// Clears all UUIDs from the internal listing
    /// </summary>
    public void InClearUUIDs()
    {
        _takenUUIDs.Clear();
    }

    /// <summary>
    /// Begins a mask
    /// 
    /// This causes the next draw calls until inBeginMaskContent/inBeginDodgeContent or inEndMask
    /// to be written to the current mask.
    /// 
    /// This also clears whatever old mask there was.
    /// </summary>
    /// <param name="hasMasks"></param>
    public void InBeginMask(bool hasMasks)
    {
        // Enable and clear the stencil buffer so we can write our mask to it
        gl.Enable(GlApi.GL_STENCIL_TEST);
        gl.ClearStencil(hasMasks ? 0 : 1);
        gl.Clear(GlApi.GL_STENCIL_BUFFER_BIT);
    }

    /// <summary>
    /// End masking
    /// 
    /// Once masking is ended content will no longer be masked by the defined mask.
    /// </summary>
    public void InEndMask()
    {
        // We're done stencil testing, disable it again so that we don't accidentally mask more stuff out
        gl.StencilMask(0xFF);
        gl.StencilFunc(GlApi.GL_ALWAYS, 1, 0xFF);
        gl.Disable(GlApi.GL_STENCIL_TEST);
    }

    /// <summary>
    /// Starts masking content
    /// 
    /// NOTE: This have to be run within a inBeginMask and inEndMask block!
    /// </summary>
    public void InBeginMaskContent()
    {
        gl.StencilFunc(GlApi.GL_EQUAL, 1, 0xFF);
        gl.StencilMask(0x00);
    }
}