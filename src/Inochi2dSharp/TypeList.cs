using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Automations;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Nodes.Drivers;
using Inochi2dSharp.Core.Nodes.MeshGroups;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Core.Nodes.Shape;

namespace Inochi2dSharp;

internal static class TypeList
{
    private static readonly Dictionary<string, Func<Puppet, I2dTime, Automation>> s_autoType = [];

    private static readonly Dictionary<string, Func<I2dCore, Node?, Node>> s_nodeTypes = [];
    public static Node InstantiateNode(string id, I2dCore core, Node? parent = null)
    {
        if (s_nodeTypes.TryGetValue(id, out var factory))
        {
            return factory(core, parent);
        }
        throw new KeyNotFoundException($"Node type '{id}' is not registered.");
    }

    public static Automation InstantiateAutomation(string id, Puppet parent, I2dTime time)
    {
        if (s_autoType.TryGetValue(id, out var factory))
        {
            return factory(parent, time);
        }
        throw new KeyNotFoundException($"No automation type registered with id {id}");
    }

    public static void RegisterNodeType<T>() where T : Node
    {
        var typeId = GetTypeId<T>()
            ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
        s_nodeTypes.Add(typeId, (I2dCore core, Node? parent)
            => (Activator.CreateInstance(typeof(T), core, parent) as T)!);
    }

    public static void RegisterAutomationType<T>() where T : Automation
    {
        var typeId = GetTypeId<T>()
            ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
        s_autoType.Add(typeId, (Puppet parent, I2dTime time)
            => (Activator.CreateInstance(typeof(T), parent, time) as T)!);
    }

    public static bool HasAutomationType(string name)
    {
        return s_autoType.ContainsKey(name);
    }

    public static bool HasNodeType(string name)
    {
        return s_nodeTypes.ContainsKey(name);
    }

    private static string? GetTypeId<T>()
    {
        var attributes = typeof(T).GetCustomAttributes(typeof(TypeIdAttribute), false);
        if (attributes.Length > 0)
        {
            return ((TypeIdAttribute)attributes[0]).Id;
        }
        return null;
    }

    private static void InInitNodes()
    {
        RegisterNodeType<MeshGroup>();
        RegisterNodeType<Part>();
        RegisterNodeType<AnimatedPart>();
        RegisterNodeType<Composite>();
        RegisterNodeType<SimplePhysics>();
        RegisterNodeType<Shapes>();
        RegisterNodeType<Node>();
        RegisterNodeType<TmpNode>();
    }

    private static void InInitAutomations()
    {
        RegisterAutomationType<PhysicsAutomation>();
        RegisterAutomationType<SineAutomation>();
    }

    public static void Init()
    {
        if (s_nodeTypes.Count == 0)
        {
            InInitNodes();
        }
        if (s_autoType.Count == 0)
        {
            InInitAutomations();
        }
    }
}
