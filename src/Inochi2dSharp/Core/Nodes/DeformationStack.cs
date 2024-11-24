using System.Numerics;

namespace Inochi2dSharp.Core.Nodes;

public class DeformationStack(Drawable parent)
{
    public void Push(Deformation deformation)
    {
        if (parent.Deformation.Length != deformation.VertexOffsets.Count)
        {
            throw new InvalidOperationException("Mismatched lengths");
        }

        for (int i = 0; i < parent.Deformation.Length; i++)
        {
            parent.Deformation[i] += deformation.VertexOffsets[i];
        }
        parent.NotifyDeformPushed(deformation);
    }

    public void PreUpdate()
    {
        for (int i = 0; i < parent.Deformation.Length; i++)
        {
            parent.Deformation[i] = new Vector2(0, 0);
        }
    }

    public void Update()
    {
        parent.RefreshDeform();
    }
}