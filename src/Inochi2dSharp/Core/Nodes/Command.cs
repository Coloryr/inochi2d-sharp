using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public static class Command
{
    private static bool inAdvancedBlending;
    private static bool inAdvancedBlendingCoherent;

    public static void inSetBlendModeLegacy(BlendMode blendingMode)
    {
        switch (blendingMode)
        {

            // If the advanced blending extension is not supported, force to Normal blending
            default:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Normal:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Multiply:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Screen:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR); break;

            case BlendMode.Lighten:
                CoreHelper.gl.BlendEquation(GlApi.GL_MAX);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.ColorDodge:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.LinearDodge:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.AddGlow:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFuncSeparate(GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.Subtract:
                CoreHelper.gl.BlendEquationSeparate(GlApi.GL_FUNC_REVERSE_SUBTRACT, GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE); break;

            case BlendMode.Exclusion:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFuncSeparate(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_COLOR, GlApi.GL_ONE, GlApi.GL_ONE); break;

            case BlendMode.Inverse:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ONE_MINUS_DST_COLOR, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.DestinationIn:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_SRC_ALPHA); break;

            case BlendMode.ClipToLower:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_DST_ALPHA, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;

            case BlendMode.SliceFromLower:
                CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
                CoreHelper.gl.BlendFunc(GlApi.GL_ZERO, GlApi.GL_ONE_MINUS_SRC_ALPHA); break;
        }
    }

    public static bool inUseMultistageBlending(BlendMode blendingMode)
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
            _ => CoreHelper.gl.HasKHRBlendEquationAdvanced()
        };
    }

    public static void inInitBlending()
    {
        if (CoreHelper.gl.HasKHRBlendEquationAdvanced()) inAdvancedBlending = true;
        if (CoreHelper.gl.HasKHRBlendEquationAdvancedCoherent()) inAdvancedBlendingCoherent = true;
        if (inAdvancedBlendingCoherent) CoreHelper.gl.Enable(GlApi.GL_BLEND_ADVANCED_COHERENT_KHR);
    }

    bool inIsAdvancedBlendMode(BlendMode mode)
    {
        if (!inAdvancedBlending) return false;
        switch (mode)
        {
            case BlendMode.Multiply:
            case BlendMode.Screen:
            case BlendMode.Overlay:
            case BlendMode.Darken:
            case BlendMode.Lighten:
            case BlendMode.ColorDodge:
            case BlendMode.ColorBurn:
            case BlendMode.HardLight:
            case BlendMode.SoftLight:
            case BlendMode.Difference:
            case BlendMode.Exclusion:
                return true;

            // Fallback to legacy
            default:
                return false;
        }
    }

    void inSetBlendMode(BlendMode blendingMode, bool legacyOnly = false)
    {
        if (!inAdvancedBlending || legacyOnly) inSetBlendModeLegacy(blendingMode);
        else switch (blendingMode)
            {
                case BlendMode.Multiply: glBlendEquation(GL_MULTIPLY_KHR); break;
                case BlendMode.Screen: glBlendEquation(GL_SCREEN_KHR); break;
                case BlendMode.Overlay: glBlendEquation(GL_OVERLAY_KHR); break;
                case BlendMode.Darken: glBlendEquation(GL_DARKEN_KHR); break;
                case BlendMode.Lighten: glBlendEquation(GL_LIGHTEN_KHR); break;
                case BlendMode.ColorDodge: glBlendEquation(GL_COLORDODGE_KHR); break;
                case BlendMode.ColorBurn: glBlendEquation(GL_COLORBURN_KHR); break;
                case BlendMode.HardLight: glBlendEquation(GL_HARDLIGHT_KHR); break;
                case BlendMode.SoftLight: glBlendEquation(GL_SOFTLIGHT_KHR); break;
                case BlendMode.Difference: glBlendEquation(GL_DIFFERENCE_KHR); break;
                case BlendMode.Exclusion: glBlendEquation(GL_EXCLUSION_KHR); break;

                // Fallback to legacy
                default: inSetBlendModeLegacy(blendingMode); break;
            }
    }

    void inBlendModeBarrier(BlendMode mode)
    {
        if (inAdvancedBlending && !inAdvancedBlendingCoherent && inIsAdvancedBlendMode(mode))
            glBlendBarrierKHR();
    }
}
