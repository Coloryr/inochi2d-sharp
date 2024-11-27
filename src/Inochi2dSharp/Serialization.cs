using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp;

namespace Inochi2dSharp;

public static class Serialization
{
    public static JsonNode ToToken(this Vector2 vector)
    {
        return new JsonArray() { vector.X, vector.Y };
    }

    public static JsonNode ToToken(this Vector3 vector)
    {
        return new JsonArray() { vector.X, vector.Y, vector.Z };
    }

    public static Vector2 ToVector2(this JsonElement array)
    {
        if (array.GetArrayLength() != 2)
        {
            return new();
        }

        var temp = array.EnumerateArray().ToArray();

        return new(temp[0].GetSingle(), temp[1].GetSingle());
    }

    public static Vector3 ToVector3(this JsonElement array)
    {
        if (array.GetArrayLength() != 3)
        {
            return new();
        }

        var temp = array.EnumerateArray().ToArray();

        return new(temp[0].GetSingle(), temp[1].GetSingle(), temp[2].GetSingle());
    }

    public static JsonNode ToToken(this List<float>[] floats)
    {
        var list = new JsonArray();
        foreach (var item in floats)
        {
            var list1 = new JsonArray();
            foreach (var item1 in item)
            {
                list1.Add(item1);
            }
            list.Add(list1);
        }

        return list;
    }

    public static JsonNode ToToken<T>(this List<List<T>> floats)
    {
        var list = new JsonArray();
        foreach (var item in floats)
        {
            var list1 = new JsonArray();
            foreach (var item1 in item)
            {
                list1.Add(item1);
            }
            list.Add(list1);
        }

        return list;
    }

    public static List<List<T>> ToListList<T>(this JsonElement array)
    {
        var list = new List<List<T>>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.Array)
            {
                var list1 = new List<T>();
                foreach (var item1 in item.EnumerateArray())
                {
                    list1.Add(item1.Deserialize<T>()!);
                }
                list.Add(list1);
            }
        }

        return list;
    }


    public static JsonNode ToToken<T>(this T[][] items)
    {
        var list = new JsonArray();
        foreach (var item in items)
        {
            var list1 = new JsonArray();
            foreach (var item1 in item)
            {
                list1.Add(item1);
            }
            list.Add(list1);
        }

        return list;
    }

    public static T[][] ToArray<T>(this JsonElement array)
    {
        return ToListList<T>(array).Select(item => item.ToArray()).ToArray();
    }
}
