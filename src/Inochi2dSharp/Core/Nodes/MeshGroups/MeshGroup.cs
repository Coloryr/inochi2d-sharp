using System.Numerics;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.MeshGroups;

/// <summary>
/// Contains various deformation shapes that can be applied to
/// children of this node
/// </summary>
[TypeId("MeshGroup")]
public class MeshGroup(Node? parent = null) : Drawable(parent)
{
    protected ushort[] bitMask;
    protected Vector4 bounds;
    protected List<Triangle> triangles = [];
    protected Vector2[] transformedVertices = [];
    protected Matrix4x4 forwardMatrix;
    protected Matrix4x4 inverseMatrix;
    protected bool _translateChildren = true;
    protected bool precalculated = false;

    public bool Dynamic = false;

    public bool TranslateChildren
    {
        get => _translateChildren;
        set
        {
            _translateChildren = value;
            foreach (var child in Children)
                SetupChild(child);
        }
    }

    public MeshGroup() : this(null)
    {

    }

    private (Vector2[]?, Matrix4x4?) FilterChildren(List<Vector2> origVertices, Vector2[] origDeformation, ref Matrix4x4 origTransform)
    {
        if (!precalculated)
            return (null, null);

        var centerMatrix = inverseMatrix * origTransform;

        // Transform children vertices in MeshGroup coordinates.
        var r = new Rect(bounds.X, bounds.Y, (MathF.Ceiling(bounds.Z) - MathF.Floor(bounds.X) + 1), (MathF.Ceiling(bounds.W) - MathF.Floor(bounds.Y) + 1));
        for (int i = 0; i < origVertices.Count; i++)
        {
            var vertex = origVertices[i];
            Vector2 cVertex;
            if (Dynamic)
            {
                var transformedVertex = centerMatrix.Multiply(new Vector4(vertex + origDeformation[i], 0, 1));
                cVertex = new Vector2(transformedVertex.X, transformedVertex.Y);
            }
            else
            {
                var transformedVertex = centerMatrix.Multiply(new Vector4(vertex, 0, 1));
                cVertex = new Vector2(transformedVertex.X, transformedVertex.Y);
            }
            int index = -1;
            if (bounds.X <= cVertex.X && cVertex.X < bounds.Z && bounds.Y <= cVertex.Y && cVertex.Y < bounds.W)
            {
                ushort bit = bitMask[(int)(cVertex.Y - bounds.Y) * (int)r.Width + (int)(cVertex.X - bounds.X)];
                index = bit - 1;
            }
            Vector2 newPos;
            if (index < 0)
            {
                newPos = cVertex;
            }
            else
            {
                var temp = triangles[index].transformMatrix * new Vector3(cVertex, 1);
                newPos = new(temp.X, temp.Y);
            }
            if (!Dynamic)
            {
                var inv = centerMatrix.Copy();
                inv[0, 3] = 0;
                inv[1, 3] = 0;
                inv[2, 3] = 0;
                var temp = inv.Multiply(new Vector4(newPos - cVertex, 0, 1));
                origDeformation[i] += new Vector2(temp.X, temp.Y);
            }
            else
                origDeformation[i] = newPos - origVertices[i];
        }

        if (!Dynamic)
            return (origDeformation, null);
        return (origDeformation, forwardMatrix);
    }

    public override void Update()
    {
        PreProcess();
        DeformStack.Update();

        if (Data.Indices.Count > 0)
        {
            if (!precalculated)
            {
                Precalculate();
            }
            transformedVertices = new Vector2[Data.Vertices.Count];
            for (int i = 0; i < Data.Vertices.Count; i++)
            {
                var vertex = Data.Vertices[i];
                transformedVertices[i] = vertex + Deformation[i];
            }
            for (int index = 0; index < triangles.Count; index++)
            {
                var p1 = transformedVertices[Data.Indices[index * 3]];
                var p2 = transformedVertices[Data.Indices[index * 3 + 1]];
                var p3 = transformedVertices[Data.Indices[index * 3 + 2]];
                triangles[index].transformMatrix = new Matrix3x3(p2.X - p1.X, p3.X - p1.X, p1.X,
                                                        p2.Y - p1.Y, p3.Y - p1.Y, p1.Y,
                                                        0, 0, 1) * triangles[index].offsetMatrices;
            }
            forwardMatrix = Transform().Matrix;
            inverseMatrix = GlobalTransform.Matrix.Copy();
        }

        ((Node)this).Update();
        UpdateDeform();
    }

    public override void Draw()
    {
        base.Draw();
    }

    public override string TypeId()
    {
        return "MeshGroup";
    }

    public void Precalculate()
    {
        if (Data.Indices.Count == 0)
        {
            triangles = [];
            bitMask = [];
            return;
        }

        static Vector4 GetBounds(ICollection<Vector2> vertices)
        {
            var bounds = new Vector4(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);
            foreach (var v in vertices)
            {
                bounds = new Vector4(MathF.Min(bounds.X, v.X), MathF.Min(bounds.Y, v.Y), MathF.Max(bounds.Z, v.X), MathF.Max(bounds.W, v.Y));
            }
            bounds.X = MathF.Floor(bounds.X);
            bounds.Y = MathF.Floor(bounds.Y);
            bounds.Z = MathF.Ceiling(bounds.Z);
            bounds.W = MathF.Ceiling(bounds.W);
            return bounds;
        }

        // Calculating conversion matrix for triangles
        bounds = GetBounds(Data.Vertices);
        triangles.Clear();
        for (int i = 0; i < Data.Indices.Count / 3; i++)
        {
            var t = new Triangle();
            Vector2[] tvertices =
            [
                Data.Vertices[Data.Indices[3*i]],
                Data.Vertices[Data.Indices[3*i+1]],
                Data.Vertices[Data.Indices[3*i+2]]
            ];

            var p1 = tvertices[0];
            var p2 = tvertices[1];
            var p3 = tvertices[2];

            var axis0 = p2 - p1;
            float axis0len = axis0.Length();
            axis0 /= axis0len;
            var axis1 = p3 - p1;
            float axis1len = axis1.Length();
            axis1 /= axis1len;

            var raxis1 = new Matrix3x3(axis0.X, axis0.Y, 0, -axis0.Y, axis0.X, 0, 0, 0, 1) * new Vector3(axis1, 1);
            float cosA = raxis1.X;
            float sinA = raxis1.Y;
            t.offsetMatrices =
                new Matrix3x3(axis0len > 0 ? 1 / axis0len : 0, 0, 0,
                        0, axis1len > 0 ? 1 / axis1len : 0, 0,
                        0, 0, 1) *
                new Matrix3x3(1, -cosA / sinA, 0,
                        0, 1 / sinA, 0,
                        0, 0, 1) *
                new Matrix3x3(axis0.X, axis0.Y, 0,
                        -axis0.Y, axis0.X, 0,
                        0, 0, 1) *
                new Matrix3x3(1, 0, -p1.X,
                        0, 1, -p1.Y,
                        0, 0, 1);
            triangles.Add(t);
        }

        // Construct bitMap
        int width = (int)(MathF.Ceiling(bounds.Z) - MathF.Floor(bounds.X) + 1);
        int height = (int)(MathF.Ceiling(bounds.W) - MathF.Floor(bounds.Y) + 1);
        bitMask = new ushort[width * height];
        for (int i = 0; i < triangles.Count; i++)
        {
            Vector2[] tvertices =
            [
                Data.Vertices[Data.Indices[3*i]],
                Data.Vertices[Data.Indices[3*i+1]],
                Data.Vertices[Data.Indices[3*i+2]]
            ];

            var tbounds = GetBounds(tvertices);
            int bwidth = (int)(MathF.Ceiling(tbounds.Z) - MathF.Floor(tbounds.X) + 1);
            int bheight = (int)(MathF.Ceiling(tbounds.W) - MathF.Floor(tbounds.Y) + 1);
            int top = (int)MathF.Floor(tbounds.Y);
            int left = (int)MathF.Floor(tbounds.X);
            for (int y = 0; y < bheight; y++)
            {
                for (int x = 0; x < bwidth; x++)
                {
                    var pt = new Vector2(left + x, top + y);
                    if (TriangleHelper.IsPointInTriangle(pt, tvertices))
                    {
                        ushort id = (ushort)(i + 1);
                        pt -= new Vector2(bounds.X, bounds.Y);
                        bitMask[(int)(pt.Y * width + pt.X)] = id;
                    }
                }
            }
        }

        precalculated = true;
        foreach (var child in Children)
        {
            SetupChild(child);
        }
    }

    public override void RenderMask(bool dodge = false)
    {

    }

    public override void Rebuffer(MeshData data)
    {
        base.Rebuffer(data);
        if (Dynamic)
        {
            precalculated = false;
        }
    }

    protected override void SerializeSelfImpl(JObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);

        serializer.Add("dynamic_deformation", Dynamic);
        serializer.Add("translate_children", _translateChildren);
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        var temp = data["dynamic_deformation"];
        if (temp != null)
        {
            Dynamic = (bool)temp;
        }

        _translateChildren = false;
        temp = data["translate_children"];
        if (temp != null)
        {
            _translateChildren = (bool)temp;
        }
    }

    public override void SetupChild(Node? child)
    {
        if (child == null)
        {
            return;
        }

        void SetGroup(Node node)
        {
            var group = node as MeshGroup;
            bool isDrawable = node is Drawable drawable;
            bool isComposite = node is Composite composite && composite.PropagateMeshGroup;
            bool mustPropagate = (isDrawable && group is null) || isComposite;
            if (_translateChildren || isDrawable)
            {
                if (isDrawable && Dynamic)
                {
                    node.PreProcessFilter = null;
                    node.PostProcessFilter = FilterChildren;
                }
                else
                {
                    node.PreProcessFilter = FilterChildren;
                    node.PostProcessFilter = null;
                }
            }
            else
            {
                node.PreProcessFilter = null;
                node.PostProcessFilter = null;
            }
            // traverse children if node is Drawable and is not MeshGroup instance.
            if (mustPropagate)
            {
                foreach (var child in node.Children)
                {
                    SetGroup(child);
                }
            }
        }

        if (Data.Indices.Count > 0)
        {
            SetGroup(child);
        }
    }

    public void ApplyDeformToChildren(List<Parameter> par)
    {
        if (Dynamic || Data.Indices.Count == 0)
            return;

        if (!precalculated)
        {
            Precalculate();
        }
        forwardMatrix = Transform().Matrix;
        inverseMatrix = GlobalTransform.Matrix.Copy();

        foreach (var param in par)
        {
            void TransferChildren(Node node, int x, int y)
            {
                var drawable = node as Drawable;
                var group = node as MeshGroup;
                bool isDrawable = drawable != null;
                bool isComposite = node is Composite composite && composite.PropagateMeshGroup;
                bool mustPropagate = (isDrawable && group is null) || isComposite;
                if (isDrawable)
                {
                    var vertices = drawable!.Vertices;
                    var matrix = drawable.Transform().Matrix;

                    var nodeBinding = param.GetOrAddBinding(node, "deform") as DeformationParameterBinding;
                    var nodeDeform = nodeBinding!.values[x][y].VertexOffsets.ToArray();
                    var filterResult = FilterChildren(vertices, nodeDeform, ref matrix);
                    if (filterResult.Item1 != null)
                    {
                        var temp = nodeBinding.values[x][y].VertexOffsets;
                        for (int i = 0; i < temp.Count; i++)
                        {
                            temp[i] = filterResult.Item1[i];
                        }
                        nodeBinding.getIsSet()[x][y] = true;
                    }
                }
                else if (_translateChildren && !isComposite)
                {
                    var temp = node.LocalTransform.Translation;
                    List<Vector2> vertices = [new Vector2(temp.X, temp.Y)];
                    var matrix = node.Parent != null ? node.Parent.Transform().Matrix : Matrix4x4.Identity;

                    var nodeBindingX = param.GetOrAddBinding(node, "transform.t.x") as ValueParameterBinding;
                    var nodeBindingY = param.GetOrAddBinding(node, "transform.t.y") as ValueParameterBinding;
                    var temp1 = node.OffsetTransform.Translation;
                    Vector2[] nodeDeform = [new Vector2(temp1.X, temp1.Y)];
                    var filterResult = FilterChildren(vertices, nodeDeform, ref matrix);
                    if (filterResult.Item1 != null)
                    {
                        nodeBindingX!.values[x][y] += filterResult.Item1[0].X;
                        nodeBindingY!.values[x][y] += filterResult.Item1[0].Y;
                        nodeBindingX.getIsSet()[x][y] = true;
                        nodeBindingY.getIsSet()[x][y] = true;
                    }

                }
                if (mustPropagate)
                {
                    foreach (var child in node.Children)
                    {
                        TransferChildren(child, x, y);
                    }
                }
            }


            if (param.GetBinding(this, "deform") is { } binding)
            {
                if (binding is not DeformationParameterBinding deformBinding)
                {
                    throw new Exception("deformBinding is not DeformationParameterBinding");
                }
                Node target = binding.getTarget().node;

                for (int x = 0; x < param.AxisPoints[0].Count; x++)
                {
                    for (int y = 0; y < param.AxisPoints[1].Count; y++)
                    {
                        Vector2[] deformation;
                        if (deformBinding.isSet[x][y])
                            deformation = [.. deformBinding.values[x][y].VertexOffsets];
                        else
                        {
                            bool rightMost = x == param.AxisPoints[0].Count - 1;
                            bool bottomMost = y == param.AxisPoints[1].Count - 1;
                            deformation = [.. deformBinding.Interpolate(new Vector2Int(rightMost ? x - 1 : x, bottomMost ? y - 1 : y), new Vector2(rightMost ? 1 : 0, bottomMost ? 1 : 0)).VertexOffsets];
                        }
                        transformedVertices = new Vector2[Vertices.Count];
                        for (int i = 0; i < Vertices.Count; i++)
                        {
                            var vertex = Vertices[i];
                            transformedVertices[i] = vertex + deformation[i];
                        }
                        for (int index = 0; index < triangles.Count; index++)
                        {

                            var p1 = transformedVertices[Data.Indices[index * 3]];
                            var p2 = transformedVertices[Data.Indices[index * 3 + 1]];
                            var p3 = transformedVertices[Data.Indices[index * 3 + 2]];
                            triangles[index].transformMatrix = 
                                new Matrix3x3(p2.X - p1.X, p3.X - p1.X, p1.X,
                                              p2.Y - p1.Y, p3.Y - p1.Y, p1.Y,
                                              0, 0, 1) * triangles[index].offsetMatrices;
                        }

                        foreach (var child in Children)
                        {
                            TransferChildren(child, x, y);
                        }

                    }
                }
                param.RemoveBinding(binding);
            }

        }
        Data.Indices.Clear();
        Data.Vertices.Clear();
        Data.Uvs.Clear();
        Rebuffer(Data);
        _translateChildren = false;
        precalculated = false;
    }

    public void SwitchMode(bool dynamic)
    {
        if (Dynamic != dynamic)
        {
            Dynamic = dynamic;
            precalculated = false;
        }
    }

    public void ClearCache()
    {
        precalculated = false;
        bitMask = [];
        triangles = [];
    }

    public override void PreProcess()
    {
        base.PreProcess();
    }

    public override void PostProcess()
    {
        base.PreProcess();
    }
}
