using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.Core;

/// <summary>
/// Mesh data as stored in Inochi2D's file format.
/// </summary>
public record MeshData
{
    /// <summary>
    /// Vertices in the mesh
    /// </summary>
    public Vector2[] Vertices { get; set; }

    /// <summary>
    /// Base uvs
    /// </summary>
    public Vector2[] Uvs { get; set; }

    /// <summary>
    /// Indices in the mesh
    /// </summary>
    public uint[] Indices { get; set; }

    /// <summary>
    /// Serialization handler
    /// </summary>
    /// <param name="obj"></param>
    public void Serialize(JsonObject obj)
    {
        var list = new JsonArray();
        foreach (var vertex in Vertices)
        {
            list.Add(vertex.X);
            list.Add(vertex.Y);
        }
        obj.Add("verts", list);

        if (Uvs.Length > 0)
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
    }

    /// <summary>
    /// Deserialization handler
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name is "verts" && item.Value.ValueKind is JsonValueKind.Array)
            {
                var list1 = item.Value.EnumerateArray().ToArray();
                Vertices = new Vector2[list1.Length / 2];
                int index = 0;
                for (int a = 0; a < list1.Length;)
                {
                    var temp1 = list1[a++].GetSingle();
                    var temp2 = list1[a++].GetSingle();
                    Vertices[index++] = new(temp1, temp2);
                }
            }
            else if (item.Name is "uvs" && item.Value.ValueKind is JsonValueKind.Array)
            {
                var list1 = item.Value.EnumerateArray().ToArray();
                Uvs = new Vector2[list1.Length / 2];
                int index = 0;
                for (int a = 0; a < list1.Length;)
                {
                    var temp1 = list1[a++].GetSingle();
                    var temp2 = list1[a++].GetSingle();
                    Uvs[index++] = new(temp1, temp2);
                }
            }
            else if (item.Name is "indices" && item.Value.ValueKind is JsonValueKind.Array)
            {
                var list1 = item.Value.EnumerateArray().ToArray();
                int index = 0;
                foreach (var indiceData in list1)
                {
                    Indices[index++] = indiceData.GetUInt32();
                }
            }
        }
        if (data.TryGetProperty("origin", out var temp) && temp.ValueKind is JsonValueKind.Array)
        {
            var origin = temp.ToVector2();
            if (origin.IsFinite())
            {
                for (int i = 0; i < Vertices.Length; i++)
                {
                    Vertices[i] -= origin;
                }
            }
        }
    }
}
