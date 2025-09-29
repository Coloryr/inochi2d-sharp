using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Deformers;

/// <summary>
/// A deformer which deforms child nodes stored within it,
/// </summary>
[TypeId("MeshGroup", 0x0102)]
public class MeshDeformer : Deformer
{
    private Mesh _mesh;
    private DeformedMesh _base;
    private DeformedMesh _deformed;
    private Vector2[] _deformDeltas = [];

    public Mesh Mesh
    {
        get
        {
            return _mesh;
        }
        set
        {
            if (value == _mesh)
                return;

            _mesh = value;
            Array.Resize(ref _deformDeltas, _mesh.VertexCount);

            _base.Parent = value;
            _deformed.Parent = value;

            _base.Reset();
            _base.PushMatrix(Transform(false).Matrix);
        }
    }

    /// <summary>
    /// The control points of the deformer.
    /// </summary>
    public override Vector2[] ControlPoints
    {
        get
        {
            return _deformed.Points;
        }
        set
        {
            var m = int.Min(value.Length, _deformed.Points.Length);
            Array.Copy(value, _deformed.Points, m);
        }
    }

    /// <summary>
    /// The base position of the deformable's points, in world space.
    /// </summary>
    public override Vector2[] BasePoints => _base.Points;
    /// <summary>
    /// The points which may be deformed by a deformer, in world space.
    /// </summary>
    public override Vector2[] DeformPoints => _deformed.Points;

    /// <summary>
    /// Constructs a new MeshGroup node
    /// </summary>
    public MeshDeformer(Node? parent = null) : base(parent)
    { 
        
    }

    /// <summary>
    /// Deforms the IDeformable.
    /// </summary>
    /// <param name="deformed">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public override void Deform(Vector2[] deformed, bool absolute = false)
    {
        _deformed.Deform(deformed);
    }

    /// <summary>
    /// Resets the deformation for the IDeformable.
    /// </summary>
    public override void ResetDeform()
    {
        _deformed.Reset();
        _base.Reset();
    }

    public override void PreUpdate(DrawList drawList)
    {
        base.PreUpdate(drawList);
        ResetDeform();
    }

    public override  void Update(float delta, DrawList drawList)
    {
        _base.PushMatrix(WorldTransform.Matrix);
        _deformed.PushMatrix(WorldTransform.Matrix);
        base.Update(delta, drawList);
    }

    /// <summary>
    /// Updates the internal transformation matrix to apply to children.
    /// </summary>
    /// <param name="drawList"></param>
    public override void PostUpdate(DrawList drawList)
    {
        // No deltas?
        if (_deformDeltas.Length == 0)
        {
            base.PostUpdate(drawList);
            return;
        }

        // Calculate the deltas from the world matrix.
        for (int i = 0; i < _deformDeltas.Length; i++)
            _deformDeltas[i] = _base.Points[i] - _deformed.Points[i];

        // Use the weights to deform each subpoint by a delta determined
        // by the weight to each vertex in their triangle.
        for (int i = 0; i < ToDeform.Count; i++)
        {
            var mesh = ToDeform[i];
            for (int j = 0; j < mesh.DeformPoints.Length; j++)
            {
                var mp = mesh.DeformPoints[j];

                for (int k = 0; k < _deformed.ElementCount / 3; k++)
                {
                    uint[] idx = [
                        _mesh.Indices[(k*3)+0],
                        _mesh.Indices[(k*3)+1],
                        _mesh.Indices[(k*3)+2],
                    ];
                    var tri = new Triangle
                    {
                        P1 = _base.Points[idx[0]],
                        P2 = _base.Points[idx[1]],
                        P3 = _base.Points[idx[2]],
                    };

                    // Do some cheaper checks first.
                    float minX = float.Min(tri.P1.X, float.Min(tri.P2.X, tri.P3.X));
                    float maxX = float.Max(tri.P1.X, float.Max(tri.P2.X, tri.P3.X));
                    float minY = float.Min(tri.P1.Y, float.Min(tri.P2.Y, tri.P3.Y));
                    float maxY = float.Max(tri.P1.Y, float.Max(tri.P2.Y, tri.P3.Y));
                    if (!(minX < mp.X && maxX > mp.X) &&
                        !(minY < mp.Y && maxY > mp.Y))
                        continue;

                    // Expensive check and barycentric coordinates.
                    var bc = tri.Barycentric(mp);
                    if (bc.X < 0 || bc.Y < 0 || bc.Z < 0)
                        continue;

                    mesh.Deform(j, -(
                        (_deformDeltas[idx[0]] * bc.X) +
                        (_deformDeltas[idx[1]] * bc.Y) +
                        (_deformDeltas[idx[2]] * bc.Z)
                    ));
                    break;
                }
            }
        }

        base.PostUpdate(drawList);
    }

    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);

        MeshData data = Mesh.ToMeshData();
        var obj1 = new JsonObject();
        data.Serialize(obj1);
        obj["mesh"] = obj1;
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);

        _deformed = new DeformedMesh();
        _base = new DeformedMesh();
        if (data.TryGetProperty("mesh", out var item) && item.ValueKind != JsonValueKind.Null)
        {
            var data1 = new MeshData();
            data1.Deserialize(item);
            Mesh = Mesh.FromMeshData(data1);
        }
    }
}
