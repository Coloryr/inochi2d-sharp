using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.MeshGroups;

public class MeshGroup : Drawable
{
    protected ushort[] bitMask;
    protected Vector4 bounds;
    protected List<Triangle> triangles = [];
    protected Vector2[] transformedVertices = [];
    protected Matrix4x4 forwardMatrix;
    protected Matrix4x4 inverseMatrix;
    protected bool translateChildren = true;
    protected bool precalculated = false;

    public bool dynamic = false;

    public MeshGroup(Node? parent = null) : base(parent)
    { 
        
    }

    private (Vector2[]?, Matrix4x4?) FilterChildren(Vector2[] origVertices, Vector2[] origDeformation, ref Matrix4x4 origTransform)
    {
        if (!precalculated)
            return (null, null);

        var centerMatrix = inverseMatrix * origTransform;

        // Transform children vertices in MeshGroup coordinates.
        var r = new Rect(bounds.X, bounds.Y, (MathF.Ceiling(bounds.Z) - MathF.Floor(bounds.X) + 1), (MathF.Ceiling(bounds.W) - MathF.Floor(bounds.Y) + 1));
        for (int i = 0; i < origVertices.Length; i++)
        {
            var vertex = origVertices[i];
            Vector2 cVertex;
            if (dynamic)
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
            if (!dynamic)
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

        if (!dynamic)
            return (origDeformation, null);
        return(origDeformation, forwardMatrix);
    }

    public override void update()
    {
        preProcess();
        deformStack.Update();

        if (data.Indices.Count > 0)
        {
            if (!precalculated)
            {
                precalculate();
            }
            transformedVertices = new Vector2[data.Vertices.Count];
            for (int i = 0; i < data.Vertices.Count; i++)
            {
                var vertex = data.Vertices[i];
                transformedVertices[i] = vertex + deformation[i];
            }
            for (int index = 0; index < triangles.Count; index++)
            {
                var p1 = transformedVertices[data.Indices[index * 3]];
                var p2 = transformedVertices[data.Indices[index * 3 + 1]];
                var p3 = transformedVertices[data.Indices[index * 3 + 2]];
                triangles[index].transformMatrix = new Matrix3x3(p2.X - p1.X, p3.X - p1.X, p1.X,
                                                        p2.Y - p1.Y, p3.Y - p1.Y, p1.Y,
                                                        0, 0, 1) * triangles[index].offsetMatrices;
            }
            forwardMatrix = Transform().Matrix;
            inverseMatrix = globalTransform.Matrix.Copy();
        }

        ((Node)this).update();
        updateDeform();
    }

    public override void draw()
    {
        base.draw();
    }

    public override string typeId()
    {
        return "MeshGroup";
    }

    private Vector4 getBounds(ICollection<Vector2> vertices)
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

    public void precalculate()
    {
        if (data.Indices.Count == 0)
        {
            triangles =[];
            bitMask = [];
            return;
        }

        // Calculating conversion matrix for triangles
        bounds = getBounds(data.Vertices);
        triangles.Clear();
        for (int i = 0; i < data.Indices.Count / 3; i++)
        {
            var t = new Triangle();
            Vector2[] tvertices =
            [
                data.Vertices[data.Indices[3*i]],
                data.Vertices[data.Indices[3*i+1]],
                data.Vertices[data.Indices[3*i+2]]
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
        for (int i =0; i< triangles.Count;i++) 
        {
            var t = triangles[i];
            Vector2[] tvertices = 
            [
                data.Vertices[data.Indices[3*i]],
                data.Vertices[data.Indices[3*i+1]],
                data.Vertices[data.Indices[3*i+2]]
            ];

            var tbounds = getBounds(tvertices);
            int bwidth = (int)(MathF.Ceiling(tbounds.Z) - MathF.Floor(tbounds.X) + 1);
            int bheight = (int)(MathF.Ceiling(tbounds.W) - MathF.Floor(tbounds.Y) + 1);
            int top = (int)MathF.Floor(tbounds.Y);
            int left = (int)MathF.Floor(tbounds.X);
            for (int y = 0; y < bheight; y++) 
            {
                for (int x = 0; x < bwidth; x++) 
                {
                    var pt = new Vector2(left + x, top + y);
                    if (isPointInTriangle(pt, tvertices))
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
            setupChild(child);
        }
    }

    public override void renderMask(bool dodge = false)
    {

    }

    public override void rebuffer(MeshData data)
    {
        base.rebuffer(data);
        if (dynamic)
        {
            precalculated = false;
        }
    }

    protected override void SerializeSelf(JObject serializer, bool recursive = true)
    {
        base.SerializeSelf(serializer, recursive);

        serializer.Add("dynamic_deformation", dynamic);
        serializer.Add("translate_children", translateChildren);
    }

    protected override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        var temp = data["dynamic_deformation"];
        if (temp != null)
        {
            dynamic = (bool)temp;
        }
      
        translateChildren = false;
        temp = data["translate_children"];
        if (temp != null)
        {
            translateChildren = (bool)temp;
        }
    }

    public override void setupChild(Node? child)
    {
        if (child == null)
        {
            return;
        }

        if (data.Indices.Count > 0)
        {
            SetGroup(child);
        }
    }

    private void SetGroup(Node node)
    {
        var group = node as MeshGroup;
        bool isDrawable = node is Drawable drawable;
        bool isComposite = node is Composite composite && composite.propagateMeshGroup;
        bool mustPropagate = (isDrawable && group is null) || isComposite;
        if (translateChildren || isDrawable)
        {
            if (isDrawable && dynamic)
            {
                node.preProcessFilter = null;
                node.postProcessFilter = FilterChildren;
            }
            else
            {
                node.preProcessFilter = FilterChildren;
                node.postProcessFilter = null;
            }
        }
        else
        {
            node.preProcessFilter = null;
            node.postProcessFilter = null;
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

    private void transferChildren(Node node, int x, int y)
    {
        var drawable =  node as Drawable;
        var group =  node as MeshGroup;
        var composite =  node as Composite;
        bool isDrawable = drawable != null;
        bool isComposite = composite != null && composite.propagateMeshGroup;
        bool mustPropagate = (isDrawable && group is null) || isComposite;
        if (isDrawable)
        {
            var vertices = drawable.vertices;
            var matrix = drawable.Transform().Matrix;

            var nodeBinding = param.getOrAddBinding(node, "deform") as DeformationParameterBinding;
            var nodeDeform = nodeBinding.values[x][y].vertexOffsets[].dup;
            var filterResult = FilterChildren(vertices, nodeDeform, &matrix);
            if (filterResult.Item1 != null && filterResult.Item1.Length > 0)
            {
                nodeBinding.values[x][y].vertexOffsets.data[0..filterResult.Item1.Length] = filterResult.Item1[0..];
                nodeBinding.getIsSet()[x][y] = true;
            }
        }
        else if (translateChildren && !isComposite)
        {
            var vertices = [node.localTransform.Translation.xy];
            var matrix = node.Parent != null ? node.Parent.Transform().Matrix : Matrix4x4.Identity;

            var nodeBindingX = cast(ValueParameterBinding)param.getOrAddBinding(node, "transform.t.x");
            var nodeBindingY = cast(ValueParameterBinding)param.getOrAddBinding(node, "transform.t.y");
            var nodeDeform = [node.offsetTransform.translation.xy];
            var filterResult = filterChildren(vertices, nodeDeform, &matrix);
            if (filterResult[0]! is null)
            {
                nodeBindingX.values[x][y] += filterResult[0][0].x;
                nodeBindingY.values[x][y] += filterResult[0][0].y;
                nodeBindingX.getIsSet()[x][y] = true;
                nodeBindingY.getIsSet()[x][y] = true;
            }

        }
        if (mustPropagate)
        {
            foreach (child; node.children) {
                transferChildren(child, x, y);
            }
        }
    }

    public void applyDeformToChildren(Parameter[] parameters)
    {
        if (dynamic || data.Indices.Count == 0)
            return;

        if (!precalculated)
        {
            precalculate();
        }
        forwardMatrix = Transform().Matrix;
        inverseMatrix = globalTransform.Matrix.Copy();

        foreach (var param in parameters) 
        {
            


            if (auto binding = param.getBinding(this, "deform")) {
                auto deformBinding = cast(DeformationParameterBinding)binding;
                assert(deformBinding! is null);
                Node target = binding.getTarget().node;

                for (int x = 0; x < param.axisPoints[0].length; x++)
                {
                    for (int y = 0; y < param.axisPoints[1].length; y++)
                    {

                        vec2[] deformation;
                        if (deformBinding.isSet_[x][y])
                            deformation = deformBinding.values[x][y].vertexOffsets[].dup;
                        else
                        {
                            bool rightMost = x == param.axisPoints[0].length - 1;
                            bool bottomMost = y == param.axisPoints[1].length - 1;
                            deformation = deformBinding.interpolate(vec2u(rightMost ? x - 1 : x, bottomMost ? y - 1 : y), vec2(rightMost ? 1 : 0, bottomMost ? 1 : 0)).vertexOffsets[];
                        }
                        transformedVertices.length = vertices.length;
                        foreach (i, vertex; vertices) {
                    transformedVertices[i] = vertex + deformation[i];
                }
                foreach (index; 0..triangles.length) {
                    auto p1 = transformedVertices[data.indices[index * 3]];
                    auto p2 = transformedVertices[data.indices[index * 3 + 1]];
                    auto p3 = transformedVertices[data.indices[index * 3 + 2]];
                    triangles[index].transformMatrix = mat3([p2.x - p1.x, p3.x - p1.x, p1.x,
                                                                    p2.y - p1.y, p3.y - p1.y, p1.y,
                                                                    0, 0, 1]) * triangles[index].offsetMatrices;
                }

                foreach (child; children) {
                    transferChildren(child, x, y);
                }

            }
        }
        param.removeBinding(binding);
    }

}
data.indices.length = 0;
        data.vertices.length = 0;
        data.uvs.length = 0;
        rebuffer(data);
translateChildren = false;
        precalculated = false;
    }

    void switchMode(bool dynamic)
{
    if (this.dynamic != dynamic)
    {
        this.dynamic = dynamic;
        precalculated = false;
    }
}

bool getTranslateChildren() { return translateChildren; }

void setTranslateChildren(bool value)
{
    translateChildren = value;
    foreach (child; children)
            setupChild(child);
}

void clearCache()
{
    precalculated = false;
    bitMask.length = 0;
    triangles.length = 0;
}

protected override void preProcess()
    {
        base.preProcess();
    }

    protected override void postProcess()
    {
        base.preProcess();
    }
}
