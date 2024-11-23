using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Fmt;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.Parts;

[TypeId("Part")]
public class Part : Drawable
{
    private uint uvbo;

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

    public override string TypeId()
    {
        return "Part";
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelf(JObject serializer, bool recursive = true)
    {
        base.SerializeSelf(serializer, recursive);
        if (FmtHelper.IsLoadingINP)
        {
            var list = new JArray();
            foreach (var texture in textures)
            {
                if (texture != null)
                {
                    var index = Puppet.getTextureSlotIndexFor(texture);
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

    protected override void Deserialize(JObject data)
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
        this.updateUVs();
    }

    public override void serializePartial(JObject obj, bool recursive = true)
    {
        base.serializePartial(obj, recursive);
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
        var temp = data.Uvs.ToArray();
        fixed (void* ptr = temp)
        {
            CoreHelper.gl.BufferData(GlApi.GL_ARRAY_BUFFER, data.Uvs.Count * Marshal.SizeOf<Vector2>(), new nint(ptr), GlApi.GL_STATIC_DRAW);
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

        var mModel = Puppet.transform.Matrix * matrix;
        var mViewProjection = CoreHelper.inCamera.Matrix();

        switch (stage)
        {
            case 0:
                // STAGE 1 - Advanced blending

                CoreHelper.gl.DrawBuffers(1, [GlApi.GL_COLOR_ATTACHMENT0]);

                PartHelper.partShaderStage1.use();
                PartHelper.partShaderStage1.setUniform(PartHelper.gs1offset, data.Origin);
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
                PartHelper.partShaderStage2.setUniform(PartHelper.gs2offset, data.Origin);
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
                PartHelper.partShader.setUniform(PartHelper.offset, data.Origin);
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
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, vbo);
        CoreHelper.gl.VertexAttribPointer(0, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable UVs array
        CoreHelper.gl.EnableVertexAttribArray(1); // uvs
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, uvbo);
        CoreHelper.gl.VertexAttribPointer(1, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Enable deform array
        CoreHelper.gl.EnableVertexAttribArray(2); // deforms
        CoreHelper.gl.BindBuffer(GlApi.GL_ARRAY_BUFFER, dbo);
        CoreHelper.gl.VertexAttribPointer(2, 2, GlApi.GL_FLOAT, false, 0, 0);

        // Bind index buffer
        bindIndex();

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
        if (overrideTransformMatrix != null)
            matrix = overrideTransformMatrix.Matrix;
        if (oneTimeTransform is { } mat)
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
            var mModel = Puppet.transform.Matrix * matrix;
            var mViewProjection = CoreHelper.inCamera.Matrix();

            PartHelper.partMaskShader.use();
            PartHelper.partMaskShader.setUniform(PartHelper.offset, data.Origin);
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

    /**
        Gets the active texture
    */
    Texture activeTexture()
    {
        return textures[0];
    }

    /**
        Constructs a new part
    */
    this(MeshData data, Texture[] textures, Node parent = null) {
        this(data, textures, inCreateUUID(), parent);
    }

    /**
        Constructs a new part
    */
    this(Node parent = null) {
        super(parent);

        version(InDoesRender) glGenBuffers(1, &uvbo);
    }

    /**
        Constructs a new part
    */
    this(MeshData data, Texture []
    textures, uint uuid, Node parent = null) {
        super(data, uuid, parent);
        foreach (i; 0..TextureUsage.COUNT) {
            if (i >= textures.length) break;
            this.textures[i] = textures[i];
        }

        version(InDoesRender) glGenBuffers(1, &uvbo);

        this.updateUVs();
    }
    
    override
    void renderMask(bool dodge = false) {

        // Enable writing to stencil buffer and disable writing to color buffer
        glColorMask(GL_FALSE, GL_FALSE, GL_FALSE, GL_FALSE);
        glStencilOp(GL_KEEP, GL_KEEP, GL_REPLACE);
        glStencilFunc(GL_ALWAYS, dodge ? 0 : 1, 0xFF);
        glStencilMask(0xFF);

        // Draw ourselves to the stencil buffer
        drawSelf!true();

        // Disable writing to stencil buffer and enable writing to color buffer
        glColorMask(GL_TRUE, GL_TRUE, GL_TRUE, GL_TRUE);
    }

    override
    bool hasParam(string key) {
        if (super.hasParam(key)) return true;

        switch (key)
        {
            case "alphaThreshold":
            case "opacity":
            case "tint.r":
            case "tint.g":
            case "tint.b":
            case "screenTint.r":
            case "screenTint.g":
            case "screenTint.b":
            case "emissionStrength":
                return true;
            default:
                return false;
        }
    }

    override
    float getDefaultValue(string key) {
        // Skip our list of our parent already handled it
        float def = super.getDefaultValue(key);
        if (!isNaN(def)) return def;

        switch (key)
        {
            case "alphaThreshold":
                return 0;
            case "opacity":
            case "tint.r":
            case "tint.g":
            case "tint.b":
                return 1;
            case "screenTint.r":
            case "screenTint.g":
            case "screenTint.b":
                return 0;
            case "emissionStrength":
                return 1;
            default: return float();
        }
    }

    override
    bool setValue(string key, float value) {

        // Skip our list of our parent already handled it
        if (super.setValue(key, value)) return true;

        switch (key)
        {
            case "alphaThreshold":
                offsetMaskThreshold *= value;
                return true;
            case "opacity":
                offsetOpacity *= value;
                return true;
            case "tint.r":
                offsetTint.x *= value;
                return true;
            case "tint.g":
                offsetTint.y *= value;
                return true;
            case "tint.b":
                offsetTint.z *= value;
                return true;
            case "screenTint.r":
                offsetScreenTint.x += value;
                return true;
            case "screenTint.g":
                offsetScreenTint.y += value;
                return true;
            case "screenTint.b":
                offsetScreenTint.z += value;
                return true;
            case "emissionStrength":
                offsetEmissionStrength += value;
                return true;
            default: return false;
        }
    }
    
    override
    float getValue(string key) {
        switch (key)
        {
            case "alphaThreshold": return offsetMaskThreshold;
            case "opacity": return offsetOpacity;
            case "tint.r": return offsetTint.x;
            case "tint.g": return offsetTint.y;
            case "tint.b": return offsetTint.z;
            case "screenTint.r": return offsetScreenTint.x;
            case "screenTint.g": return offsetScreenTint.y;
            case "screenTint.b": return offsetScreenTint.z;
            case "emissionStrength": return offsetEmissionStrength;
            default: return super.getValue(key);
        }
    }

    bool isMaskedBy(Drawable drawable) {
        foreach (mask; masks) {
            if (mask.maskSrc.uuid == drawable.uuid) return true;
        }
        return false;
    }

    ptrdiff_t getMaskIdx(Drawable drawable) {
        if (drawable is null) return -1;
        foreach (i, ref mask; masks) {
            if (mask.maskSrc.uuid == drawable.uuid) return i;
        }
        return -1;
    }

    ptrdiff_t getMaskIdx(uint uuid) {
        foreach (i, ref mask; masks) {
            if (mask.maskSrc.uuid == uuid) return i;
        }
        return -1;
    }

    override
    void beginUpdate() {
        offsetMaskThreshold = 0;
        offsetOpacity = 1;
        offsetTint = vec3(1, 1, 1);
        offsetScreenTint = vec3(0, 0, 0);
        offsetEmissionStrength = 1;
        super.beginUpdate();
    }
    
    override
    void rebuffer(ref MeshData data) {
        super.rebuffer(data);
        this.updateUVs();
    }

    override
    void draw() {
        if (!enabled) return;
        this.drawOne();

        foreach (child; children) {
            child.draw();
        }
    }

    override
    void drawOne() {
        version(InDoesRender) {
            if (!enabled) return;
            if (!data.isReady) return; // Yeah, don't even try

            size_t cMasks = maskCount;

            if (masks.length > 0)
            {
                import std.stdio: writeln;
                inBeginMask(cMasks > 0);

                foreach (ref mask; masks) {
                    mask.maskSrc.renderMask(mask.mode == MaskingMode.DodgeMask);
                }

                inBeginMaskContent();

                // We are the content
                this.drawSelf();

                inEndMask();
                return;
            }

            // No masks, draw normally
            this.drawSelf();
        }
        super.drawOne();
    }

    override
    void drawOneDirect(bool forMasking) {
        if (forMasking) this.drawSelf!true();
        else this.drawSelf!false();
    }

    override
    void finalize() {
        super.finalize();

        MaskBinding[] validMasks;
        foreach (i; 0..masks.length) {
            if (Drawable nMask = puppet.find!Drawable(masks[i].maskSrcUUID)) {
                masks[i].maskSrc = nMask;
                validMasks ~= masks[i];
            }
        }

        // Remove invalid masks
        masks = validMasks;
    }


    override
    void setOneTimeTransform(mat4* transform) {
        super.setOneTimeTransform(transform);
        foreach (m; masks) {
            m.maskSrc.oneTimeTransform = transform;
        }
    }
    }
