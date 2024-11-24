using System.Numerics;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.Composites;

[TypeId("Composite")]
public class Composite : Node
{
    protected List<Part> SubParts = [];

    //
    //      PARAMETER OFFSETS
    //
    protected float OffsetOpacity = 1;
    protected Vector3 OffsetTint = new(0);
    protected Vector3 OffsetScreenTint = new(0);

    public bool PropagateMeshGroup = true;

    /// <summary>
    /// The blending mode
    /// </summary>
    public BlendMode BlendingMode;

    /// <summary>
    /// The opacity of the composite
    /// </summary>
    public float Opacity = 1;

    /// <summary>
    /// The threshold for rendering masks
    /// </summary>
    public float Threshold = 0.5f;

    /// <summary>
    /// Multiplicative tint color
    /// </summary>
    public Vector3 Tint = new(1, 1, 1);

    /// <summary>
    /// Screen tint color
    /// </summary>
    public Vector3 ScreenTint = new(0, 0, 0);

    /// <summary>
    /// List of masks to apply
    /// </summary>
    public List<MaskBinding> Masks = [];

    public Composite() : this(null)
    {

    }

    /// <summary>
    /// Constructs a new mask
    /// </summary>
    /// <param name="parent"></param>
    public Composite(Node? parent = null) : this(NodeHelper.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new composite
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Composite(uint uuid, Node? parent = null) : base(uuid, parent)
    {

    }

    public override bool HasParam(string key)
    {
        if (base.HasParam(key)) return true;

        return key switch
        {
            "opacity" or "tint.r" or "tint.g" or "tint.b" or "screenTint.r" or "screenTint.g" or "screenTint.b" => true,
            _ => false,
        };
    }

    public override float GetDefaultValue(string key)
    {
        // Skip our list of our parent already handled it
        float def = base.GetDefaultValue(key);
        if (!float.IsNaN(def)) return def;

        return key switch
        {
            "opacity" or "tint.r" or "tint.g" or "tint.b" => 1,
            "screenTint.r" or "screenTint.g" or "screenTint.b" => 0,
            _ => (float)0,
        };
    }

    public override bool SetValue(string key, float value)
    {
        // Skip our list of our parent already handled it
        if (base.SetValue(key, value)) return true;

        switch (key)
        {
            case "opacity":
                OffsetOpacity *= value;
                return true;
            case "tint.r":
                OffsetTint.X *= value;
                return true;
            case "tint.g":
                OffsetTint.Y *= value;
                return true;
            case "tint.b":
                OffsetTint.Z *= value;
                return true;
            case "screenTint.r":
                OffsetScreenTint.X += value;
                return true;
            case "screenTint.g":
                OffsetScreenTint.Y += value;
                return true;
            case "screenTint.b":
                OffsetScreenTint.Z += value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "opacity" => OffsetOpacity,
            "tint.r" => OffsetTint.X,
            "tint.g" => OffsetTint.Y,
            "tint.b" => OffsetTint.Z,
            "screenTint.r" => OffsetScreenTint.X,
            "screenTint.g" => OffsetScreenTint.Y,
            "screenTint.b" => OffsetScreenTint.Z,
            _ => base.GetValue(key),
        };
    }

    public bool IsMaskedBy(Drawable drawable)
    {
        foreach (var mask in Masks)
        {
            if (mask.maskSrc.UUID == drawable.UUID) return true;
        }
        return false;
    }

    public int GetMaskIdx(Drawable drawable)
    {
        if (drawable is null) return -1;
        for (int i = 0; i < Masks.Count; i++)
        {
            var mask = Masks[i];
            if (mask.maskSrc.UUID == drawable.UUID) return i;
        }
        return -1;
    }

    public int GetMaskIdx(uint uuid)
    {
        for (int i = 0; i < Masks.Count; i++)
        {
            var mask = Masks[i];
            if (mask.maskSrc.UUID == uuid) return i;
        }
        return -1;
    }

    public override void BeginUpdate()
    {
        OffsetOpacity = 1;
        OffsetTint = new(1, 1, 1);
        OffsetScreenTint = new(0, 0, 0);
        base.BeginUpdate();
    }

    public override void DrawOne()
    {
        if (!enabled) return;

        SelfSort();
        DrawContents();

        var cMasks = MaskCount();

        if (Masks.Count > 0)
        {
            NodeHelper.inBeginMask(cMasks > 0);

            foreach (var mask in Masks)
            {
                mask.maskSrc.RenderMask(mask.Mode == MaskingMode.DodgeMask);
            }

            NodeHelper.inBeginMaskContent();

            // We are the content
            DrawSelf();

            NodeHelper.inEndMask();
            return;
        }

        // No masks, draw normally
        base.DrawOne();
        DrawSelf();
    }

    public override void Draw()
    {
        if (!enabled) return;
        DrawOne();
    }

    public override void Dispose()
    {
        base.Dispose();

        var validMasks = new List<MaskBinding>();
        for (int i = 0; i < Masks.Count; i++)
        {
            if (Puppet.Find<Drawable>(Masks[i].MaskSrcUUID) is { } nMask)
            {
                Masks[i].maskSrc = nMask;
                validMasks.Add(Masks[i]);
            }
        }

        // Remove invalid masks
        Masks = validMasks;
    }

    /// <summary>
    /// Scans for parts to render
    /// </summary>
    public void ScanParts()
    {
        SubParts.Clear();
        if (Children.Count > 0)
        {
            var temp = Children[0].Parent;
            ScanPartsRecurse(temp!);
            Children[0].Parent = temp;
        }
    }

    private void DrawContents()
    {
        // Optimization: Nothing to be drawn, skip context switching
        if (SubParts.Count == 0) return;

        CoreHelper.inBeginComposite();

        foreach (var child in SubParts)
        {
            child.DrawOne();
        }

        CoreHelper.inEndComposite();
    }

    /// <summary>
    /// RENDERING
    /// </summary>
    private void DrawSelf()
    {
        if (SubParts.Count == 0) return;

        CoreHelper.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

        CompositeHelper.CShader.use();
        CompositeHelper.CShader.setUniform(CompositeHelper.Gopacity, float.Clamp(OffsetOpacity * Opacity, 0, 1));
        CoreHelper.incCompositePrepareRender();

        var clampedColor = Tint;
        if (!float.IsNaN(OffsetTint.X)) clampedColor.X = float.Clamp(Tint.X * OffsetTint.X, 0, 1);
        if (!float.IsNaN(OffsetTint.Y)) clampedColor.Y = float.Clamp(Tint.Y * OffsetTint.Y, 0, 1);
        if (!float.IsNaN(OffsetTint.Z)) clampedColor.Z = float.Clamp(Tint.Z * OffsetTint.Z, 0, 1);
        CompositeHelper.CShader.setUniform(CompositeHelper.GMultColor, clampedColor);

        clampedColor = ScreenTint;
        if (!float.IsNaN(OffsetScreenTint.X)) clampedColor.X = float.Clamp(ScreenTint.X + OffsetScreenTint.X, 0, 1);
        if (!float.IsNaN(OffsetScreenTint.Y)) clampedColor.Y = float.Clamp(ScreenTint.Y + OffsetScreenTint.Y, 0, 1);
        if (!float.IsNaN(OffsetScreenTint.Z)) clampedColor.Z = float.Clamp(ScreenTint.Z + OffsetScreenTint.Z, 0, 1);
        CompositeHelper.CShader.setUniform(CompositeHelper.GScreenColor, clampedColor);
        NodeHelper.inSetBlendMode(BlendingMode, true);

        // Bind the texture
        CoreHelper.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
    }

    private void SelfSort()
    {
        SubParts.Sort((a, b) => a.ZSort.CompareTo(b.ZSort));
    }

    private void ScanPartsRecurse(Node node)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Do the main check
        if (node is Part part)
        {
            SubParts.Add(part);
            foreach (var child in part.Children)
            {
                ScanPartsRecurse(child);
            }

        }
        else
        {

            // Non-part nodes just need to be recursed through,
            // they don't draw anything.
            foreach (var child in node.Children)
            {
                ScanPartsRecurse(child);
            }
        }
    }

    protected void RenderMask()
    {
        CoreHelper.inBeginComposite();

        // Enable writing to stencil buffer and disable writing to color buffer
        CoreHelper.gl.ColorMask(false, false, false, false);
        CoreHelper.gl.StencilOp(GlApi.GL_KEEP, GlApi.GL_KEEP, GlApi.GL_REPLACE);
        CoreHelper.gl.StencilFunc(GlApi.GL_ALWAYS, 1, 0xFF);
        CoreHelper.gl.StencilMask(0xFF);

        foreach (Part child in SubParts)
        {
            child.DrawOneDirect(true);
        }

        // Disable writing to stencil buffer and enable writing to color buffer
        CoreHelper.gl.ColorMask(true, true, true, true);
        CoreHelper.inEndComposite();

        CompositeHelper.CShaderMask.use();
        CompositeHelper.CShaderMask.setUniform(CompositeHelper.Mopacity, Opacity);
        CompositeHelper.CShaderMask.setUniform(CompositeHelper.Mthreshold, Threshold);
        CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

        CoreHelper.gl.ActiveTexture(GlApi.GL_TEXTURE0);
        CoreHelper.gl.BindTexture(GlApi.GL_TEXTURE_2D, CoreHelper.inGetCompositeImage());
        CoreHelper.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
    }

    protected override void SerializeSelfImpl(JObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);

        serializer.Add("blend_mode", BlendingMode.ToString());
        serializer.Add("tint", Tint.ToToken());
        serializer.Add("screenTint", ScreenTint.ToToken());
        serializer.Add("mask_threshold", Threshold);
        serializer.Add("opacity", Opacity);
        serializer.Add("propagate_meshgroup", PropagateMeshGroup);

        if (Masks.Count > 0)
        {
            var list = new JArray();
            foreach (var m in Masks)
            {
                var obj = new JObject(m);
                list.Add(obj);
            }
            serializer.Add("masks", list);
        }
    }

    public override void Deserialize(JObject data)
    {
        // Older models may not have these tags
        var temp = data["opacity"];
        if (temp != null)
        {
            Opacity = (float)temp;
        }

        temp = data["mask_threshold"];
        if (temp != null)
        {
            Threshold = (float)temp;
        }

        temp = data["tint"];
        if (temp != null)
        {
            Tint = temp.ToVector3();
        }

        temp = data["screenTint"];
        if (temp != null)
        {
            ScreenTint = temp.ToVector3();
        }

        temp = data["blend_mode"];
        if (temp != null)
        {
            BlendingMode = Enum.Parse<BlendMode>(temp.ToString());
        }

        temp = data["propagate_meshgroup"];
        if (temp != null)
        {
            PropagateMeshGroup = (bool)temp;
        }
        else
        {
            PropagateMeshGroup = false;
        }

        temp = data["masks"];
        if (temp is JArray array)
        {
            foreach (JObject item in array.Cast<JObject>())
            {
                Masks.Add(item.ToObject<MaskBinding>()!);
            }
        }

        base.Deserialize(data);
    }

    public override string TypeId()
    {
        return "Composite";
    }

    // TODO: Cache this
    protected int MaskCount()
    {
        int c = -0;
        foreach (var m in Masks) if (m.Mode == MaskingMode.Mask) c++;
        return c;
    }

    protected int DodgeCount()
    {
        int c = 0;
        foreach (var m in Masks) if (m.Mode == MaskingMode.DodgeMask) c++;
        return c;
    }

    public override void PreProcess()
    {
        if (!PropagateMeshGroup)
            ((Node)this).PreProcess();
    }

    public override void PostProcess()
    {
        if (!PropagateMeshGroup)
            ((Node)this).PostProcess();
    }
}
