using System.Numerics;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.Core;

public sealed record Mesh
{
    /// <summary>
    /// Creates a mesh from a encoded Inochi2D MeshData structure.
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static Mesh FromMeshData(MeshData data)
    {
        return new Mesh(data);
    }

    private readonly VtxData[] _vtx;
    private readonly uint[] _idx;
    private readonly Vector2[] _vto;

    /// <summary>
    /// The points of the vertices of the mesh.
    /// </summary>
    /// <returns></returns>
    public Vector2[] Points => _vto;
    /// <summary>
    /// The vertex data stored in the mesh.
    /// </summary>
    public VtxData[] Vertices => _vtx;
    /// <summary>
    /// The index data stored in the mesh.
    /// </summary>
    public uint[] Indices => _idx;
    /// <summary>
    /// How many vertices are in the mesh.
    /// </summary>
    public int VertexCount => _vtx.Length;
    /// <summary>
    /// How many indices are in the mesh.
    /// </summary>
    public int ElementCount => _idx.Length;
    /// <summary>
    /// How many triangles are in the mesh.
    /// </summary>
    public int TriangleCount => _idx.Length / 3;
    /// <summary>
    /// Bounds of the deformed mesh.
    /// </summary>
    public Rect Bounds => Rect.GetBounds(_vto);

    /// <summary>
    /// Creates an empty mesh.
    /// </summary>
    public Mesh()
    {

    }

    /// <summary>
    /// Creates a mesh from a encoded Inochi2D MeshData structure.
    /// </summary>
    public Mesh(MeshData meshData)
    {
        _vtx = new VtxData[meshData.Vertices.Length];
        _idx = [.. meshData.Indices];
        _vto = [.. meshData.Vertices];

        for (int i = 0; i < _vtx.Length; i++)
        {
            _vtx[i] = new VtxData
            {
                Vtx = _vto[i],
                Uv = meshData.Uvs[i]
            };

            if (float.IsNaN(_vtx[i].Vtx.X) || float.IsNaN(_vtx[i].Vtx.Y))
            {

            }
        }
    }

    private Mesh(Mesh mesh)
    {
        _vtx = [.. mesh._vtx];
        _idx = [.. mesh._idx];
        _vto = [.. mesh._vto];
    }

    /// <summary>
    /// Gets the triangle in the mesh at the given offset.
    /// </summary>
    /// <param name="offset">The offset into the mesh.</param>
    /// <returns>The requested triangle.</returns>
    public Triangle GetTriangle(uint offset)
    {
        if (offset > _idx.Length / 3)
            return new Triangle();

        return new Triangle
        {
            P1 = _vto[_idx[(offset * 3) + 0]],
            P2 = _vto[_idx[(offset * 3) + 1]],
            P3 = _vto[_idx[(offset * 3) + 2]]
        };
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
                P1 = _vto[_idx[(i * 3) + 0]],
                P2 = _vto[_idx[(i * 3) + 1]],
                P3 = _vto[_idx[(i * 3) + 2]]
            };
        }
        return tris;
    }

    /// <summary>
    /// Makes a clone of this mesh.
    /// </summary>
    /// <returns>A new mesh with the data cloned.</returns>
    public Mesh Copy()
    {
        return new Mesh(this);
    }

    public MeshData ToMeshData()
    {
        return ToMeshData(this);
    }

    /// <summary>
    /// Converts a Mesh back into a MeshData.
    /// </summary>
    /// <param name="mesh">The mesh to convert.</param>
    /// <returns>A MeshData instance.</returns>
    public static MeshData ToMeshData(Mesh mesh)
    {
        var data = new MeshData
        {
            Indices = [.. mesh.Indices],
            Vertices = new Vector2[mesh.Vertices.Length],
            Uvs = new Vector2[mesh.Vertices.Length]
        };
        for (int i = 0; i < mesh.Vertices.Length; i++)
        {
            data.Vertices[i] = mesh.Vertices[i].Vtx;
            data.Uvs[i] = mesh.Vertices[i].Uv;
        }
        return data;
    }
}
