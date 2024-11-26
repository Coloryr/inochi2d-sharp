using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Text.Json.Nodes;

namespace Inochi2dSharp.Core.Nodes.Parts;

[TypeId("Part")]
public class Part : Drawable
{
    private readonly uint _uvbo;

    //
    //      PARAMETER OFFSETS
    //
    private float _offsetMaskThreshold = 0;
    private float _offsetOpacity = 1;
    private float _offsetEmissionStrength = 1;
    private Vector3 _offsetTint = new(0);
    private Vector3 _offsetScreenTint = new(0);

    //List of textures this part can use
    //TODO: use more than texture 0
    public Texture[] Textures = new Texture[(int)TextureUsage.COUNT];

    /// <summary>
    /// List of texture IDs
    /// </summary>
    private readonly List<uint> _textureIds = [];

    /// <summary>
    /// List of masks to apply
    /// </summary>
    private List<MaskBinding> _masks = [];

    /// <summary>
    /// Blending mode
    /// </summary>
    private BlendMode _blendingMode = BlendMode.Normal;

    /// <summary>
    /// Alpha Threshold for the masking system, the higher the more opaque pixels will be discarded in the masking process
    /// </summary>
    private float _maskAlphaThreshold = 0.5f;

    /// <summary>
    /// Opacity of the mesh
    /// </summary>
    public float Opacity = 1;

    /// <summary>
    /// Strength of emission
    /// </summary>
    private float _emissionStrength = 1;

    /// <summary>
    /// Multiplicative tint color
    /// </summary>
    public Vector3 Tint  = new(1, 1, 1);

    /// <summary>
    /// Screen tint color
    /// </summary>
    public Vector3 ScreenTint  = new(0, 0, 0);

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="parent"></param>
    public Part(I2dCore core, MeshData data, Texture[] textures, Node? parent = null) : this(core, data, textures, core.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="parent"></param>
    public Part(I2dCore core, Node? parent = null) : base(core, parent)
    {
        _uvbo = core.gl.GenBuffer();
    }

    /// <summary>
    /// Constructs a new part
    /// </summary>
    /// <param name="data"></param>
    /// <param name="textures"></param>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Part(I2dCore core, MeshData data, Texture[] textures, uint uuid, Node? parent = null) : base(core, data, uuid, parent)
    {
        for (int i = 0; i < (int)TextureUsage.COUNT; i++)
        {
            if (i >= textures.Length) break;
            Textures[i] = textures[i];
        }

        _uvbo = core.gl.GenBuffer();

        UpdateUVs();
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
    protected override void SerializeSelf(JsonObject serializer)
    {
        base.SerializeSelf(serializer);
        if (_core.IsLoadingINP)
        {
            var list = new JsonArray();
            foreach (var texture in Textures)
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
                        list.Add(I2dCore.NO_TEXTURE);
                    }
                }
                else
                {
                    list.Add(I2dCore.NO_TEXTURE);
                }
            }
            serializer.Add("textures", list);
        }

        serializer.Add("blend_mode", _blendingMode.ToString());
        serializer.Add("tint", Tint.ToToken());
        serializer.Add("screenTint", ScreenTint.ToToken());
        serializer.Add("emissionStrength", _emissionStrength);

        if (_masks != null && _masks.Count > 0)
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

        serializer.Add("mask_threshold", _maskAlphaThreshold);
        serializer.Add("opacity", Opacity);
    }

    public override void Deserialize(JsonObject data)
    {
        base.Deserialize(data);

        if (_core.IsLoadingINP)
        {
            int i = 0;
            if (data.TryGetPropertyValue("textures", out var temp1) && temp1 is JsonArray array4)
            {
                foreach (var texElement in array4)
                {
                    if (texElement == null)
                    {
                        continue;
                    }
                    uint textureId = texElement.GetValue<uint>();

                    // uint max = no texture set
                    if (textureId == I2dCore.NO_TEXTURE)
                    {
                        continue;
                    }

                    _textureIds.Add(textureId);
                    Textures[i++] = _core.InGetTextureFromId(textureId);
                }
            }
        }
        else
        {
            throw new Exception("Loading from texture path is deprecated.");
        }

        if (data.TryGetPropertyValue("opacity", out var temp) && temp != null)
        {
            Opacity = temp.GetValue<float>();
        }

        if (data.TryGetPropertyValue("mask_threshold", out temp) && temp != null)
        {
            _maskAlphaThreshold = temp.GetValue<float>();
        }

        // Older models may not have tint
        if (data.TryGetPropertyValue("tint", out temp) && temp is JsonArray array1)
        {
            Tint = array1.ToVector3();
        }

        // Older models may not have screen tint
        if (data.TryGetPropertyValue("screenTint", out temp) && temp is JsonArray array2)
        {
            ScreenTint = array2.ToVector3();
        }

        // Older models may not have emission
        if (data.TryGetPropertyValue("emissionStrength", out temp) && temp is JsonArray array3)
        {
            Tint = array3.ToVector3();
        }

        // Older models may not have blend mode
        if (data.TryGetPropertyValue("blend_mode", out temp) && temp != null)
        {
            _blendingMode = Enum.Parse<BlendMode>(temp.GetValue<string>());
        }

        if (data.TryGetPropertyValue("mask_mode", out temp) && temp != null)
        {
            var mode = Enum.Parse<MaskingMode>(temp.ToString());

            // Go every masked part
            if (data.TryGetPropertyValue("masked_by", out var temp1) && temp1 is JsonArray array4)
            {
                foreach (var imask in array4)
                {
                    if (imask == null)
                    {
                        continue;
                    }
                    uint uuid = imask.GetValue<uint>();
                    _masks.Add(new MaskBinding
                    {
                        MaskSrcUUID = uuid,
                        Mode = mode
                    });
                }
            }
        }

        if (data.TryGetPropertyValue("masks", out temp) && temp is JsonArray array)
        {
            foreach (var item in array.Cast<JsonObject>())
            {
                var item1 = new MaskBinding();
                item1.Deserialize(item);
                _masks.Add(item1);
            }
        }

        // Update indices and vertices
        UpdateUVs();
    }

    public override void SerializePartial(JsonObject obj, bool recursive = true)
    {
        base.SerializePartial(obj, recursive);
        var list = new JsonArray();
        foreach (var texture in Textures)
        {
            uint uuid;
            if (texture != null)
            {
                uuid = texture.UUID;
            }
            else
            {
                uuid = I2dCore.InInvalidUUID;
            }
            list.Add(uuid);
        }
        obj.Add("textureUUIDs", list);
    }

    // TODO: Cache this
    protected int MaskCount()
    {
        int c = 0;
        foreach (var m in _masks) if (m.Mode == MaskingMode.Mask) c++;
        return c;
    }

    protected int DodgeCount()
    {
        int c = 0;
        foreach (var m in _masks) if (m.Mode == MaskingMode.DodgeMask) c++;
        return c;
    }

    private unsafe void UpdateUVs()
    {
        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _uvbo);
        var temp = Data.Uvs.ToArray();
        fixed (void* ptr = temp)
        {
            _core.gl.BufferData(GlApi.GL_ARRAY_BUFFER, Data.Uvs.Count * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
        }
    }

    private void SetupShaderStage(int stage, Matrix4x4 matrix)
    {
        var clampedTint = Tint;
        if (!float.IsNaN(_offsetTint.X)) clampedTint.X = float.Clamp(Tint.X * _offsetTint.X, 0, 1);
        if (!float.IsNaN(_offsetTint.Y)) clampedTint.Y = float.Clamp(Tint.Y * _offsetTint.Y, 0, 1);
        if (!float.IsNaN(_offsetTint.Z)) clampedTint.Z = float.Clamp(Tint.Z * _offsetTint.Z, 0, 1);

        var clampedScreen = ScreenTint;
        if (!float.IsNaN(_offsetScreenTint.X)) clampedScreen.X = float.Clamp(ScreenTint.X + _offsetScreenTint.X, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Y)) clampedScreen.Y = float.Clamp(ScreenTint.Y + _offsetScreenTint.Y, 0, 1);
        if (!float.IsNaN(_offsetScreenTint.Z)) clampedScreen.Z = float.Clamp(ScreenTint.Z + _offsetScreenTint.Z, 0, 1);

        var mModel = Puppet.Transform.Matrix * matrix;
        var mViewProjection = _core.InCamera.Matrix();

        switch (stage)
        {
            case 0:
                // STAGE 1 - Advanced blending

                _core.gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);

                _core.partShaderStage1.Use();
                _core.partShaderStage1.SetUniform(_core.gs1offset, Data.Origin);
                _core.partShaderStage1.SetUniform(_core.gs1MvpModel, mModel);
                _core.partShaderStage1.SetUniform(_core.gs1MvpViewProjection, mViewProjection);
                _core.partShaderStage1.SetUniform(_core.gs1opacity, float.Clamp(_offsetOpacity * Opacity, 0, 1));

                _core.partShaderStage1.SetUniform(_core.partShaderStage1.GetUniformLocation("albedo"), 0);
                _core.partShaderStage1.SetUniform(_core.gs1MultColor, clampedTint);
                _core.partShaderStage1.SetUniform(_core.gs1ScreenColor, clampedScreen);
                _core.InSetBlendMode(_blendingMode, false);
                break;
            case 1:

                // STAGE 2 - Basic blending (albedo, bump)
                _core.gl.DrawBuffers(2, [GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

                _core.partShaderStage2.Use();
                _core.partShaderStage2.SetUniform(_core.gs2offset, Data.Origin);
                _core.partShaderStage2.SetUniform(_core.gs2MvpModel, mModel);
                _core.partShaderStage2.SetUniform(_core.gs2MvpViewProjection, mViewProjection);
                _core.partShaderStage2.SetUniform(_core.gs2opacity, float.Clamp(_offsetOpacity * Opacity, 0, 1));
                _core.partShaderStage2.SetUniform(_core.gs2EmissionStrength, _emissionStrength * _offsetEmissionStrength);

                _core.partShaderStage2.SetUniform(_core.partShaderStage2.GetUniformLocation("emission"), 0);
                _core.partShaderStage2.SetUniform(_core.partShaderStage2.GetUniformLocation("bump"), 1);

                // These can be reused from stage 2
                _core.partShaderStage1.SetUniform(_core.gs2MultColor, clampedTint);
                _core.partShaderStage1.SetUniform(_core.gs2ScreenColor, clampedScreen);
                _core.InSetBlendMode(_blendingMode, true);
                break;
            case 2:

                // Basic blending
                _core.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);

                _core.partShader.Use();
                _core.partShader.SetUniform(_core.offset, Data.Origin);
                _core.partShader.SetUniform(_core.mvpModel, mModel);
                _core.partShader.SetUniform(_core.mvpViewProjection, mViewProjection);
                _core.partShader.SetUniform(_core.gopacity, float.Clamp(_offsetOpacity * Opacity, 0, 1));
                _core.partShader.SetUniform(_core.gEmissionStrength, _emissionStrength * _offsetEmissionStrength);

                _core.partShader.SetUniform(_core.partShader.GetUniformLocation("albedo"), 0);
                _core.partShader.SetUniform(_core.partShader.GetUniformLocation("emissive"), 1);
                _core.partShader.SetUniform(_core.partShader.GetUniformLocation("bumpmap"), 2);

                var clampedColor = Tint;
                if (!float.IsNaN(_offsetTint.X)) clampedColor.X = float.Clamp(Tint.X * _offsetTint.X, 0, 1);
                if (!float.IsNaN(_offsetTint.Y)) clampedColor.Y = float.Clamp(Tint.Y * _offsetTint.Y, 0, 1);
                if (!float.IsNaN(_offsetTint.Z)) clampedColor.Z = float.Clamp(Tint.Z * _offsetTint.Z, 0, 1);
                _core.partShader.SetUniform(_core.gMultColor, clampedColor);

                clampedColor = ScreenTint;
                if (!float.IsNaN(_offsetScreenTint.X)) clampedColor.X = float.Clamp(ScreenTint.X + _offsetScreenTint.X, 0, 1);
                if (!float.IsNaN(_offsetScreenTint.Y)) clampedColor.Y = float.Clamp(ScreenTint.Y + _offsetScreenTint.Y, 0, 1);
                if (!float.IsNaN(_offsetScreenTint.Z)) clampedColor.Z = float.Clamp(ScreenTint.Z + _offsetScreenTint.Z, 0, 1);
                _core.partShader.SetUniform(_core.gScreenColor, clampedColor);
                _core.InSetBlendMode(_blendingMode, true);
                break;
            default: return;
        }
    }

    private void RenderStage(BlendMode mode, bool advanced = true)
    {
        // Enable points array
        _core.gl.EnableVertexAttribArray(0);
        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Vbo);
        _core.gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        _core.gl.EnableVertexAttribArray(1); // uvs
        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, _uvbo);
        _core.gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable deform array
        _core.gl.EnableVertexAttribArray(2); // deforms
        _core.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, Dbo);
        _core.gl.VertexAttribPointer(2, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind index buffer
        BindIndex();

        // Disable the vertex attribs after use
        _core.gl.DisableVertexAttribArray(0);
        _core.gl.DisableVertexAttribArray(1);
        _core.gl.DisableVertexAttribArray(2);

        if (advanced)
        {
            _core.InBlendModeBarrier(mode);
        }
    }

    /// <summary>
    /// RENDERING
    /// </summary>
    /// <param name="isMask"></param>
    private void DrawSelf(bool isMask = false)
    {
        // In some cases this may happen
        if (Textures.Length == 0) return;

        // Bind the vertex array
        _core.IncDrawableBindVAO();

        // Calculate matrix
        var matrix = Transform().Matrix;
        if (OverrideTransformMatrix != null)
            matrix = OverrideTransformMatrix.Matrix;
        if (OneTimeTransform is { } mat)
            matrix = mat * matrix;

        // Make sure we check whether we're already bound
        // Otherwise we're wasting GPU resources
        if (_core.boundAlbedo != Textures[0])
        {
            // Bind the textures
            for (int i = 0; i < Textures.Length; i++)
            {
                var texture = Textures[i];
                if (texture != null) texture.Bind((uint)i);
                else
                {
                    // Disable texture when none is there.
                    _core.gl.ActiveTexture(GlApi.GL_TEXTURE0 + (uint)i);
                    _core.gl.BindTexture(GlApi.GL_TEXTURE_2D, 0);
                }
            }
        }

        if (isMask)
        {
            var mModel = Puppet.Transform.Matrix * matrix;
            var mViewProjection = _core.InCamera.Matrix();

            _core.partMaskShader.Use();
            _core.partMaskShader.SetUniform(_core.offset, Data.Origin);
            _core.partMaskShader.SetUniform(_core.mMvpModel, mModel);
            _core.partMaskShader.SetUniform(_core.mMvpViewProjection, mViewProjection);
            _core.partMaskShader.SetUniform(_core.mthreshold, float.Clamp(_offsetMaskThreshold + _maskAlphaThreshold, 0, 1));

            // Make sure the equation is correct
            _core.gl.BlendEquation(GlApi.GL_FUNC_ADD);
            _core.gl.BlendFunc(GlApi.GL_ONE, GlApi.GL_ONE_MINUS_SRC_ALPHA);

            RenderStage(_blendingMode, false);
        }
        else
        {
            bool hasEmissionOrBumpmap = Textures[1] != null || Textures[2] != null;

            if (_core.InUseMultistageBlending(_blendingMode))
            {

                // TODO: Detect if this Part is NOT in a composite,
                // If so, we can relatively safely assume that we may skip stage 1.
                SetupShaderStage(0, matrix);
                RenderStage(_blendingMode);

                // Only do stage 2 if we have emission or bumpmap textures.
                if (hasEmissionOrBumpmap)
                {
                    SetupShaderStage(1, matrix);
                    RenderStage(_blendingMode, false);
                }
            }
            else
            {
                SetupShaderStage(2, matrix);
                RenderStage(_blendingMode, false);
            }
        }

        // Reset draw buffers
        _core.gl.DrawBuffers(3, [GlApi.GL_COLOR_ATTACHMENT0, GlApi.GL_COLOR_ATTACHMENT1, GlApi.GL_COLOR_ATTACHMENT2]);
        _core.gl.BlendEquation(GlApi.GL_FUNC_ADD);
    }

    /// <summary>
    /// Gets the active texture
    /// </summary>
    /// <returns></returns>
    public Texture ActiveTexture()
    {
        return Textures[0];
    }

    public override void RenderMask(bool dodge = false)
    {
        // Enable writing to stencil buffer and disable writing to color buffer
        _core.gl.ColorMask(false, false, false, false);
        _core.gl.StencilOp(GlApi.GL_KEEP, GlApi.GL_KEEP, GlApi.GL_REPLACE);
        _core.gl.StencilFunc(GlApi.GL_ALWAYS, dodge ? 0 : 1, 0xFF);
        _core.gl.StencilMask(0xFF);

        // Draw ourselves to the stencil buffer
        DrawSelf(true);

        // Disable writing to stencil buffer and enable writing to color buffer
        _core.gl.ColorMask(true, true, true, true);
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
                _offsetMaskThreshold *= value;
                return true;
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
            case "emissionStrength":
                _offsetEmissionStrength += value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "alphaThreshold" => _offsetMaskThreshold,
            "opacity" => _offsetOpacity,
            "tint.r" => _offsetTint.X,
            "tint.g" => _offsetTint.Y,
            "tint.b" => _offsetTint.Z,
            "screenTint.r" => _offsetScreenTint.X,
            "screenTint.g" => _offsetScreenTint.Y,
            "screenTint.b" => _offsetScreenTint.Z,
            "emissionStrength" => _offsetEmissionStrength,
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
        _offsetMaskThreshold = 0;
        _offsetOpacity = 1;
        _offsetTint = new(1, 1, 1);
        _offsetScreenTint = new(0, 0, 0);
        _offsetEmissionStrength = 1;
        base.BeginUpdate();
    }

    public override void Rebuffer(MeshData data)
    {
        base.Rebuffer(data);
        UpdateUVs();
    }

    public override void Draw()
    {
        if (!enabled) return;
        DrawOne();

        foreach (var child in Children)
        {
            child.Draw();
        }
    }

    public override void DrawOne()
    {
        if (!enabled) return;
        if (!Data.IsReady()) return; // Yeah, don't even try

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
        DrawSelf();
        base.DrawOne();
    }

    public override void DrawOneDirect(bool forMasking)
    {
        if (forMasking) DrawSelf(true);
        else DrawSelf(false);
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

    public override void SetOneTimeTransform(Matrix4x4 transform)
    {
        base.SetOneTimeTransform(transform);
        foreach (var m in _masks)
        {
            m.MaskSrc.OneTimeTransform = transform;
        }
    }

    public override void Dispose()
    {
        base.Dispose();
        if (_uvbo > 0)
        {
            _core.gl.DeleteBuffer(_uvbo);
        }
    }
}
