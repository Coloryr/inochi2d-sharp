using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Deformers;

/// <summary>
/// A deformer which uses a 2D lattice as the basis for its deformation.
/// </summary>
[TypeId("LatticeDeformer", 0x0202)]
public class LatticeDeformer : Deformer
{
    private int subdivs;
    private Vector2 size_;

    //private float[][] weights_ = [];
    private Vector2[] latticeInitial = [];
    private Vector2[] lattice = [];

    /// <summary>
    /// The size of the lattice (in pixels)
    /// </summary>
    public Vector2 Size
    {
        get 
        {
            return size_;
        }
        set
        {
            size_ = value;
            RegenLattice();
        }
    }

    /// <summary>
    /// The amount of subdivisions in the lattice.
    /// </summary>
    public int Subdivisions
    {
        get 
        {
            return subdivs;
        }
        set
        {
            subdivs = value;
            RegenLattice();
        }
    }

    /// <summary>
    /// The base position of the deformable's points.
    /// </summary>
    public override Vector2[] BasePoints => latticeInitial;

    public override Vector2[] ControlPoints
    {
        get
        {
            return lattice;
        }
        set
        {
            var m = int.Min(value.Length, lattice.Length);
            Array.Copy(value, lattice, m);
        }
    }

    /// <summary>
    /// Constructs a new MeshGroup node
    /// </summary>
    /// <param name="parent"></param>
    public LatticeDeformer(Node? parent = null) : base(parent)
    { 
        
    }

    /// <summary>
    /// Updates the lattice deformer.
    /// </summary>
    /// <param name="delta"></param>
    /// <param name="drawList"></param>
    public override void Update(float delta, DrawList drawList)
    { 
        
    }

    /// <summary>
    /// Deforms the IDeformable.
    /// </summary>
    /// <param name="deformed">The deformation delta.</param>
    /// <param name="absolute">Whether the deformation is absolute, replacing the original deformation.</param>
    public override void Deform(Vector2[] deformed, bool absolute)
    {
        base.Deform(deformed, absolute);
    }

    /// <summary>
    /// Resets the deformation.
    /// </summary>
    public override void ResetDeform()
    {
        var m = int.Min(latticeInitial.Length, lattice.Length);
        Array.Copy(latticeInitial, lattice, m);
    }

    /// <summary>
    /// Rescans the children of the deformer.
    /// </summary>
    public override void Rescan()
    {
        base.Rescan();
    }

    /// <summary>
    /// Regenerates the lattice points.
    /// </summary>
    private void RegenLattice()
    {
        if (subdivs == 0)
            return;

        Array.Resize(ref latticeInitial, subdivs * subdivs);
        Array.Resize(ref lattice, latticeInitial.Length);
        Array.Clear(latticeInitial);

        var iter = new Vector2(size_.X / subdivs, size_.Y / subdivs);
        for (int i= 0;i< lattice.Length;i++) 
        {
            float x = i % (float)subdivs;
            float y = i / (float)subdivs;
            latticeInitial[i] = iter * new Vector2(x, y);
        }
    }

    /// <summary>
    /// Clears lattice weights
    /// </summary>
    private void ClearWeights()
    {
        //weights_ = null!;
    }

    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);
        obj["subdivisions"] = subdivs;
        obj["size"] = size_.ToToken();
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);
        if (data.TryGetProperty("subdivisions", out var item) && item.ValueKind != JsonValueKind.Null)
        {
            subdivs = item.GetInt32();
        }
        if (data.TryGetProperty("size", out var item1) && item1.ValueKind != JsonValueKind.Null)
        {
            size_ = item1.ToVector2();
        }
        RegenLattice();
    }
}
