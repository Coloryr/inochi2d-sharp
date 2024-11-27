using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Nodes.Parts;

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
    private BlendMode _blendingMode;

    /// <summary>
    /// The opacity of the composite
    /// </summary>
    private float _opacity = 1;

    /// <summary>
    /// The threshold for rendering masks
    /// </summary>
    private float _threshold = 0.5f;

    /// <summary>
    /// Multiplicative tint color
    /// </summary>
    private Vector3 _tint = new(1, 1, 1);

    /// <summary>
    /// Screen tint color
    /// </summary>
    private Vector3 _screenTint = new(0, 0, 0);

    /// <summary>
    /// List of masks to apply
    /// </summary>
    private List<MaskBinding> _masks = [];

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
        foreach (var mask in _masks)
        {
            if (mask.MaskSrc.UUID == drawable.UUID) return true;
        }
        return false;
    }

    public int GetMaskIdx(Drawable drawable)
    {
        if (drawable is null) return -1;
        for (int i = 0; i < _masks.Count; i++)
        {
            var mask = _masks[i];
            if (mask.MaskSrc.UUID == drawable.UUID) return i;
        }
        return -1;
    }

    public int GetMaskIdx(uint uuid)
    {
        for (int i = 0; i < _masks.Count; i++)
        {
            var mask = _masks[i];
            if (mask.MaskSrc.UUID == uuid) return i;
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

        if (_masks.Count > 0)
        {
            _core.InBeginMask(cMasks > 0);

            foreach (var mask in _masks)
            {
                mask.MaskSrc.RenderMask(mask.Mode == MaskingMode.DodgeMask);
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

    public override void JsonLoadDone()
    {
        base.JsonLoadDone();

        var validMasks = new List<MaskBinding>();
        for (int i = 0; i < _masks.Count; i++)
        {
            if (Puppet.Find<Drawable>(_masks[i].MaskSrcUUID) is { } nMask)
            {
                _masks[i].MaskSrc = nMask;
                validMasks.Add(_masks[i]);
            }
        }

        // Remove invalid masks
        _masks = validMasks;
    }

    /// <summary>
    /// Scans for parts to render
    /// </summary>
    public void ScanParts()
    {
        _subParts.Clear();
        if (Children.Count > 0)
        {
            ScanPartsRecurse(Children[0].Parent!);
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

        _core.CShader.Use();
        _core.CShader.SetUniform(_core.Gopacity, float.Clamp(_offsetOpacity * _opacity, 0, 1));
        _core.IncCompositePrepareRender();

        var clampedColor = _tint;
        if (!float.IsNaN(_offsetTint.X)) clampedColor.X = float.Clamp(_tint.X * _offsetTint.X, 0, 1);
        if (!float.IsNaN(_offsetTint.Y)) clampedColor.Y = float.Clamp(_tint.Y * _offsetTint.Y, 0, 1);
        if (!float.IsNaN(_offsetTint.Z)) clampedColor.Z = float.Clamp(_tint.Z * _offsetTint.Z, 0, 1);
        _core.CShader.SetUniform(_core.GMultColor, clampedColor);

        clampedColor = _screenTint;
        if (!float.IsNaN(_offsetScreenTint.X)) clampedColor.X = float.Clamp(_screenTint.X + _offsetScreenTint.X, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Y)) clampedColor.Y = float.Clamp(_screenTint.Y + _offsetScreenTint.Y, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Z)) clampedColor.Z = float.Clamp(_screenTint.Z + _offsetScreenTint.Z, 0, 1);
        _core.CShader.SetUniform(_core.GScreenColor, clampedColor);
        _core.InSetBlendMode(_blendingMode, true);

        // Bind the texture
        _core.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
    }

    private void SelfSort()
    {
        _subParts.Sort((a, b) => b.ZSort.CompareTo(a.ZSort));
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

        _core.CShaderMask.Use();
        _core.CShaderMask.SetUniform(_core.Mopacity, _opacity);
        _core.CShaderMask.SetUniform(_core.Mthreshold, _threshold);
        _core.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

        _core.gl.ActiveTexture(GlApi.GL_TEXTURE0);
        _core.gl.BindTexture(GlApi.GL_TEXTURE_2D, _core.InGetCompositeImage());
        _core.gl.DrawArrays(GlApi.GL_TRIANGLES, 0, 6);
    }

    protected override void SerializeSelfImpl(JsonObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);

        serializer.Add("blend_mode", _blendingMode.ToString());
        serializer.Add("tint", _tint.ToToken());
        serializer.Add("screenTint", _screenTint.ToToken());
        serializer.Add("mask_threshold", _threshold);
        serializer.Add("opacity", _opacity);
        serializer.Add("propagate_meshgroup", PropagateMeshGroup);

        if (_masks.Count > 0)
        {
            var list = new JsonArray();
            foreach (var item in _masks)
            {
                var obj = new JsonObject();
                item.Serialize(obj);
                list.Add(obj);
            }
            serializer.Add("masks", list);
        }
    }

    public override void Deserialize(JsonElement data)
    {
        PropagateMeshGroup = false;
        foreach (var item in data.EnumerateObject())
        {
            // Older models may not have these tags
            if (item.Name == "opacity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _opacity = item.Value.GetSingle();
            }

            else if (item.Name == "mask_threshold" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _threshold = item.Value.GetSingle();
            }

            else if (item.Name == "tint" && item.Value.ValueKind == JsonValueKind.Array)
            {
                _tint = item.Value.ToVector3();
            }

            else if (item.Name == "screenTint" && item.Value.ValueKind == JsonValueKind.Array)
            {
                _screenTint = item.Value.ToVector3();
            }

            else if (item.Name == "blend_mode" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _blendingMode = Enum.Parse<BlendMode>(item.Value.GetString()!);
            }

            else if (item.Name == "propagate_meshgroup" && item.Value.ValueKind != JsonValueKind.Null)
            {
                PropagateMeshGroup = item.Value.GetBoolean();
            }
            else if (item.Name == "masks" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    var mask = new MaskBinding();
                    mask.Deserialize(item1);
                    _masks.Add(mask);
                }
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
        foreach (var m in _masks) if (m.Mode == MaskingMode.Mask) c++;
        return c;
    }

    protected int DodgeCount()
    {
        int c = 0;
        foreach (var m in _masks) if (m.Mode == MaskingMode.DodgeMask) c++;
        return c;
    }

    public override void PreProcess()
    {
        if (!PropagateMeshGroup)
            base.PreProcess();
    }

    public override void PostProcess()
    {
        if (!PropagateMeshGroup)
            base.PostProcess();
    }
}
