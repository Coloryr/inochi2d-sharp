namespace Inochi2dSharp.OpenGL;

public static class GLShaderCode
{
    public const string BasicFrag =
"""
#version 420
in vec2 texUVs;
in vec2 ndcTexCoords;

layout(location = 0) out vec4 outAlbedo;
layout(location = 1) out vec4 outEmission;
layout(location = 2) out vec4 outBumpmap;
layout(binding = 0) uniform sampler2D mask;
layout(binding = 1) uniform sampler2D albedo;
layout(binding = 2) uniform sampler2D emission;
layout(binding = 3) uniform sampler2D bumpmap;

layout(std140, binding = 0)
uniform iUniforms {
    vec3 tint;
    vec3 screenTint;
    float opacity;
};

vec4 screen(vec4 inColor, vec3 screenColor) {
    return vec4(vec3(1.0) - ((vec3(1.0)-inColor.rgb) * (vec3(1.0)-(screenColor*inColor.a))), inColor.a);
}

void main() {
    vec4 inAlbedo = texture(albedo, texUVs) * texture(mask, ndcTexCoords).rrrr;
    vec4 inEmission = texture(emission, texUVs);
    vec4 inBumpmap = texture(bumpmap, texUVs);
    vec4 multColor = vec4(tint, 1.0);

    outAlbedo = screen(inAlbedo, screenTint) * multColor;
    outEmission = inEmission * outAlbedo.aaaa;
    outBumpmap = inBumpmap * outAlbedo.aaaa;
}
""";

    public const string BasicVert =
"""
#version 330
uniform mat4 modelViewMatrix;

layout(location = 0) in vec2 verts;
layout(location = 1) in vec2 uvs;

out vec2 texUVs;
out vec2 ndcTexCoords;

void main() {
    texUVs = uvs;

    vec4 vertexCoords = modelViewMatrix * vec4(verts.x, verts.y, 0, 1);

    // Normalized device coordinates go from -1..+1,
    // but texture sampling goes from 0..1, so we need to
    // remap the ndc coordinates to texture coordinates.
    ndcTexCoords = (vertexCoords.xy * 0.5 + vertexCoords.w * 0.5) / vertexCoords.w;
    gl_Position = vertexCoords;
}
""";

    public const string MaskFrag =
"""
#version 420
in vec2 texUVs;

layout(location = 0) out float outMask;
layout(binding = 0) uniform sampler2D mask;

uniform int maskMode;

layout(std140, binding = 0)
uniform iUniforms {
    vec3 tint;
    vec3 screenTint;
    float opacity;
};

void main() {
    outMask = maskMode == 1 ? texture(mask, texUVs).a : 1-texture(mask, texUVs).a;
}
""";
    public const string MaskVert =
"""
#version 330
uniform mat4 modelViewMatrix;

layout(location = 0) in vec2 verts;
layout(location = 1) in vec2 uvs;

out vec2 texUVs;

void main() {
    texUVs = uvs;
    gl_Position = modelViewMatrix * vec4(verts.x, verts.y, 0, 1);
}
""";

}
