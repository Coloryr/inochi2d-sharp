using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core;

/// <summary>
/// Mesh data
/// </summary>
public record MeshData
{
    /// <summary>
    /// Vertices in the mesh
    /// </summary>
    public List<Vector2> Vertices = [];

    /// <summary>
    /// Base uvs
    /// </summary>
    public List<Vector2> Uvs = [];

    /// <summary>
    /// Indices in the mesh
    /// </summary>
    public List<ushort> Indices = [];

    /// <summary>
    /// Origin of the mesh
    /// </summary>
    public Vector2 Origin = new(0, 0);

    public List<List<float>> GridAxes = [[], []];

    /// <summary>
    /// Adds a new vertex
    /// </summary>
    /// <param name="vertex"></param>
    /// <param name="uv"></param>
    public void Add(Vector2 vertex, Vector2 uv)
    {
        Vertices.Add(vertex);
        Uvs.Add(uv);
    }

    /// <summary>
    /// Clear connections/indices
    /// </summary>
    public void ClearConnections()
    {
        Indices = [];
    }

    /// <summary>
    /// Connects 2 vertices together
    /// </summary>
    /// <param name="first"></param>
    /// <param name="second"></param>
    public void Connect(ushort first, ushort second)
    {
        Indices.Add(first);
        Indices.Add(second);
    }

    /// <summary>
    /// Find the index of a vertex
    /// </summary>
    /// <param name="vert"></param>
    /// <returns></returns>
    public int Find(Vector2 vert)
    {
        for (int a = 0; a < Vertices.Count; a++)
        {
            var v = Vertices[a];
            if (v == vert) return a;
        }
        return -1;
    }

    /// <summary>
    /// Whether the mesh data is ready to be used
    /// </summary>
    /// <returns></returns>
    public bool IsReady()
    {
        return Indices.Count != 0 && Indices.Count % 3 == 0;
    }

    /// <summary>
    /// Whether the mesh data is ready to be triangulated
    /// </summary>
    /// <returns></returns>
    public bool CanTriangulate()
    {
        return Indices.Count != 0 && Indices.Count % 3 == 0;
    }

    /// <summary>
    /// Fixes the winding order of a mesh.
    /// </summary>
    public void FixWinding()
    {
        if (!IsReady()) return;

        for (int j = 0; j < Indices.Count / 3; j++)
        {
            var i = j * 3;

            var vertA = Vertices[Indices[i + 0]];
            var vertB = Vertices[Indices[i + 1]];
            var vertC = Vertices[Indices[i + 2]];
            bool cw = Vector3.Cross(new Vector3(vertB - vertA, 0), new Vector3(vertC - vertA, 0)).Z < 0;

            // Swap winding
            if (cw)
            {
                (Indices[i + 2], Indices[i + 1]) = (Indices[i + 1], Indices[i + 2]);
            }
        }
    }

    /// <summary>
    /// Gets connections at a certain point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public int ConnectionsAtPoint(Vector2 point)
    {
        int p = Find(point);
        if (p == -1) return 0;
        return ConnectionsAtPoint(p);
    }

    /// <summary>
    /// Gets connections at a certain point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public int ConnectionsAtPoint(int point)
    {
        int found = 0;
        foreach (var index in Indices)
        {
            if (index == point) found++;
        }
        return found;
    }

    public MeshData Copy()
    {
        return new MeshData
        {
            // Copy verts
            Vertices = [.. Vertices],
            // Copy UVs
            Uvs = [.. Uvs],
            // Copy UVs
            Indices = [.. Indices],
            // Copy axes
            GridAxes = GridAxes.Select(row => row.ToList()).ToList(),
            Origin = new(Origin.X, Origin.Y)
        };
    }

    public void Serialize(JsonObject obj)
    {
        var list = new JsonArray();
        foreach (var vertex in Vertices)
        {
            list.Add(vertex.X);
            list.Add(vertex.Y);
        }
        obj.Add("verts", list);

        if (Uvs.Count > 0)
        {
            list = [];
            foreach (var uv in Uvs)
            {
                list.Add(uv.X);
                list.Add(uv.Y);
            }
            obj.Add("uvs", list);
        }

        list = [];
        foreach (var item in Indices)
        {
            list.Add(item);
        }
        obj.Add("indices", list);

        obj.Add("origin", Origin.ToToken());
        if (IsGrid())
        {
            obj.Add("grid_axes", GridAxes.ToToken());
        }
    }

    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "verts" && item.Value.ValueKind == JsonValueKind.Array)
            {
                var list1 = item.Value.EnumerateArray().ToArray();
                for (int a = 0; a < list1.Length;)
                {
                    var temp1 = list1[a++].GetSingle();
                    var temp2 = list1[a++].GetSingle();
                    Vertices.Add(new(temp1, temp2));
                }
            }
            else if (item.Name == "uvs" && item.Value.ValueKind == JsonValueKind.Array)
            {
                var list1 = item.Value.EnumerateArray().ToArray();
                for (int a = 0; a < list1.Length;)
                {
                    var temp1 = list1[a++].GetSingle();
                    var temp2 = list1[a++].GetSingle();
                    Uvs.Add(new(temp1, temp2));
                }
            }
            else if (item.Name == "origin" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Origin = item.Value.ToVector2();
            }

            else if (item.Name == "grid_axes" && item.Value.ValueKind == JsonValueKind.Array)
            {
                GridAxes = item.Value.ToListList<float>();
            }
            else if (item.Name == "indices" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement indiceData in item.Value.EnumerateArray())
                {
                    if (indiceData.ValueKind != JsonValueKind.Null)
                    {
                        Indices.Add(indiceData.GetUInt16());
                    }
                }
            }
        }
    }

    public static MeshData CreateQuadMesh(Vector2Int size, Vector4 uvBounds)
    {
        return CreateQuadMesh(size, uvBounds, new(6, 6), new(0));
    }

    /// <summary>
    /// Generates a quad based mesh which is cut `cuts` amount of times
    /// 
    /// Example:
    ///                         Size of Texture                       Uses all of UV    width > height
    /// MeshData.createQuadMesh(vec2i(texture.width, texture.height), vec4(0, 0, 1, 1), vec2i(32, 16))</summary>
    /// <param name="size">size of the mesh</param>
    /// <param name="uvBounds">x, y UV coordinates + width/height in UV coordinate space</param>
    /// <param name="cuts">how many time to cut the mesh on the X and Y axis</param>
    /// <param name="origin"></param>
    /// <returns></returns>
    public static MeshData CreateQuadMesh(Vector2Int size, Vector4 uvBounds, Vector2Int cuts, Vector2Int origin)
    {
        // Splits may not be below 2.
        if (cuts.X < 2) cuts.X = 2;
        if (cuts.Y < 2) cuts.Y = 2;

        var data = new MeshData();
        var m = new Dictionary<int[], ushort>();
        int sw = size.X / cuts.X;
        int sh = size.Y / cuts.Y;
        float uvx = uvBounds.W / cuts.X;
        float uvy = uvBounds.Z / cuts.Y;

        // Generate vertices and UVs
        for (int y = 0; y < cuts.Y + 1; y++)
        {
            data.GridAxes[0].Add(y * sh - origin.Y);
            for (int x = 0; x < cuts.X + 1; x++)
            {
                data.GridAxes[1].Add(x * sw - origin.X);
                data.Vertices.Add(new(
                    (x * sw) - origin.X,
                    (y * sh) - origin.Y
                ));
                data.Uvs.Add(new(
                    uvBounds.X + x * uvx,
                    uvBounds.Y + y * uvy
                ));
                m.TryAdd([x, y], (ushort)(data.Vertices.Count - 1));
            }
        }

        // Generate indices
        var center = new Vector2Int(cuts.X / 2, cuts.Y / 2);
        for (int y = 0; y < cuts.Y; y++)
        {
            for (int x = 0; x < cuts.X; x++)
            {
                // Indices
                int[] indice0 = [x, y];
                int[] indice1 = [x, y + 1];
                int[] indice2 = [x + 1, y];
                int[] indice3 = [x + 1, y + 1];

                // We want the verticies to generate in an X pattern so that we won't have too many distortion problems
                if ((x < center.X && y < center.Y) || (x >= center.X && y >= center.Y))
                {
                    data.Indices.Add(m[indice0]);
                    data.Indices.Add(m[indice2]);
                    data.Indices.Add(m[indice3]);
                    data.Indices.Add(m[indice0]);
                    data.Indices.Add(m[indice3]);
                    data.Indices.Add(m[indice1]);
                }
                else
                {
                    data.Indices.Add(m[indice0]);
                    data.Indices.Add(m[indice1]);
                    data.Indices.Add(m[indice2]);
                    data.Indices.Add(m[indice1]);
                    data.Indices.Add(m[indice2]);
                    data.Indices.Add(m[indice3]);
                }
            }
        }

        return data;
    }

    public bool IsGrid()
    {
        return GridAxes.Count == 2 && GridAxes[0].Count > 2 && GridAxes[1].Count > 2;
    }

    public bool ClearGridIsDirty()
    {
        if (GridAxes.Count < 2 || GridAxes[0].Count == 0 || GridAxes[1].Count == 0)
            return false;

        bool clearGrid()
        {
            GridAxes[0].Clear();
            GridAxes[1].Clear();
            return true;
        }

        if (Vertices.Count != GridAxes[0].Count * GridAxes[1].Count)
        {
            return clearGrid();
        }

        int index = 0;
        foreach (var y in GridAxes[0])
        {
            foreach (var x in GridAxes[1])
            {
                var vert = new Vector2(x, y);
                if (vert != Vertices[index])
                {
                    return clearGrid();
                }
                index += 1;
            }
        }
        return false;
    }

    public bool RegenerateGrid()
    {
        if (GridAxes[0].Count < 2 || GridAxes[1].Count < 2)
            return false;

        Vertices.Clear();
        Uvs.Clear();
        Indices.Clear();

        var m = new Dictionary<int[], ushort>();

        float minY = GridAxes[0][0], maxY = GridAxes[0][^1];
        float minX = GridAxes[1][0], maxX = GridAxes[1][^1];
        float width = maxY - minY;
        float height = maxX - minX;
        foreach (var y in GridAxes[0])
        {
            foreach (var x in GridAxes[1])
            {
                Vertices.Add(new(x, y));
                Uvs.Add(new((x - minX) / width, (y - minY) / height));
                m.TryAdd([(int)x, (int)y], (ushort)(Vertices.Count - 1));
            }
        }

        var center = new Vector2(minX + width / 2, minY + height / 2);
        for (var i = 0; i < GridAxes[0].Count - 1; i++)
        {
            var yValue = GridAxes[0][i];
            for (var j = 0; j < GridAxes[1].Count - 1; j++)
            {

                var xValue = GridAxes[1][j];
                int x = j, y = i;

                // Indices
                int[] indice0 = [x, y];
                int[] indice1 = [x, y + 1];
                int[] indice2 = [x + 1, y];
                int[] indice3 = [x + 1, y + 1];

                // We want the verticies to generate in an X pattern so that we won't have too many distortion problems
                if ((xValue < center.X && yValue < center.Y) || (xValue >= center.X && yValue >= center.Y))
                {
                    Indices.Add(m[indice0]);
                    Indices.Add(m[indice2]);
                    Indices.Add(m[indice3]);
                    Indices.Add(m[indice0]);
                    Indices.Add(m[indice3]);
                    Indices.Add(m[indice1]);
                }
                else
                {
                    Indices.Add(m[indice0]);
                    Indices.Add(m[indice1]);
                    Indices.Add(m[indice2]);
                    Indices.Add(m[indice1]);
                    Indices.Add(m[indice2]);
                    Indices.Add(m[indice3]);
                }
            }
        }
        return true;
    }
#if DEBUG
    public void Dbg()
    {
        Console.WriteLine($"{Vertices.Count} {Uvs.Count} {Indices.Count}");
    }
#endif
}
