using System.Numerics;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Drawables;

/// <summary>
/// Dynamic Mesh Part
/// </summary>
[TypeId("Part", 0x0101)]
public class Part : Drawable
{
    public const uint NO_TEXTURE = uint.MaxValue;

    /// <summary>
    /// PARAMETER OFFSETS
    /// </summary>
    protected float OffsetMaskThreshold = 0;
    protected float OffsetOpacity = 1;
    protected float OffsetEmissionStrength = 1;

    protected Vector3 OffsetTint = new(0);
    protected Vector3 OffsetScreenTint = new(0);

    //List of textures this part can use
    //TODO: use more than texture 0
    public Texture[] Textures = new Texture[DrawCmd.IN_MAX_ATTACHMENTS];

    /// <summary>
    /// List of masks to apply
    /// </summary>
    public List<MaskBinding> Masks = [];

    /// <summary>
    /// Blending mode
    /// </summary>
    public BlendMode BlendingMode = BlendMode.Normal;

    /// <summary>
    /// Opacity of the mesh
    /// </summary>
    public float Opacity = 1;

    /// <summary>
    /// Strength of emission
    /// </summary>
    public float EmissionStrength = 1;

    /// <summary>
    /// Multiplicative tint color
    /// </summary>
    public Vector3 Tint = new(1, 1, 1);

    /// <summary>
    /// Screen tint color
    /// </summary>
    public Vector3 ScreenTint = new(0, 0, 0);

    /// <summary>
    /// Gets the active texture
    /// </summary>
    public Texture? ActiveTexture => Textures[0];

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="recursive"></param>
    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);

        var list = new JsonArray();

        foreach (var texture in Textures)
        {
            if (texture != null)
            {
                var index = Puppet.GetTextureSlotIndexFor(texture);
                list.Add(index >= 0 ? index : NO_TEXTURE);
            }
            else
            {
                list.Add(uint.MaxValue);
            }
        }

        obj["textures"] = list;
        obj["blend_mode"] = BlendingMode.ToString();
        obj["tint"] = Tint.ToToken();
        obj["screenTint"] = ScreenTint.ToToken();
        obj["emissionStrength"] = EmissionStrength;
        var list1 = new JsonArray();
        foreach (var item in Masks)
        {
            var obj1 = new JsonObject();
            item.Serialize(obj1);
            list1.Add(obj1);
        }
        obj["masks"] = list1;
        obj["opacity"] = Opacity;
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);

        int i = 0;
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "textures" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement texElement in item.Value.EnumerateArray())
                {
                    if (texElement.ValueKind == JsonValueKind.Null)
                    {
                        i++;
                        continue;
                    }
                    uint textureId = texElement.GetUInt32();

                    // uint max = no texture set
                    if (textureId == NO_TEXTURE)
                    {
                        i++;
                        continue;
                    }

                    // TODO: Abstract this to properly handle refcounts.
                    var temp = Puppet.TextureCache.Get((int)textureId);
                    if (temp != null)
                    {
                        Textures[i] = temp;
                        Textures[i].Retain();
                    }
                    i++;
                }
            }
            else if (item.Name == "opacity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Opacity = item.Value.GetSingle();
            }
            // Older models may not have tint
            else if (item.Name == "tint" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Tint = item.Value.ToVector3();
            }
            // Older models may not have screen tint
            else if (item.Name == "screenTint" && item.Value.ValueKind == JsonValueKind.Array)
            {
                ScreenTint = item.Value.ToVector3();
            }
            // Older models may not have emission
            else if (item.Name == "emissionStrength" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Tint = item.Value.ToVector3();
            }
            // Older models may not have blend mode
            else if (item.Name == "blend_mode" && item.Value.ValueKind != JsonValueKind.Null)
            {
                BlendingMode = item.Value.GetString()!.ToBlendMode();
            }
            else if (item.Name == "masks" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    var mask = new MaskBinding();
                    mask.Deserialize(item1);
                    Masks.Add(mask);
                }
            }
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (var item in Textures)
        {
            item?.Released();
        }
    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="parent"></param>
    public Part(Node? parent = null) : base(parent)
    {

    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="parent"></param>
    public Part(MeshData data, Texture[] textures, Node? parent = null) : this(data, textures, Guid.NewGuid(), parent)
    {

    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="guid"></param>
    /// <param name="parent"></param>
    public Part(MeshData data, Texture[] textures, Guid guid, Node? parent = null) : base(data, guid, parent)
    {
        for (int i = 0; i < (int)TextureUsage.COUNT; i++)
        {
            if (i >= textures.Length) break;
            Textures[i] = textures[i];
        }
    }

    public override bool HasParam(string key)
    {
        if (base.HasParam(key)) return true;

        return key switch
        {
            "alphaThreshold" or "opacity" or "tint.r" or "tint.g" or "tint.b" or "screenTint.r" or "screenTint.g" or "screenTint.b" or "emissionStrength" => true,
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
            "alphaThreshold" => 0,
            "opacity" or "tint.r" or "tint.g" or "tint.b" => 1,
            "screenTint.r" or "screenTint.g" or "screenTint.b" => 0,
            "emissionStrength" => 1,
            _ => 0,
        };
    }

    public override bool SetValue(string key, float value)
    {
        // Skip our list of our parent already handled it
        if (base.SetValue(key, value)) return true;

        switch (key)
        {
            case "alphaThreshold":
                OffsetMaskThreshold *= value;
                return true;
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
            case "emissionStrength":
                OffsetEmissionStrength += value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "alphaThreshold" => OffsetMaskThreshold,
            "opacity" => OffsetOpacity,
            "tint.r" => OffsetTint.X,
            "tint.g" => OffsetTint.Y,
            "tint.b" => OffsetTint.Z,
            "screenTint.r" => OffsetScreenTint.X,
            "screenTint.g" => OffsetScreenTint.Y,
            "screenTint.b" => OffsetScreenTint.Z,
            "emissionStrength" => OffsetEmissionStrength,
            _ => base.GetValue(key),
        };
    }

    public bool IsMaskedBy(Drawable drawable)
    {
        foreach (var mask in Masks)
        {
            if (mask.MaskSrc.Guid == drawable.Guid) return true;
        }
        return false;
    }

    public int GetMaskIdx(Drawable drawable)
    {
        if (drawable is null) return -1;
        for (int i = 0; i < Masks.Count; i++)
        {
            var mask = Masks[i];
            if (mask.MaskSrc.Guid == drawable.Guid) return i;
        }
        return -1;
    }

    public int GetMaskIdx(Guid guid)
    {
        for (int i = 0; i < Masks.Count; i++)
        {
            var mask = Masks[i];
            if (mask.MaskSrc.Guid == guid) return i;
        }
        return -1;
    }

    public override void PreUpdate(DrawList drawList)
    {
        OffsetMaskThreshold = 0;
        OffsetOpacity = 1;
        OffsetTint = new(1, 1, 1);
        OffsetScreenTint = new(0, 0, 0);
        OffsetEmissionStrength = 1;
        base.PreUpdate(drawList);
    }

    public override unsafe void Draw(float delta, DrawList drawList)
    {
        if (!RenderEnabled)
            return;

        PartVars* vars = stackalloc PartVars[1];

        vars->Tint = Tint * OffsetTint;
        vars->ScreenTint = ScreenTint * OffsetScreenTint;
        vars->Opacity = Opacity * OffsetOpacity;
        vars->EmissionStrength = EmissionStrength * OffsetEmissionStrength;

        if (Masks.Count > 0)
        {
            foreach (var mask in Masks)
            {
                mask.MaskSrc?.DrawAsMask(delta, drawList, mask.Mode);
            }

            base.Draw(delta, drawList);
            drawList.SetDrawState(DrawState.MaskedDraw);
            drawList.SetVariables(Nid, vars, PartVarsHelper.Size);
            drawList.SetBlending(BlendingMode);
            drawList.SetSources(Textures);
            drawList.Next();
            return;
        }

        base.Draw(delta, drawList);
        drawList.SetSources(Textures);
        drawList.SetBlending(BlendingMode);
        drawList.SetVariables(Nid, vars, PartVarsHelper.Size);
        drawList.Next();
    }

    public override void DrawAsMask(float delta, DrawList drawList, MaskingMode mode)
    {
        base.DrawAsMask(delta, drawList, mode);
        drawList.SetDrawState(DrawState.DefineMask);
        drawList.SetSources(Textures);
        drawList.SetMasking(mode);
        drawList.Next();
    }

    public override void Finalized()
    {
        base.Finalized();

        var validMasks = new List<MaskBinding>();
        for (int i = 0; i < Masks.Count; i++)
        {
            if (Puppet.Find<Drawable>(Masks[i].MaskSrcGUID) is { } nMask)
            {
                Masks[i].MaskSrc = nMask;
                validMasks.Add(Masks[i]);
            }
        }

        // Remove invalid masks
        Masks = validMasks;
    }
}
