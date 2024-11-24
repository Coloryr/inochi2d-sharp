﻿using System.Numerics;

namespace Inochi2dSharp.Core.Nodes.Shape;

[TypeId("Shapes")]
public class Shapes : Node
{
    public const float MAX_DIST = 1.5f;
    /// <summary>
    /// A list of the shape offsets to apply per part
    /// </summary>
    public Dictionary<Drawable, ShapeNode[]> shapes;

    /// <summary>
    /// The cursor inside the Shapes node
    /// </summary>
    public Vector2 selector;

    public Shapes() : this(null)
    {

    }

    public Shapes(Node? parent = null) : base(parent)
    {

    }

    public override string TypeId()
    {
        return "Shapes";
    }

    public override void Update()
    {
        foreach (var item in shapes)
        {
            var part = item.Key;
            var nodes = item.Value;
            int nodeLen = nodes.Length;
            float[] weights = new float[nodeLen];
            float accWeight = 0;

            // Calculate weighted average for each breakpoint
            for (int i = 0; i < nodes.Length; i++)
            {
                weights[i] = MAX_DIST - (Vector2.Distance(nodes[i].breakpoint, selector) / MAX_DIST);
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
                for (int i = 0; i < node.shapeData.Length; i++)
                {
                    vertices[i] += weights[i] * node.shapeData[i];
                }
            }
        }
    }
}