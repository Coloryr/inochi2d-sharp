using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public class DeformationStack
{
    private Drawable parent;

    public DeformationStack(Drawable parent)
    {
        this.parent = parent;
    }

    public void Push(Deformation deformation)
    {
        if (parent.Deformation.Count != deformation.VertexOffsets.Count)
        {
            throw new InvalidOperationException("Mismatched lengths");
        }

        for (int i = 0; i < parent.Deformation.Count; i++)
        {
            parent.Deformation[i] += deformation.VertexOffsets[i];
        }
        parent.NotifyDeformPushed(deformation);
    }

    public void PreUpdate()
    {
        for (int i = 0; i < parent.Deformation.Count; i++)
        {
            parent.Deformation[i] = new Vec2(0, 0);
        }
    }

    public void Update()
    {
        parent.RefreshDeform();
    }
}