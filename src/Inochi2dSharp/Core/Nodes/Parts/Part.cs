using System.Numerics;
using System.Runtime.InteropServices;
using Inochi2dSharp.Fmt;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.Parts;

[TypeId("Part")]
public class Part : Drawable
{
    private readonly uint uvbo;

    //
    //      PARAMETER OFFSETS
    //
    protected float offsetMaskThreshold = 0;
    protected float offsetOpacity = 1;
    protected float offsetEmissionStrength = 1;
    protected Vector3 offsetTint = new(0);
    protected Vector3 offsetScreenTint = new(0);

    //List of textures this part can use
    //TODO: use more than texture 0
    public Texture[] textures = new Texture[(int)TextureUsage.COUNT];

    /// <summary>
    /// List of texture IDs
    /// </summary>
    public List<uint> textureIds = [];

    /// <summary>
    /// List of masks to apply
    /// </summary>
    public List<MaskBinding> masks = [];

    /// <summary>
    /// Blending mode
    /// </summary>
    public BlendMode blendingMode = BlendMode.Normal;

    /// <summary>
    /// Alpha Threshold for the masking system, the higher the more opaque pixels will be discarded in the masking process
    /// </summary>
    public float maskAlphaThreshold = 0.5f;

    /// <summary>
    /// Opacity of the mesh
    /// </summary>
    public float opacity = 1;

    /// <summary>
    /// Strength of emission
    /// </summary>
    public float emissionStrength = 1;

    /// <summary>
    /// Multiplicative tint color
    /// </summary>
    public Vector3 tint = new(1, 1, 1);

    /// <summary>
    /// Screen tint color
    /// </summary>
    public Vector3 screenTint = new(0, 0, 0);

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="parent"></param>
    public Part(MeshData data, Texture[] textures, Node? parent = null) : this(data, textures, NodeHelper.InCreateUUID(), parent)
    {

    }

    public Part() : this(null)
    {

    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="parent"></param>
    public Part(Node? parent = null) : base(parent)
    {
        CoreHelper.gl.GenBuffers(1, out uvbo);
    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Part(MeshData data, Texture[] textures, uint uuid, Node? parent = null) : base(data, uuid, parent)
    {
        for (int i = 0; i < (int)TextureUsage.COUNT; i++)
        {
            if (i >= textures.Length) break;
            this.textures[i] = textures[i];
        }

        CoreHelper.gl.GenBuffers(1, out uvbo);

        updateUVs();
    }

    public override string TypeId()
    {
        return "Part";
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelf(JObject serializer)
    {
        base.SerializeSelf(serializer);
        if (FmtHelper.IsLoadingINP)
        {
            var list = new JArray();
            foreach (var texture in textures)
            {
                if (texture != null)
                {
                    var index = Puppet.GetTextureSlotIndexFor(texture);
                    if (index >= 0)
                    {
                        list.Add(index);
                    }
                    else
                    {
                        list.Add(PartHelper.NO_TEXTURE);
                    }
                }
                else
                {
                    list.Add(PartHelper.NO_TEXTURE);
                }
            }
            serializer.Add("textures", list);
        }

        serializer.Add("blend_mode", blendingMode.ToString());
        serializer.Add("tint", tint.ToToken());
        serializer.Add("screenTint", screenTint.ToToken());
        serializer.Add("emissionStrength", emissionStrength);

        if (masks != null && masks.Count > 0)
        {
            var list = new JArray();
            foreach (var m in masks)
            {
                list.Add(m);
            }
            serializer.Add("masks", list);
        }

        serializer.Add("mask_threshold", maskAlphaThreshold);
        serializer.Add("opacity", opacity);
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        if (FmtHelper.IsLoadingINP)
        {
            int i = 0;
            var temp1 = data["textures"];
            if (temp1 != null)
            {
                foreach (var texElement in temp1)
                {
                    uint textureId = (uint)texElement;

                    // uint max = no texture set
                    if (textureId == PartHelper.NO_TEXTURE) continue;

                    textureIds.Add(textureId);
                    textures[i++] = CoreHelper.inGetTextureFromId(textureId);
                }
            }
        }
        else
        {
            throw new Exception("Loading from texture path is deprecated.");
        }

        var temp = data["opacity"];
        if (temp != null)
        {
            opacity = (float)temp;
        }

        temp = data["mask_threshold"];
        if (temp != null)
        {
            maskAlphaThreshold = (float)temp;
        }

        // Older models may not have tint
        temp = data["tint"];
        if (temp != null)
        {
            tint = temp.ToVector3();
        }

        // Older models may not have screen tint
        temp = data["screenTint"];
        if (temp != null)
        {
            screenTint = temp.ToVector3();
        }

        // Older models may not have emission
        temp = data["emissionStrength"];
        if (temp != null)
        {
            tint = temp.ToVector3();
        }

        // Older models may not have blend mode
        temp = data["blend_mode"];
        if (temp != null)
        {
            blendingMode = Enum.Parse<BlendMode>(temp.ToString());
        }

        temp = data["masked_by"];
        if (temp != null)
        {
            var mode = Enum.Parse<MaskingMode>(temp.ToString());

            // Go every masked part
            var temp1 = data["masked_by"];
            if (temp1 != null)
            {
                foreach (var imask in temp1)
                {
                    uint uuid = (uint)imask;
                    masks.Add(new MaskBinding
                    {
                        MaskSrcUUID = uuid,
                        Mode = mode
                    });
                }
            }
        }

        temp = data["masks"];
        if (temp is JArray array)
        {
            foreach (var item in array)
            {
                masks.Add(item.ToObject<MaskBinding>()!);
            }
        }

        // Update indices and vertices
        updateUVs();
    }

    public override void SerializePartial(JObject obj, bool recursive = true)
    {
        base.SerializePartial(obj, recursive);
        var list = new JArray();
        foreach (var texture in textures)
        {
            uint uuid;
            if (texture != null)
            {
                uuid = texture.UUID;
            }
            else
            {
                uuid = NodeHelper.InInvalidUUID;
            }
            list.Add(uuid);
        }
        obj.Add("textureUUIDs", list);
    }

    // TODO: Cache this
    protected int maskCount()
    {
        int c = 0;
        foreach (var m in masks) if (m.Mode == MaskingMode.Mask) c++;
        return c;
    }

    protected int dodgeCount()
    {
        int c = 0;
        foreach (var m in masks) if (m.Mode == MaskingMode.DodgeMask) c++;
        return c;
    }

    private unsafe void updateUVs()
    {
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, uvbo);
        var temp = Data.Uvs.ToArray();
        fixed (void* ptr = temp)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, Data.Uvs.Count * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
    }

    private void setupShaderStage(int stage, Matrix4x4 matrix)
    {
        var clampedTint = tint;
        if (!float.IsNaN(offsetTint.X)) clampedTint.X = float.Clamp(tint.X * offsetTint.X, 0, 1);
        if (!float.IsNaN(offsetTint.Y)) clampedTint.Y = float.Clamp(tint.Y * offsetTint.Y, 0, 1);
        if (!float.IsNaN(offsetTint.Z)) clampedTint.Z = float.Clamp(tint.Z * offsetTint.Z, 0, 1);

        var clampedScreen = screenTint;
        if (!float.IsNaN(offsetScreenTint.X)) clampedScreen.X = float.Clamp(screenTint.X + offsetScreenTint.X, 0, 1);
        if (!float.IsNaN(offsetScreenTint.Y)) clampedScreen.Y = float.Clamp(screenTint.Y + offsetScreenTint.Y, 0, 1);
        if (!float.IsNaN(offsetScreenTint.Z)) clampedScreen.Z = float.Clamp(screenTint.Z + offsetScreenTint.Z, 0, 1);

        var mModel = Puppet.Transform.Matrix * matrix;
        var mViewProjection = CoreHelper.inCamera.Matrix();

        switch (stage)
        {
            case 0:
                // STAGE 1 - Advanced blending

                CoreHelper.gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);

                PartHelper.partShaderStage1.use();
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1offset, Data.Origin);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1MvpModel, mModel);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1MvpViewProjection, mViewProjection);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1opacity, float.Clamp(offsetOpacity * opacity, 0, 1));

                PartHelper.partShaderStage1.setUniform(PartHelper.partShaderStage1.getUniformLocation("albedo"), 0);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1MultColor, clampedTint);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1ScreenColor, clampedScreen);
                NodeHelper.inSetBlendMode(blendingMode, false);
                break;
            case 1:

                // STAGE 2 - Basic blending (albedo, bump)
                CoreHelper.gl.DrawBuffers(2, [GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

                PartHelper.partShaderStage2.use();
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2offset, Data.Origin);
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2MvpModel, mModel);
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2MvpViewProjection, mViewProjection);
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2opacity, float.Clamp(offsetOpacity * opacity, 0, 1));
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2EmissionStrength, emissionStrength * offsetEmissionStrength);

                PartHelper.partShaderStage2.setUniform(PartHelper.partShaderStage2.getUniformLocation("emission"), 0);
                PartHelper.partShaderStage2.setUniform(PartHelper.partShaderStage2.getUniformLocation("bump"), 1);

                // These can be reused from stage 2
                PartHelper.partShaderStage1.setUniform(PartHelper.gs2MultColor, clampedTint);
                PartHelper.partShaderStage1.setUniform(PartHelper.gs2ScreenColor, clampedScreen);
                NodeHelper.inSetBlendMode(blendingMode, true);
                break;
            case 2:

                // Basic blending
                CoreHelper.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

                PartHelper.partShader.use();
                PartHelper.partShader.setUniform(PartHelper.offset, Data.Origin);
                PartHelper.partShader.setUniform(PartHelper.mvpModel, mModel);
                PartHelper.partShader.setUniform(PartHelper.mvpViewProjection, mViewProjection);
                PartHelper.partShader.setUniform(PartHelper.gopacity, float.Clamp(offsetOpacity * opacity, 0, 1));
                PartHelper.partShader.setUniform(PartHelper.gEmissionStrength, emissionStrength * offsetEmissionStrength);

                PartHelper.partShader.setUniform(PartHelper.partShader.getUniformLocation("albedo"), 0);
                PartHelper.partShader.setUniform(PartHelper.partShader.getUniformLocation("emissive"), 1);
                PartHelper.partShader.setUniform(PartHelper.partShader.getUniformLocation("bumpmap"), 2);

                var clampedColor = tint;
                if (!float.IsNaN(offsetTint.X)) clampedColor.X = float.Clamp(tint.X * offsetTint.X, 0, 1);
                if (!float.IsNaN(offsetTint.Y)) clampedColor.Y = float.Clamp(tint.Y * offsetTint.Y, 0, 1);
                if (!float.IsNaN(offsetTint.Z)) clampedColor.Z = float.Clamp(tint.Z * offsetTint.Z, 0, 1);
                PartHelper.partShader.setUniform(PartHelper.gMultColor, clampedColor);

                clampedColor = screenTint;
                if (!float.IsNaN(offsetScreenTint.X)) clampedColor.X = float.Clamp(screenTint.X + offsetScreenTint.X, 0, 1);
                if (!float.IsNaN(offsetScreenTint.Y)) clampedColor.Y = float.Clamp(screenTint.Y + offsetScreenTint.Y, 0, 1);
                if (!float.IsNaN(offsetScreenTint.Z)) clampedColor.Z = float.Clamp(screenTint.Z + offsetScreenTint.Z, 0, 1);
                PartHelper.partShader.setUniform(PartHelper.gScreenColor, clampedColor);
                NodeHelper.inSetBlendMode(blendingMode, true);
                break;
            default: return;
        }
    }

    private void renderStage(BlendMode mode, bool advanced = true)
    {
        // Enable points array
        CoreHelper.gl.EnableVertexAttribArray(0);
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Vbo);
        CoreHelper.gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        CoreHelper.gl.EnableVertexAttribArray(1); // uvs
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, uvbo);
        CoreHelper.gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable deform array
        CoreHelper.gl.EnableVertexAttribArray(2); // deforms
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Dbo);
        CoreHelper.gl.VertexAttribPointer(2, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind index buffer
        BindIndex();

        // Disable the vertex attribs after use
        CoreHelper.gl.DisableVertexAttribArray(0);
        CoreHelper.gl.DisableVertexAttribArray(1);
        CoreHelper.gl.DisableVertexAttribArray(2);

        if (advanced)
        {
            NodeHelper.inBlendModeBarrier(mode);
        }
    }

    /// <summary>
    /// RENDERING
    /// </summary>
    /// <param name="isMask"></param>
    private void drawSelf(bool isMask = false)
    {
        // In some cases this may happen
        if (textures.Length == 0) return;

        // Bind the vertex array
        NodeHelper.incDrawableBindVAO();

        // Calculate matrix
        var matrix = Transform().Matrix;
        if (OverrideTransformMatrix != null)
            matrix = OverrideTransformMatrix.Matrix;
        if (OneTimeTransform is { } mat)
            matrix = mat * matrix;

        // Make sure we check whether we're already bound
        // Otherwise we're wasting GPU resources
        if (PartHelper.boundAlbedo != textures[0])
        {
            // Bind the textures
            for (int i = 0; i < textures.Length; i++)
            {
                var texture = textures[i];
                if (texture != null) texture.Bind((uint)i);
                else
                {
                    // Disable texture when none is there.
                    CoreHelper.gl.ActiveTexture(GlApi.GL_TEXTURE0 + (uint)i);
                    CoreHelper.gl.BindTexture(GlApi.GL_TEXTURE_2D, 0);
                }
            }
        }

        if (isMask)
        {
            var mModel = Puppet.Transform.Matrix * matrix;
            var mViewProjection = CoreHelper.inCamera.Matrix();

            PartHelper.partMaskShader.use();
            PartHelper.partMaskShader.setUniform(PartHelper.offset, Data.Origin);
            PartHelper.partMaskShader.setUniform(PartHelper.mMvpModel, mModel);
            PartHelper.partMaskShader.setUniform(PartHelper.mMvpViewProjection, mViewProjection);
            PartHelper.partMaskShader.setUniform(PartHelper.mthreshold, float.Clamp(offsetMaskThreshold + maskAlphaThreshold, 0, 1));

            // Make sure the equation is correct
            CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
            CoreHelper.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

            renderStage(blendingMode, false);
        }
        else
        {
            bool hasEmissionOrBumpmap = textures[1] != null || textures[2] != null;

            if (NodeHelper.inUseMultistageBlending(blendingMode))
            {

                // TODO: Detect if this Part is NOT in a composite,
                // If so, we can relatively safely assume that we may skip stage 1.
                setupShaderStage(0, matrix);
                renderStage(blendingMode);

                // Only do stage 2 if we have emission or bumpmap textures.
                if (hasEmissionOrBumpmap)
                {
                    setupShaderStage(1, matrix);
                    renderStage(blendingMode, false);
                }
            }
            else
            {
                setupShaderStage(2, matrix);
                renderStage(blendingMode, false);
            }
        }

        // Reset draw buffers
        CoreHelper.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        CoreHelper.gl.BlendEquation(GlApi.GL_FUNC_ADD);
    }

    /// <summary>
    /// Gets the active texture
    /// </summary>
    /// <returns></returns>
    public Texture activeTexture()
    {
        return textures[0];
    }

    public override void RenderMask(bool dodge = false)
    {
        // Enable writing to stencil buffer and disable writing to color buffer
        CoreHelper.gl.ColorMask(false, false, false, false);
        CoreHelper.gl.StencilOp(GlApi.GL_KEEP, GlApi.GL_KEEP, GlApi.GL_REPLACE);
        CoreHelper.gl.StencilFunc(GlApi.GL_ALWAYS, dodge ? 0 : 1, 0xFF);
        CoreHelper.gl.StencilMask(0xFF);

        // Draw ourselves to the stencil buffer
        drawSelf(true);

        // Disable writing to stencil buffer and enable writing to color buffer
        CoreHelper.gl.ColorMask(true, true, true, true);
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
                offsetMaskThreshold *= value;
                return true;
            case "opacity":
                offsetOpacity *= value;
                return true;
            case "tint.r":
                offsetTint.X *= value;
                return true;
            case "tint.g":
                offsetTint.Y *= value;
                return true;
            case "tint.b":
                offsetTint.Z *= value;
                return true;
            case "screenTint.r":
                offsetScreenTint.X += value;
                return true;
            case "screenTint.g":
                offsetScreenTint.Y += value;
                return true;
            case "screenTint.b":
                offsetScreenTint.Z += value;
                return true;
            case "emissionStrength":
                offsetEmissionStrength += value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "alphaThreshold" => offsetMaskThreshold,
            "opacity" => offsetOpacity,
            "tint.r" => offsetTint.X,
            "tint.g" => offsetTint.Y,
            "tint.b" => offsetTint.Z,
            "screenTint.r" => offsetScreenTint.X,
            "screenTint.g" => offsetScreenTint.Y,
            "screenTint.b" => offsetScreenTint.Z,
            "emissionStrength" => offsetEmissionStrength,
            _ => base.GetValue(key),
        };
    }

    public bool isMaskedBy(Drawable drawable)
    {
        foreach (var mask in masks)
        {
            if (mask.maskSrc.UUID == drawable.UUID) return true;
        }
        return false;
    }

    public int getMaskIdx(Drawable drawable)
    {
        if (drawable is null) return -1;
        for (int i = 0; i < masks.Count; i++)
        {
            var mask = masks[i];
            if (mask.maskSrc.UUID == drawable.UUID) return i;
        }
        return -1;
    }

    public int getMaskIdx(uint uuid)
    {
        for (int i = 0; i < masks.Count; i++)
        {
            var mask = masks[i];
            if (mask.maskSrc.UUID == uuid) return i;
        }
        return -1;
    }

    public override void BeginUpdate()
    {
        offsetMaskThreshold = 0;
        offsetOpacity = 1;
        offsetTint = new(1, 1, 1);
        offsetScreenTint = new(0, 0, 0);
        offsetEmissionStrength = 1;
        base.BeginUpdate();
    }

    public override void Rebuffer(MeshData data)
    {
        base.Rebuffer(data);
        this.updateUVs();
    }

    public override void Draw()
    {
        if (!enabled) return;
        this.DrawOne();

        foreach (var child in Children)
        {
            child.Draw();
        }
    }

    public override void DrawOne()
    {
        if (!enabled) return;
        if (!Data.IsReady()) return; // Yeah, don't even try

        var cMasks = maskCount();

        if (masks.Count > 0)
        {
            InBeginMask(cMasks > 0);

            foreach (var mask in masks)
            {
                mask.maskSrc.RenderMask(mask.Mode == MaskingMode.DodgeMask);
            }

            InBeginMaskContent();

            // We are the content
            this.drawSelf();

            InEndMask();
            return;
        }

        // No masks, draw normally
        this.drawSelf();
        base.DrawOne();
    }

    public override void DrawOneDirect(bool forMasking)
    {
        if (forMasking) this.drawSelf(true);
        else this.drawSelf(false);
    }

    public override void Dispose()
    {
        base.Dispose();

        var validMasks = new List<MaskBinding>();
        for (int i = 0; i < masks.Count; i++)
        {
            if (Puppet.Find<Drawable>(masks[i].MaskSrcUUID) is { } nMask)
            {
                masks[i].maskSrc = nMask;
                validMasks.Add(masks[i]);
            }
        }

        // Remove invalid masks
        masks = validMasks;
    }

    public override void SetOneTimeTransform(Matrix4x4 transform)
    {
        base.SetOneTimeTransform(transform);
        foreach (var m in masks)
        {
            m.maskSrc.OneTimeTransform = transform;
        }
    }
}
