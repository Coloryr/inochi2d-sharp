using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes.Parts;

namespace Inochi2dSharp.Core.Nodes.Composites;

[TypeId("Composite")]
public class Composite : Node
{
    protected Part[] subParts;

    private void drawContents()
    {
        // Optimization: Nothing to be drawn, skip context switching
        if (subParts.Length == 0) return;

        inBeginComposite();

        foreach (var child in subParts) 
        {
            child.drawOne();
        }

        inEndComposite();
    }

    /// <summary>
    /// RENDERING
    /// </summary>
    private void drawSelf()
    {
        if (subParts.length == 0) return;

        glDrawBuffers(3, [GL_COLOR_ATTACHMENT0, GL_COLOR_ATTACHMENT1, GL_COLOR_ATTACHMENT2].ptr);

        cShader.use();
        cShader.setUniform(gopacity, clamp(offsetOpacity * opacity, 0, 1));
        incCompositePrepareRender();

        vec3 clampedColor = tint;
        if (!offsetTint.x.isNaN) clampedColor.x = clamp(tint.x * offsetTint.x, 0, 1);
        if (!offsetTint.y.isNaN) clampedColor.y = clamp(tint.y * offsetTint.y, 0, 1);
        if (!offsetTint.z.isNaN) clampedColor.z = clamp(tint.z * offsetTint.z, 0, 1);
        cShader.setUniform(gMultColor, clampedColor);

        clampedColor = screenTint;
        if (!offsetScreenTint.x.isNaN) clampedColor.x = clamp(screenTint.x + offsetScreenTint.x, 0, 1);
        if (!offsetScreenTint.y.isNaN) clampedColor.y = clamp(screenTint.y + offsetScreenTint.y, 0, 1);
        if (!offsetScreenTint.z.isNaN) clampedColor.z = clamp(screenTint.z + offsetScreenTint.z, 0, 1);
        cShader.setUniform(gScreenColor, clampedColor);
        inSetBlendMode(blendingMode, true);

        // Bind the texture
        glDrawArrays(GL_TRIANGLES, 0, 6);
    }

    private void selfSort()
    {
        import std.math: cmp;
        sort!((a, b) => cmp(
            a.zSort,
            b.zSort) > 0)(subParts);
    }

    private void scanPartsRecurse(ref Node node)
    {

        // Don't need to scan null nodes
        if (node is null) return;

        // Do the main check
        if (Part part = cast(Part)node) {
            subParts ~= part;
            foreach (child; part.children) {
                scanPartsRecurse(child);
            }

        } else
        {

            // Non-part nodes just need to be recursed through,
            // they don't draw anything.
            foreach (child; node.children) {
                scanPartsRecurse(child);
            }
        }
    }
}
