using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Nodes.Drawables;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Composites;

/// <summary>
/// Composite Node
/// </summary>
[TypeId("Composite", 0x0004)]
public class Composite : Node
{
    private DrawListAlloc? _screenSpaceAlloc;

    private readonly List<Part> _subParts = [];

    protected readonly List<Node> ToRender = [];

    /// <summary>
    /// PARAMETER OFFSETS
    /// </summary>
    protected float OffsetOpacity = 1;
    protected Vector3 OffsetTint = new(0);
    protected Vector3 OffsetScreenTint = new(0);

    /// <summary>
    /// The blending mode
    /// </summary>
    private BlendMode _blendingMode;

    /// <summary>
    /// The opacity of the composite
    /// </summary>
    private float _opacity = 1;

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
    public Composite(Node? parent = null) : this(Guid.NewGuid(), parent)
    {

    }

    /// <summary>
    /// Constructs a new composite
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="parent"></param>
    private Composite(Guid guid, Node? parent = null) : base(guid, parent)
    {

    }

    private void SelfSort()
    {
        _subParts.Sort((a, b) => b.ZSort.CompareTo(a.ZSort));
    }

    private void ScanPartsRecurse(Node? node)
    {
        // Don't need to scan null nodes
        if (node == null) return;

        // Do the main check
        if (node is Drawable drawable)
        {
            if (!drawable.RenderEnabled)
                return;

            ToRender.Add(drawable);
            foreach (var child in drawable.Children)
            {
                ScanPartsRecurse(child);
            }
        }
        else if (node is Composite composite)
        {
            if (!composite.RenderEnabled)
                return;

            ToRender.Add(composite);
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

    public override void Serialize(JsonObject serializer, bool recursive = true)
    {
        base.Serialize(serializer, recursive);

        serializer.Add("blend_mode", _blendingMode.ToString());
        serializer.Add("tint", _tint.ToToken());
        serializer.Add("screenTint", _screenTint.ToToken());
        serializer.Add("opacity", _opacity);
        var list = new JsonArray();
        foreach (var item in _masks)
        {
            var obj = new JsonObject();
            item.Serialize(obj);
            list.Add(obj);
        }
        serializer.Add("masks", list);
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);
        foreach (var item in data.EnumerateObject())
        {
            // Older models may not have these tags
            if (item.Name == "opacity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _opacity = item.Value.GetSingle();
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
                var str = item.Value.GetString()!;
                _blendingMode = str.ToBlendMode();
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
    }

    public override void Finalized()
    {
        base.Finalized();

        var validMasks = new List<MaskBinding>();
        for (int i = 0; i < _masks.Count; i++)
        {
            if (Puppet.Find<Drawable>(_masks[i].MaskSrcGUID) is { } nMask)
            {
                _masks[i].MaskSrc = nMask;
                validMasks.Add(_masks[i]);
            }
        }

        // Remove invalid masks
        _masks = validMasks;
    }

    protected int MaskCount()
    {
        int c = 0;
        foreach (var m in _masks) if (m.Mode == MaskingMode.Mask) c++;
        return c;
    }

    protected int DodgeCount()
    {
        int c = 0;
        foreach (var m in _masks) if (m.Mode == MaskingMode.Dodge) c++;
        return c;
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
        foreach (var mask in _masks)
        {
            if (mask.MaskSrc.Guid == drawable.Guid) return true;
        }
        return false;
    }

    public int GetMaskIdx(Drawable drawable)
    {
        if (drawable is null) return -1;
        for (int i = 0; i < _masks.Count; i++)
        {
            var mask = _masks[i];
            if (mask.MaskSrc.Guid == drawable.Guid) return i;
        }
        return -1;
    }

    public int GetMaskIdx(Guid guid)
    {
        for (int i = 0; i < _masks.Count; i++)
        {
            var mask = _masks[i];
            if (mask.MaskSrc.Guid == guid) return i;
        }
        return -1;
    }

    public override void PreUpdate(DrawList drawList)
    {
        base.PreUpdate(drawList);
        _screenSpaceAlloc = null;
        OffsetOpacity = 1;
        OffsetTint = new(1, 1, 1);
        OffsetScreenTint = new(0, 0, 0);
    }

    public override void Update(float delta, DrawList drawList)
    {
        base.Update(delta, drawList);

        // Avoid over allocating a single screenspace
        // rect.
        if (_screenSpaceAlloc!=null)
            _screenSpaceAlloc = drawList.Allocate(Inochi2d.ScreenSpaceMesh.Vertices, Inochi2d.ScreenSpaceMesh.Indices);
    }

    public override void Draw(float delta, DrawList drawList)
    {
        if (!RenderEnabled || ToRender.Count == 0)
            return;

        var compositeVars = new CompositeVars
        { 
            Tint = _tint * OffsetTint,
            ScreenTint = _screenTint * OffsetScreenTint,
           Opacity =  _opacity *OffsetOpacity
        };

        SelfSort();

        // Push sub render area.
        drawList.BeginComposite();
        foreach (var  child in ToRender) 
        {
            child.Draw(delta, drawList);
        }
        drawList.EndComposite();

        if (_masks.Count > 0)
        {
            foreach (var  mask in _masks) 
            {
                mask.MaskSrc.DrawAsMask(delta, drawList, mask.Mode);
            }
        }

        // Then blit it to the main framebuffer
        drawList.SetVariables(Nid, compositeVars);
        drawList.SetMesh(_screenSpaceAlloc);
        drawList.SetDrawState(DrawState.CompositeBlit);
        drawList.SetBlending(_blendingMode);
        drawList.Next();
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
}
