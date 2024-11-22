using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Math;

public static class Serialization
{
    public static JToken ToToken(this Vector2 vector)
    {
        return new JArray() { vector.X, vector.Y };
    }

    public static JToken ToToken(this Vector3 vector)
    {
        return new JArray() { vector.X, vector.Y, vector.Z };
    }

    public static Vector2 ToVector2(this JToken token)
    {
        if (token is not JArray array || array.Count != 2)
        {
            return new();
        }

        var list = array.Values<float>().ToArray();
        return new(list[0], list[1]);
    }

    public static Vector3 ToVector3(this JToken token)
    {
        if (token is not JArray array || array.Count != 3)
        {
            return new();
        }

        var list = array.Values<float>().ToArray();
        return new(list[0], list[1], list[2]);
    }

    public static JToken ToToken(this List<float>[] floats)
    {
        var list = new JArray();
        foreach (var item in floats)
        {
            var list1 = new JArray(item);
            list.Add(list1);
        }

        return list;
    }

    public static List<float>[] ToFloatList(this JToken token)
    {
        if (token is not JArray array)
        {
            return [];
        }
        var list = new List<List<float>>();
        foreach (var item in array)
        {
            list.Add(item.ToObject<List<float>>() ?? []);
        }

        return [.. list];
    }


    public static JToken ToToken<T>(this T[][] items)
    {
        var list = new JArray();
        foreach (var item in items)
        {
            var list1 = new JArray(item);
            list.Add(list1);
        }

        return list;
    }

    public static T[][] ToArray<T>(this JArray token)
    {
        var list = new List<List<T>>();
        foreach (var item in token)
        {
            list.Add(item.ToObject<List<T>>() ?? []);
        }

        return list.Select(item => item.ToArray()).ToArray();
    }
}
