using System.Numerics;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.Composites;

[TypeId("Composite")]
public class Composite : Node
{
    private readonly List<Part> _subParts = [];

    //
    //      PARAMETER OFFSETS
    //
    private float _offsetOpacity = 1;
    private Vector3 _offsetTint = new(0);
    private Vector3 _offsetScreenTint = new(0);

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

    /// <summary>
    /// Constructs a new mask
    /// </summary>
    /// <param name="parent"></param>
    public Composite(I2dCore core, Node? parent = null) : this(core, core.InCreateUUID(), parent)
    {
        
    }

    /// <summary>
    /// Constructs a new composite
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    private Composite(I2dCore core, uint uuid, Node? parent = null) : base(core, uuid, parent)
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
                _offsetOpacity *= value;
                return true;
            case "tint.r":
                _offsetTint.X *= value;
                return true;
            case "tint.g":
                _offsetTint.Y *= value;
                return true;
            case "tint.b":
                _offsetTint.Z *= value;
                return true;
            case "screenTint.r":
                _offsetScreenTint.X += value;
                return true;
            case "screenTint.g":
                _offsetScreenTint.Y += value;
                return true;
            case "screenTint.b":
                _offsetScreenTint.Z += value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "opacity" => _offsetOpacity,
            "tint.r" => _offsetTint.X,
            "tint.g" => _offsetTint.Y,
            "tint.b" => _offsetTint.Z,
            "screenTint.r" => _offsetScreenTint.X,
            "screenTint.g" => _offsetScreenTint.Y,
            "screenTint.b" => _offsetScreenTint.Z,
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
        _offsetOpacity = 1;
        _offsetTint = new(1, 1, 1);
        _offsetScreenTint = new(0, 0, 0);
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
            _core.InBeginMask(cMasks > 0);

            foreach (var mask in Masks)
            {
                mask.maskSrc.RenderMask(mask.Mode == MaskingMode.DodgeMask);
            }

            _core.InBeginMaskContent();

            // We are the content
            DrawSelf();

            _core.InEndMask();
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
        _subParts.Clear();
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
        if (_subParts.Count == 0) return;

        _core.InBeginComposite();

        foreach (var child in _subParts)
        {
            child.DrawOne();
        }

        _core.InEndComposite();
    }

    /// <summary>
    /// RENDERING
    /// </summary>
    private void DrawSelf()
    {
        if (_subParts.Count == 0) return;

        _core.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

        _core.CShader.use();
        _core.CShader.setUniform(_core.Gopacity, float.Clamp(_offsetOpacity * Opacity, 0, 1));
        _core.IncCompositePrepareRender();

        var clampedColor = Tint;
        if (!float.IsNaN(_offsetTint.X)) clampedColor.X = float.Clamp(Tint.X * _offsetTint.X, 0, 1);
        if (!float.IsNaN(_offsetTint.Y)) clampedColor.Y = float.Clamp(Tint.Y * _offsetTint.Y, 0, 1);
        if (!float.IsNaN(_offsetTint.Z)) clampedColor.Z = float.Clamp(Tint.Z * _offsetTint.Z, 0, 1);
        _core.CShader.setUniform(_core.GMultColor, clampedColor);

        clampedColor = ScreenTint;
        if (!float.IsNaN(_offsetScreenTint.X)) clampedColor.X = float.Clamp(ScreenTint.X + _offsetScreenTint.X, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Y)) clampedColor.Y = float.Clamp(ScreenTint.Y + _offsetScreenTint.Y, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Z)) clampedColor.Z = float.Clamp(ScreenTint.Z + _offsetScreenTint.Z, 0, 1);
        _core.CShader.setUniform(_core.GScreenColor, clampedColor);
        _core.InSetBlendMode(BlendingMode, true);

        // Bind the texture
        _core.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
    }

    private void SelfSort()
    {
        _subParts.Sort((a, b) => a.ZSort.CompareTo(b.ZSort));
    }

    private void ScanPartsRecurse(Node node)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Do the main check
        if (node is Part part)
        {
            _subParts.Add(part);
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
        _core.InBeginComposite();

        // Enable writing to stencil buffer and disable writing to color buffer
        _core.gl.ColorMask(false, false, false, false);
        _core.gl.StencilOp(GlApi.GL_KEEP, GlApi.GL_KEEP, GlApi.GL_REPLACE);
        _core.gl.StencilFunc(GlApi.GL_ALWAYS, 1, 0xFF);
        _core.gl.StencilMask(0xFF);

        foreach (Part child in _subParts)
        {
            child.DrawOneDirect(true);
        }

        // Disable writing to stencil buffer and enable writing to color buffer
        _core.gl.ColorMask(true, true, true, true);
        _core.InEndComposite();

        _core.CShaderMask.use();
        _core.CShaderMask.setUniform(_core.Mopacity, Opacity);
        _core.CShaderMask.setUniform(_core.Mthreshold, Threshold);
        _core.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

        _core.gl.ActiveTexture(GlApi.GL_TEXTURE0);
        _core.gl.BindTexture(GlApi.GL_TEXTURE_2D, _core.InGetCompositeImage());
        _core.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
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
