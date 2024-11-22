using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp.Core.Nodes;

public static class NodeHelper
{
    public const uint InInvalidUUID = uint.MaxValue;

    private static readonly List<uint> s_takenUUIDs = [];

    static NodeHelper()
    {
        //RegisterNodeType<>();
    }

    /// <summary>
    /// Creates a new UUID for a node
    /// </summary>
    /// <returns></returns>
    public static uint InCreateUUID()
    {
        uint id;
        var random = new Random();
        do
        {
            // Make sure the ID is actually unique in the current context
            id = (uint)random.NextInt64(uint.MinValue, InInvalidUUID);
        }
        while (s_takenUUIDs.Contains(id));

        return id;
    }

    /// <summary>
    /// Unloads a single UUID from the internal listing, freeing it up for reuse
    /// </summary>
    /// <param name="id"></param>
    public static void InUnloadUUID(uint id)
    {
        s_takenUUIDs.Remove(id);
    }

    /// <summary>
    /// Clears all UUIDs from the internal listing
    /// </summary>
    public static void InClearUUIDs()
    {
        s_takenUUIDs.Clear();
    }

    private static readonly Dictionary<string, Func<Node?, Node>> s_typeFactories = [];
    public static Node InstantiateNode(string id, Node? parent = null)
    {
        if (s_typeFactories.TryGetValue(id, out var factory))
        {
            return factory(parent);
        }
        throw new ArgumentException($"Node type '{id}' is not registered.");
    }

    public static void RegisterNodeType<T>() where T : Node, new()
    {
        var typeId = typeof(T).GetCustomAttributes(typeof(TypeIdAttribute), false);
        if (typeId.Length > 0)
        {
            var id = ((TypeIdAttribute)typeId[0]).Id;
            s_typeFactories[id] = (Node? parent) => new T(); // Assuming T has a constructor that accepts a Node
        }
        else
        {
            throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
        }
    }

    public static bool HasNodeType(string id)
    {
        return s_typeFactories.ContainsKey(id);
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class TypeIdAttribute(string id) : Attribute
{
    public string Id { get; } = id;
}