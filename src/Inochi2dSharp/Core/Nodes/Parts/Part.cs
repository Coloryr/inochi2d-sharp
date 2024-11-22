using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
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
    public int[] textureIds;

    /// <summary>
    /// List of masks to apply
    /// </summary>
    public MaskBinding[] masks;

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

    public override string typeId()
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
        if (inIsINPMode())
        {
            var list = new JArray();
            foreach (var texture in textures) 
            {
                if (texture != null)
                {
                    var index = Puppet.getTextureSlotIndexFor(texture);
                    if (index >= 0)
                    {
                        serializer.elemBegin;
                        serializer.putValue(cast(size_t)index);
                    }
                    else
                    {
                        serializer.elemBegin;
                        serializer.putValue(cast(size_t)NO_TEXTURE);
                    }
                }
                else
                {
                    serializer.elemBegin;
                    serializer.putValue(cast(size_t)NO_TEXTURE);
                }
            }
            serializer.Add("textures", list);
        }


        serializer.putKey("blend_mode");
        serializer.serializeValue(blendingMode);

        serializer.putKey("tint");
        tint.serialize(serializer);

        serializer.putKey("screenTint");
        screenTint.serialize(serializer);

        serializer.putKey("emissionStrength");
        serializer.serializeValue(emissionStrength);

        if (masks.length > 0)
        {
            serializer.putKey("masks");
            auto state = serializer.listBegin();
            foreach (m; masks) {
                serializer.elemBegin;
                serializer.serializeValue(m);
            }
            serializer.listEnd(state);
        }

        serializer.putKey("mask_threshold");
        serializer.putValue(maskAlphaThreshold);

        serializer.putKey("opacity");
        serializer.putValue(opacity);
    }

    protected override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        if (inIsINPMode())
        {
            size_t i;
            foreach (texElement; data["textures"].byElement) {
                uint textureId;
                texElement.deserializeValue(textureId);

                // uint max = no texture set
                if (textureId == NO_TEXTURE) continue;

                textureIds ~= textureId;
                this.textures[i++] = inGetTextureFromId(textureId);
            }
        }
        else
        {
            enforce(0, "Loading from texture path is deprecated.");
        }

        data["opacity"].deserializeValue(this.opacity);
        data["mask_threshold"].deserializeValue(this.maskAlphaThreshold);

        // Older models may not have tint
        if (!data["tint"].isEmpty) deserialize(tint, data["tint"]);

        // Older models may not have screen tint
        if (!data["screenTint"].isEmpty) deserialize(screenTint, data["screenTint"]);

        // Older models may not have emission
        if (!data["emissionStrength"].isEmpty) deserialize(tint, data["emissionStrength"]);

        // Older models may not have blend mode
        if (!data["blend_mode"].isEmpty) data["blend_mode"].deserializeValue(this.blendingMode);

        if (!data["masked_by"].isEmpty)
        {
            MaskingMode mode;
            data["mask_mode"].deserializeValue(mode);

            // Go every masked part
            foreach (imask; data["masked_by"].byElement) {
                uint uuid;
                if (auto exc = imask.deserializeValue(uuid)) return exc;
                this.masks ~= MaskBinding(uuid, mode, null);
            }
        }

        if (!data["masks"].isEmpty)
        {
            data["masks"].deserializeValue(this.masks);
        }

        // Update indices and vertices
        this.updateUVs();
    }

    override
    void serializePartial(ref InochiSerializer serializer, bool recursive = true)
    {
        super.serializePartial(serializer, recursive);
        serializer.putKey("textureUUIDs");
        auto state = serializer.listBegin();
        foreach (ref texture; textures) {
            uint uuid;
            if (texture! is null)
            {
                uuid = texture.getRuntimeUUID();
            }
            else
            {
                uuid = InInvalidUUID;
            }
            serializer.elemBegin;
            serializer.putValue(cast(size_t)uuid);
        }
        serializer.listEnd(state);
    }

    // TODO: Cache this
    protected size_t maskCount()
    {
        size_t c;
        foreach (m; masks) if (m.mode == MaskingMode.Mask) c++;
        return c;
    }

    protected size_t dodgeCount()
    {
        size_t c;
        foreach (m; masks) if (m.mode == MaskingMode.DodgeMask) c++;
        return c;
    }

    private void updateUVs()
    {
        version(InDoesRender) {
            glBindBuffer(GL_ARRAY_BUFFER, uvbo);
            glBufferData(GL_ARRAY_BUFFER, data.uvs.length * vec2.sizeof, data.uvs.ptr, GL_STATIC_DRAW);
        }
    }

    private void setupShaderStage(int stage, mat4 matrix)
    {

        vec3 clampedTint = tint;
        if (!offsetTint.x.isNaN) clampedTint.x = clamp(tint.x * offsetTint.x, 0, 1);
        if (!offsetTint.y.isNaN) clampedTint.y = clamp(tint.y * offsetTint.y, 0, 1);
        if (!offsetTint.z.isNaN) clampedTint.z = clamp(tint.z * offsetTint.z, 0, 1);

        vec3 clampedScreen = screenTint;
        if (!offsetScreenTint.x.isNaN) clampedScreen.x = clamp(screenTint.x + offsetScreenTint.x, 0, 1);
        if (!offsetScreenTint.y.isNaN) clampedScreen.y = clamp(screenTint.y + offsetScreenTint.y, 0, 1);
        if (!offsetScreenTint.z.isNaN) clampedScreen.z = clamp(screenTint.z + offsetScreenTint.z, 0, 1);

        mat4 mModel = puppet.transform.matrix * matrix;
        mat4 mViewProjection = inGetCamera().matrix;

        switch (stage)
        {
            case 0:
                // STAGE 1 - Advanced blending

                glDrawBuffers(1, [GL_COLOR_ATTACHMENT0].ptr);

                partShaderStage1.use();
                partShaderStage1.setUniform(gs1offset, data.origin);
                partShaderStage1.setUniform(gs1MvpModel, mModel);
                partShaderStage1.setUniform(gs1MvpViewProjection, mViewProjection);
                partShaderStage1.setUniform(gs1opacity, clamp(offsetOpacity * opacity, 0, 1));

                partShaderStage1.setUniform(partShaderStage1.getUniformLocation("albedo"), 0);
                partShaderStage1.setUniform(gs1MultColor, clampedTint);
                partShaderStage1.setUniform(gs1ScreenColor, clampedScreen);
                inSetBlendMode(blendingMode, false);
                break;
            case 1:

                // STAGE 2 - Basic blending (albedo, bump)
                glDrawBuffers(2, [GL_COLOR_ATTACHMENT1, GL_COLOR_ATTACHMENT2].ptr);

                partShaderStage2.use();
                partShaderStage2.setUniform(gs2offset, data.origin);
                partShaderStage2.setUniform(gs2MvpModel, mModel);
                partShaderStage2.setUniform(gs2MvpViewProjection, mViewProjection);
                partShaderStage2.setUniform(gs2opacity, clamp(offsetOpacity * opacity, 0, 1));
                partShaderStage2.setUniform(gs2EmissionStrength, emissionStrength * offsetEmissionStrength);

                partShaderStage2.setUniform(partShaderStage2.getUniformLocation("emission"), 0);
                partShaderStage2.setUniform(partShaderStage2.getUniformLocation("bump"), 1);

                // These can be reused from stage 2
                partShaderStage1.setUniform(gs2MultColor, clampedTint);
                partShaderStage1.setUniform(gs2ScreenColor, clampedScreen);
                inSetBlendMode(blendingMode, true);
                break;
            case 2:

                // Basic blending
                glDrawBuffers(3, [GL_COLOR_ATTACHMENT0, GL_COLOR_ATTACHMENT1, GL_COLOR_ATTACHMENT2].ptr);

                partShader.use();
                partShader.setUniform(offset, data.origin);
                partShader.setUniform(mvpModel, mModel);
                partShader.setUniform(mvpViewProjection, mViewProjection);
                partShader.setUniform(gopacity, clamp(offsetOpacity * opacity, 0, 1));
                partShader.setUniform(gEmissionStrength, emissionStrength * offsetEmissionStrength);

                partShader.setUniform(partShader.getUniformLocation("albedo"), 0);
                partShader.setUniform(partShader.getUniformLocation("emissive"), 1);
                partShader.setUniform(partShader.getUniformLocation("bumpmap"), 2);

                vec3 clampedColor = tint;
                if (!offsetTint.x.isNaN) clampedColor.x = clamp(tint.x * offsetTint.x, 0, 1);
                if (!offsetTint.y.isNaN) clampedColor.y = clamp(tint.y * offsetTint.y, 0, 1);
                if (!offsetTint.z.isNaN) clampedColor.z = clamp(tint.z * offsetTint.z, 0, 1);
                partShader.setUniform(gMultColor, clampedColor);

                clampedColor = screenTint;
                if (!offsetScreenTint.x.isNaN) clampedColor.x = clamp(screenTint.x + offsetScreenTint.x, 0, 1);
                if (!offsetScreenTint.y.isNaN) clampedColor.y = clamp(screenTint.y + offsetScreenTint.y, 0, 1);
                if (!offsetScreenTint.z.isNaN) clampedColor.z = clamp(screenTint.z + offsetScreenTint.z, 0, 1);
                partShader.setUniform(gScreenColor, clampedColor);
                inSetBlendMode(blendingMode, true);
                break;
            default: return;
        }

    }

    private void renderStage(BlendMode mode, bool advanced = true) {

        // Enable points array
        glEnableVertexAttribArray(0);
        glBindBuffer(GL_ARRAY_BUFFER, vbo);
        glVertexAttribPointer(0, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Enable UVs array
        glEnableVertexAttribArray(1); // uvs
        glBindBuffer(GL_ARRAY_BUFFER, uvbo);
        glVertexAttribPointer(1, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Enable deform array
        glEnableVertexAttribArray(2); // deforms
        glBindBuffer(GL_ARRAY_BUFFER, dbo);
        glVertexAttribPointer(2, 2, GL_FLOAT, GL_FALSE, 0, null);

        // Bind index buffer
        this.bindIndex();

        // Disable the vertex attribs after use
        glDisableVertexAttribArray(0);
        glDisableVertexAttribArray(1);
        glDisableVertexAttribArray(2);

        if (advanced) {
            // Blending barrier
            inBlendModeBarrier(mode);
        }
    }

    /*
        RENDERING
    */
    private void drawSelf(bool isMask = false) {

        // In some cases this may happen
        if (textures.length == 0) return;

        // Bind the vertex array
        incDrawableBindVAO();

        // Calculate matrix
        mat4 matrix = transform.matrix();
        if (overrideTransformMatrix! is null)
            matrix = overrideTransformMatrix.matrix;
        if (oneTimeTransform! is null)
            matrix = (*oneTimeTransform) * matrix;

        // Make sure we check whether we're already bound
        // Otherwise we're wasting GPU resources
        if (boundAlbedo != textures[0])
        {

            // Bind the textures
            foreach (i, ref texture; textures) {
                if (texture) texture.bind(cast(uint)i);
                else
                {

                    // Disable texture when none is there.
                    glActiveTexture(GL_TEXTURE0 + cast(uint)i);
                    glBindTexture(GL_TEXTURE_2D, 0);
                }
            }
        }

        if (isMask)
        {

            mat4 mModel = puppet.transform.matrix * matrix;
            mat4 mViewProjection = inGetCamera().matrix;

            partMaskShader.use();
            partMaskShader.setUniform(offset, data.origin);
            partMaskShader.setUniform(mMvpModel, mModel);
            partMaskShader.setUniform(mMvpViewProjection, mViewProjection);
            partMaskShader.setUniform(mthreshold, clamp(offsetMaskThreshold + maskAlphaThreshold, 0, 1));

            // Make sure the equation is correct
            glBlendEquation(GL_FUNC_ADD);
            glBlendFunc(GL_ONE, GL_ONE_MINUS_SRC_ALPHA);

            renderStage!false(blendingMode);
        }
        else
        {

            bool hasEmissionOrBumpmap = (textures[1] || textures[2]);

            if (inUseMultistageBlending(blendingMode))
            {

                // TODO: Detect if this Part is NOT in a composite,
                // If so, we can relatively safely assume that we may skip stage 1.
                setupShaderStage(0, matrix);
                renderStage(blendingMode);

                // Only do stage 2 if we have emission or bumpmap textures.
                if (hasEmissionOrBumpmap)
                {
                    setupShaderStage(1, matrix);
                    renderStage!false(blendingMode);
                }
            }
            else
            {
                setupShaderStage(2, matrix);
                renderStage!false(blendingMode);
            }
        }

        // Reset draw buffers
        glDrawBuffers(3, [GL_COLOR_ATTACHMENT0, GL_COLOR_ATTACHMENT1, GL_COLOR_ATTACHMENT2].ptr);
        glBlendEquation(GL_FUNC_ADD);
    }
}
