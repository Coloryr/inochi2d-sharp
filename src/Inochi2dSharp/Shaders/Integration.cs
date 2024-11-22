using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Shaders;

public static class Integration
{
    public static TextureBlob[] inCurrentPuppetTextureSlots;

    public const string inPartMaskShader = 
"""
#version 330
in vec2 texUVs;
in vec4 vertexCoord;

out vec4 outColor;

uniform sampler2D tex;
uniform float threshold;

void main() {
    vec4 color = texture(tex, texUVs);
    if (color.a <= threshold) discard;
    outColor = vec4(1, 1, 1, 1);
}
""";
    public const string inPartFragmentShader = 
"""
#version 330
in vec2 texUVs;
in vec4 vertexCoord;

layout(location = 0) out vec4 outAlbedo;
layout(location = 1) out vec4 outEmissive;
layout(location = 2) out vec4 outBump;

uniform sampler2D albedo;
uniform sampler2D emissive;
uniform sampler2D bumpmap;

uniform mat4 mvpModel;
uniform mat4 mvpViewProjection;
uniform float opacity;
uniform vec3 multColor;
uniform vec3 screenColor;
uniform float emissionStrength = 1;

vec4 screen(vec3 tcol, float a) {
    return vec4(vec3(1.0) - ((vec3(1.0)-tcol) * (vec3(1.0)-(screenColor*a))), a);
}

void main() {
    // Sample texture
    vec4 texColor = texture(albedo, texUVs);
    vec4 emiColor = texture(emissive, texUVs);

    vec4 mult = vec4(multColor.xyz, 1);

    // Bumpmapping orientation
    vec4 origin = mvpModel * vec4(0, 0, 0, 1);
    vec4 bumpAngle = texture(bumpmap, texUVs) * 2.0 - 1.0;
    vec4 normal = mvpModel * (vec4(-bumpAngle.x, bumpAngle.yz, 1.0));
    normal = normalize(normal-origin) * 0.5 + 0.5;

    // Out color math
    vec4 albedoOut = screen(texColor.xyz, texColor.a) * mult;
    vec4 emissionOut = screen(emiColor.xyz, texColor.a) * mult * emissionStrength;

    // Albedo
    outAlbedo = albedoOut * opacity;

    // Emissive
    outEmissive = emissionOut * outAlbedo.a;

    // Bumpmap
    outBump = normal * outAlbedo.a;
}
""";
    public const string inPartVertexShader =
"""
#version 330
uniform mat4 mvpModel;
uniform mat4 mvpViewProjection;
uniform vec2 offset;

layout(location = 0) in vec2 verts;
layout(location = 1) in vec2 uvs;
layout(location = 2) in vec2 deform;

out vec2 texUVs;
out vec4 vertexCoord;

void main() {
    vertexCoord = mvpModel * 
                    vec4(verts.x-offset.x+deform.x, verts.y-offset.y+deform.y, 0, 1);
    texUVs = uvs;
    gl_Position = mvpViewProjection * vertexCoord;
}
""";

    public const string inCompositeMaskShader =
"""
#version 330
in vec2 texUVs;
out vec4 outColor;

uniform sampler2D tex;
uniform float threshold;
uniform float opacity;

void main() {
    vec4 color = texture(tex, texUVs) * vec4(1, 1, 1, opacity);
    if (color.a <= threshold) discard;
    outColor = vec4(1, 1, 1, 1);
}
""";
    public const string inCompositeFragmentShader =
"""
#version 330
in vec2 texUVs;

layout(location = 0) out vec4 outAlbedo;
layout(location = 1) out vec4 outEmissive;
layout(location = 2) out vec4 outBump;

uniform sampler2D albedo;
uniform sampler2D emissive;
uniform sampler2D bumpmap;

uniform float opacity;
uniform vec3 multColor;
uniform vec3 screenColor;

vec4 screen(vec3 tcol, float a) {
    return vec4(vec3(1.0) - ((vec3(1.0)-tcol) * (vec3(1.0)-(screenColor*a))), a);
}

void main() {
    // Sample texture
    vec4 texColor = texture(albedo, texUVs);
    vec4 emiColor = texture(emissive, texUVs);
    vec4 bmpColor = texture(bumpmap, texUVs);

    vec4 mult = vec4(multColor.xyz, 1);

    // Out color math
    vec4 albedoOut = screen(texColor.xyz, texColor.a) * mult;
    vec4 emissionOut = screen(emiColor.xyz, texColor.a) * mult;

    // Albedo
    outAlbedo = albedoOut * opacity;

    // Emissive
    outEmissive = emissionOut;

    // Bumpmap
    outBump = bmpColor;
}
""";
    public const string inCompositeVertexShader =
"""
#version 330

out vec2 texUVs;

vec2 verts[6] = vec2[](
    vec2(-1, -1),
    vec2(-1, 1),
    vec2(1, -1),

    vec2(1, -1),
    vec2(-1, 1),
    vec2(1, 1)
);

vec2 uvs[6] = vec2[](
    vec2(0, 0),
    vec2(0, 1),
    vec2(1, 0),

    vec2(1, 0),
    vec2(0, 1),
    vec2(1, 1)
);

void main() {
    gl_Position = vec4(verts[gl_VertexID], 0, 1);
    texUVs = uvs[gl_VertexID];
}
""";

    public const string inMaskFragmentShader =
"""
#version 330
out vec4 outColor;

void main() {
    outColor = vec4(0, 0, 0, 1);
}
""";
    public const string inMaskVertexShader =
"""
#version 330
uniform mat4 mvpModel;
uniform mat4 mvpView;
uniform mat4 mvpProjection;
uniform vec2 offset;
layout(location = 0) in vec2 verts;

out vec2 texUVs;

void main() {
    gl_Position = mvpProjection * mvpView * mvpModel * vec4(verts.x-offset.x, verts.y-offset.y, 0, 1);
}
""";

    public const string inDebugVert =
"""
#version 330
uniform mat4 mvp;
layout(location = 0) in vec3 verts;

out vec2 texUVs;

void main() {
    gl_Position = mvp * vec4(verts.x, verts.y, verts.z, 1);
}
""";
    public const string inDebugLineFrag =
"""
#version 330
layout(location = 0) out vec4 outColor;

uniform vec4 color;

void main() {
    outColor = color;
}
""";
    public const string inDebugPointFrag =
"""
#version 330
layout(location = 0) out vec4 outColor;

uniform vec4 color;

void main() {
    float r = 0.0; // radius
    float alpha = 1.0; // alpha

    // r = point in circle compared against circle raidus
    vec2 cxy = 2.0 * gl_PointCoord - 1.0;
    r = dot(cxy, cxy);

    // epsilon width
    float epsilon = fwidth(r)*0.5;

    // apply delta
    alpha = 1.0 - smoothstep(1.0 - epsilon, 1.0 + epsilon, r);
    outColor = color * alpha;
}
""";
    public const string inSceneFrag =
"""
#version 330
in vec2 texUVs;

out vec4 outColor;

uniform sampler2D fbo;

void main() {
    // Set color to the corrosponding pixel in the FBO
    vec4 color = texture(fbo, texUVs);
    outColor = vec4(color.r, color.g, color.b, color.a);
}
""";
    public const string inScencVert =
"""
#version 330
uniform mat4 mvpModel;
uniform mat4 mvpView;
uniform mat4 mvpProjection;

layout(location = 0) in vec2 verts;
layout(location = 1) in vec2 uvs;

out vec2 texUVs;
out vec3 viewPosition;
out vec3 fragPosition;

void main() {
    fragPosition = vec3(verts.x, verts.y, 0);
    viewPosition = (mvpModel * vec4(fragPosition, 1.0)).xyz;
    texUVs = uvs;

    gl_Position = mvpProjection * mvpView * mvpModel * vec4(fragPosition, 1.0);
}
""";
    public const string inLighingFrag =
"""
#version 330
in vec2 texUVs;
in vec3 viewPosition;
in vec3 fragPosition;

layout(location = 0) out vec4 outAlbedo;
layout(location = 1) out vec4 outEmissive;
layout(location = 2) out vec4 outBump;

uniform vec3 ambientLight;
uniform vec3 lightColor;
uniform vec3 lightDirection;
uniform vec2 fbSize;

uniform sampler2D albedo;
uniform sampler2D emissive;
uniform sampler2D bumpmap;
uniform int LOD = 2;
uniform int samples = 25;

// Gaussian
float gaussian(vec2 i, float sigma) {
    return exp(-0.5*dot(i /= sigma, i)) / (6.28*sigma*sigma);
}

// Bloom texture by blurring it
vec4 bloom(sampler2D sp, vec2 uv, vec2 scale) {
    float sigma = float(samples) * 0.25;
    vec4 out_ = vec4(0);
    int sLOD = 1 << LOD;
    int s = samples/sLOD;

    for ( int i = 0; i < s*s; i++ ) {
        vec2 d = vec2(i%s, i/s)*float(sLOD) - float(samples)/2.0;
        out_ += gaussian(d, sigma) * textureLod( sp, uv + scale * d, LOD);
    }

    return out_ / out_.a;
}

// Normal mapping using blinn-phong
// This function takes a light and shadow color
// This allows coloring the shadowed parts.
vec4 normalMapping(vec3 bump, vec4 albedo, vec3 light, vec3 ambientLight) {
    vec3 lightDir = vec3(lightDirection.xy, clamp(-lightDirection.z, -1, 1));
    vec3 viewDir = vec3(0, 0, -1);
    vec3 halfwayDir = normalize(lightDir + viewDir);
    vec3 normal = normalize((bump * 2.0) - 1.0);

    // Callculate diffuse factor
    float diff = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = light * diff;

    // Calculate specular factor.
    float spec = pow(max(dot(normal, halfwayDir), 0.0), 0.5);
    vec3 specular = light * spec;

    // Calculate the object color
    vec4 objectColor = vec4((ambientLight + diffuse + specular), 1.0) * albedo;

    // Mix between the shadow color and calculated light
    // via linear interpolation
    return vec4(objectColor.rgb, albedo.a);
}

void main() {

    // Bloom
    outEmissive = texture(emissive, texUVs)+bloom(emissive, texUVs, 1.0/fbSize);
    outBump = texture(bumpmap, texUVs);

    vec4 albedo = texture(albedo, texUVs);
    vec4 emission = albedo * outEmissive;
    vec4 bump = normalMapping(outBump.rgb, albedo, lightColor, ambientLight);

    vec4 final = vec4((bump + emission).rgb, bump.a);
    outAlbedo = final;
}
""";
    public const string inCompositeVert =
"""
#version 330

out vec2 texUVs;

vec2 verts[6] = vec2[](
    vec2(-1, -1),
    vec2(-1, 1),
    vec2(1, -1),

    vec2(1, -1),
    vec2(-1, 1),
    vec2(1, 1)
);

vec2 uvs[6] = vec2[](
    vec2(0, 0),
    vec2(0, 1),
    vec2(1, 0),

    vec2(1, 0),
    vec2(0, 1),
    vec2(1, 1)
);

void main() {
    gl_Position = vec4(verts[gl_VertexID], 0, 1);
    texUVs = uvs[gl_VertexID];
}
""";
    public const string inCompositeFrag =
"""
#version 330
in vec2 texUVs;

layout(location = 0) out vec4 outAlbedo;
layout(location = 1) out vec4 outEmissive;
layout(location = 2) out vec4 outBump;

uniform sampler2D albedo;
uniform sampler2D emissive;
uniform sampler2D bumpmap;

uniform float opacity;
uniform vec3 multColor;
uniform vec3 screenColor;

vec4 screen(vec3 tcol, float a) {
    return vec4(vec3(1.0) - ((vec3(1.0)-tcol) * (vec3(1.0)-(screenColor*a))), a);
}

void main() {
    // Sample texture
    vec4 texColor = texture(albedo, texUVs);
    vec4 emiColor = texture(emissive, texUVs);
    vec4 bmpColor = texture(bumpmap, texUVs);

    vec4 mult = vec4(multColor.xyz, 1);

    // Out color math
    vec4 albedoOut = screen(texColor.xyz, texColor.a) * mult;
    vec4 emissionOut = screen(emiColor.xyz, texColor.a) * mult;

    // Albedo
    outAlbedo = albedoOut * opacity;

    // Emissive
    outEmissive = emissionOut;

    // Bumpmap
    outBump = bmpColor;
}
""";
    public const string inCompositeMaskFrag =
"""
#version 330
in vec2 texUVs;
out vec4 outColor;

uniform sampler2D tex;
uniform float threshold;
uniform float opacity;

void main() {
    vec4 color = texture(tex, texUVs) * vec4(1, 1, 1, opacity);
    if (color.a <= threshold) discard;
    outColor = vec4(1, 1, 1, 1);
}
""";
}
