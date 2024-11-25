using System.Numerics;
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

    public static Vector2 ToVector2(this JsonArray array)
    {
        if (array.Count != 2)
        {
            return new();
        }

        var list = array.GetValues<float>().ToArray();
        return new(list[0], list[1]);
    }

    public static Vector3 ToVector3(this JsonArray array)
    {
        if (array.Count != 3)
        {
            return new();
        }

        var list = array.GetValues<float>().ToArray();
        return new(list[0], list[1], list[2]);
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

    public static List<List<T>> ToListList<T>(this JsonArray array)
    {
        var list = new List<List<T>>();
        foreach (JsonArray item in array.Cast<JsonArray>())
        {
            list.Add(item.GetValues<T>().ToList());
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

    public static T[][] ToArray<T>(this JsonArray array)
    {
        var list = new List<List<T>>();
        foreach (JsonArray item in array.Cast<JsonArray>())
        {
            list.Add(item.GetValues<T>().ToList());
        }

        return list.Select(item => item.ToArray()).ToArray();
    }
}
