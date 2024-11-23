using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public static class NodeHelper
{
    public const uint InInvalidUUID = uint.MaxValue;

    public static bool doGenerateBounds { get; set; }

    private static uint drawableVAO;

    private static readonly List<uint> s_takenUUIDs = [];

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

    public static bool inIsAdvancedBlendMode(BlendMode mode)
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

    public static void inSetBlendMode(BlendMode blendingMode, bool legacyOnly = false)
    {
        if (!inAdvancedBlending || legacyOnly) inSetBlendModeLegacy(blendingMode);
        else switch (blendingMode)
            {
                case BlendMode.Multiply: CoreHelper.gl.BlendEquation(GL_MULTIPLY_KHR); break;
                case BlendMode.Screen: CoreHelper.gl.BlendEquation(GL_SCREEN_KHR); break;
                case BlendMode.Overlay: CoreHelper.gl.BlendEquation(GL_OVERLAY_KHR); break;
                case BlendMode.Darken: CoreHelper.gl.BlendEquation(GL_DARKEN_KHR); break;
                case BlendMode.Lighten: CoreHelper.gl.BlendEquation(GL_LIGHTEN_KHR); break;
                case BlendMode.ColorDodge: CoreHelper.gl.BlendEquation(GL_COLORDODGE_KHR); break;
                case BlendMode.ColorBurn:  CoreHelper.gl.BlendEquation(GL_COLORBURN_KHR); break;
                case BlendMode.HardLight:  CoreHelper.gl.BlendEquation(GL_HARDLIGHT_KHR); break;
                case BlendMode.SoftLight: CoreHelper.gl.BlendEquation(GL_SOFTLIGHT_KHR); break;
                case BlendMode.Difference: CoreHelper.gl.BlendEquation(GL_DIFFERENCE_KHR); break;
                case BlendMode.Exclusion: CoreHelper.gl.BlendEquation(GL_EXCLUSION_KHR); break;

                // Fallback to legacy
                default: inSetBlendModeLegacy(blendingMode); break;
            }
    }

    public static void inBlendModeBarrier(BlendMode mode)
    {
        if (inAdvancedBlending && !inAdvancedBlendingCoherent && inIsAdvancedBlendMode(mode))
            CoreHelper.gl.BlendBarrierKHR();
    }

    /// <summary>
    /// Binds the internal vertex array for rendering
    /// </summary>
    public static void incDrawableBindVAO()
    {
        // Bind our vertex array
        CoreHelper.gl.BindVertexArray(drawableVAO);
    }

    public static void inInitDrawable()
    {
        CoreHelper.gl.GenVertexArrays(1, out drawableVAO);
    }

    /// <summary>
    /// Creates a new UUID for a node
    /// </summary>
    /// <returns></returns>
    public static uint InCreateUUID()
    {
        uint id;
        var random = new Random();
        do
        {
            // Make sure the ID is actually unique in the current context
            id = (uint)random.NextInt64(uint.MinValue, InInvalidUUID);
        }
        while (s_takenUUIDs.Contains(id));

        return id;
    }

    /// <summary>
    /// Unloads a single UUID from the internal listing, freeing it up for reuse
    /// </summary>
    /// <param name="id"></param>
    public static void InUnloadUUID(uint id)
    {
        s_takenUUIDs.Remove(id);
    }

    /// <summary>
    /// Clears all UUIDs from the internal listing
    /// </summary>
    public static void InClearUUIDs()
    {
        s_takenUUIDs.Clear();
    }

    private static readonly Dictionary<string, Func<Node?, Node>> s_typeFactories = [];
    public static Node InstantiateNode(string id, Node? parent = null)
    {
        if (s_typeFactories.TryGetValue(id, out var factory))
        {
            return factory(parent);
        }
        throw new ArgumentException($"Node type '{id}' is not registered.");
    }

    public static void RegisterNodeType<T>() where T : Node, new()
    {
        var typeId = typeof(T).GetCustomAttributes(typeof(TypeIdAttribute), false);
        if (typeId.Length > 0)
        {
            var id = ((TypeIdAttribute)typeId[0]).Id;
            s_typeFactories[id] = (Node? parent) => new T(); // Assuming T has a constructor that accepts a Node
        }
        else
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
        }
    }

    public static bool HasNodeType(string id)
    {
        return s_typeFactories.ContainsKey(id);
    }
}