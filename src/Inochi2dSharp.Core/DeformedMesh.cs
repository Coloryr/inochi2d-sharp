using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.Core;

/// <summary>
/// A mesh which recieves deformation data from the outside.
/// </summary>
public class DeformedMesh
{
    private Mesh parent_;
    private VtxData[] deformed_;
    private Vector2[] delta_;

    /// <summary>
    /// The parent of the deformed mesh.
    /// </summary>
    public Mesh Parent
    {
        get
        {
            return parent_;
        }
        set
        {
            parent_ = value;
            Array.Resize(ref deformed_, value.Points.Length);
            Array.Resize(ref delta_, value.Points.Length);
        }
    }

    /// <summary>
    /// The deformed points of the mesh.
    /// </summary>
    public Vector2[] Points => delta_;
    /// <summary>
    /// The deformed vertices of the mesh.
    /// </summary>
    public VtxData[] Vertices => deformed_;
    /// <summary>
    /// The indices for the mesh.
    /// </summary>
    /// <returns></returns>
    public uint[] Indices => Parent.Indices;
    /// <summary>
    /// How many vertices are in the mesh.
    /// </summary>
    /// <returns></returns>
    public int VertexCount => deformed_.Length;
    /// <summary>
    /// How many indices are in the mesh.
    /// </summary>
    public int ElementCount => parent_.ElementCount;
    /// <summary>
    /// How many triangles are in the mesh.
    /// </summary>
    public int TriangleCount => parent_.TriangleCount;
    /// <summary>
    /// Bounds of the deformed mesh.
    /// </summary>
    public Rect Bounds => Rect.GetBounds(delta_);

    /// <summary>
    /// Constructs a new empty DeformedMesh
    /// </summary>
    public DeformedMesh()
    {

    }
    /// <summary>
    /// Constructs a new DeformedMesh
    /// </summary>
    /// <param name="parent"></param>
    public DeformedMesh(Mesh parent)
    {
        parent_ = parent;

        deformed_ = new VtxData[parent.Points.Length];
        delta_ = new Vector2[parent.Points.Length];
    }

    /// <summary>
    /// Deform the mesh by the given amount.
    /// </summary>
    /// <param name="by">The deltas to deform the mesh by</param>
    public void Deform(Vector2[] by)
    {
        for (int i = 0; i < delta_.Length; i++)
        {
            delta_[i] += by[i];

            deformed_[i].Vtx.X = delta_[i].X;
            deformed_[i].Vtx.Y = delta_[i].Y;
        }
    }
    /// <summary>
    /// Deforms a single vertex within the mesh by the given amount.
    /// </summary>
    /// <param name="offset">Offset into the mesh to deform.</param>
    /// <param name="by">The delta to deform the mesh by</param>
    public void Deform(int offset, Vector2 by)
    {
        if (offset >= delta_.Length)
            return;

        delta_[offset] += by;
        deformed_[offset].Vtx.X = delta_[offset].X;
        deformed_[offset].Vtx.Y = delta_[offset].Y;
    }

    /// <summary>
    /// Pushes a matrix to the deformed mesh.
    /// </summary>
    /// <param name="matrix"></param>
    public void PushMatrix(Matrix4x4 matrix)
    {
        // NOTE: SIMD is slower in this instance due to how multiple arrays
        // are involved.
        for (int i = 0; i < delta_.Length; i++)
        {
            delta_[i] += matrix.Multiply(new Vector4(delta_[i].X, delta_[i].Y, 0, 1)).GetXY();

            deformed_[i].Vtx.X = delta_[i].X;
            deformed_[i].Vtx.Y = delta_[i].Y;
        }
    }

    /// <summary>
    /// Gets an array of every triangle in the mesh.
    /// </summary>
    /// <returns>A array of triangles</returns>
    public Triangle[] GetTriangles()
    {
        Triangle[] tris = new Triangle[TriangleCount];
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = new Triangle
            {
                P1 = delta_[parent_.Indices[(i * 3) + 0]],
                P2 = delta_[parent_.Indices[(i * 3) + 1]],
                P3 = delta_[parent_.Indices[(i * 3) + 2]]
            };
        }
        return tris;
    }

    /// <summary>
    /// Resets the deformation.
    /// </summary>
    public void Reset()
    {
        deformed_ = [.. parent_.Vertices];
        delta_ = [.. parent_.Points];
    }
}

