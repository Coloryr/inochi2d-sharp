using System.Numerics;

namespace Inochi2dSharp.Core.Nodes.Shape;

[TypeId("Shapes")]
public class Shapes(I2dCore core, Node? parent = null) : Node(core, parent)
{
    public const float MAX_DIST = 1.5f;
    /// <summary>
    /// A list of the shape offsets to apply per part
    /// </summary>
    private readonly Dictionary<Drawable, ShapeNode[]> _shapes = [];

    /// <summary>
    /// The cursor inside the Shapes node
    /// </summary>
    private Vector2 _selector;

    public override string TypeId()
    {
        return "Shapes";
    }

    public override void Update()
    {
        foreach (var item in _shapes)
        {
            var part = item.Key;
            var nodes = item.Value;
            int nodeLen = nodes.Length;
            float[] weights = new float[nodeLen];
            float accWeight = 0;

            // Calculate weighted average for each breakpoint
            for (int i = 0; i < nodes.Length; i++)
            {
                weights[i] = MAX_DIST - (Vector2.Distance(nodes[i].Breakpoint, _selector) / MAX_DIST);
                accWeight += weights[i];
            }

            // Acount for weights outside 1.0
            if (accWeight > 1)
            {
                for (int i = 0; i < weights.Length; i++)
                {
                    weights[i] /= nodeLen;
                }
            }

            // Make sure our vertices buffer is ready
            Vector2[] vertices = new Vector2[part.Vertices.Count];
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] = new Vector2(0);
            }

            // Apply our weighted offsets
            foreach (var node in nodes)
            {
                for (int i = 0; i < node.ShapeData.Length; i++)
                {
                    vertices[i] += weights[i] * node.ShapeData[i];
                }
            }
        }
    }
}
