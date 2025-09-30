using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Nodes.Deformers;
using Inochi2dSharp.Core.Nodes.Drawables;
using Inochi2dSharp.Core.Nodes.Drivers;

namespace Inochi2dSharp.Core;

public static class TypeList
{
    //private static readonly Dictionary<string, Func<Puppet, I2dTime, Automation>> s_autoType = [];

    private static readonly List<TypeIdAttribute> s_nodeTypeIdStore = [];
    private static readonly Dictionary<string, Func<Node?, Node>> s_nodeFactoryStoreS = [];
    private static readonly Dictionary<uint, Func<Node?, Node>> s_nodeFactoryStoreN = [];

    static TypeList()
    {
        Init();
    }

    public static Node InstantiateNode(string guid, Node? parent = null)
    {
        if (s_nodeFactoryStoreS.TryGetValue(guid, out var factory))
        {
            return factory(parent);
        }
        throw new KeyNotFoundException($"Node type '{guid}' is not registered.");
    }

    public static Node InstantiateNode(uint nid, Node? parent = null)
    {
        if (s_nodeFactoryStoreN.TryGetValue(nid, out var factory))
        {
            return factory(parent);
        }
        throw new KeyNotFoundException($"Node type '{nid}' is not registered.");
    }

    //public static Automation InstantiateAutomation(string id, Puppet parent, I2dTime time)
    //{
    //    if (s_autoType.TryGetValue(id, out var factory))
    //    {
    //        return factory(parent, time);
    //    }
    //    throw new KeyNotFoundException($"No automation type registered with id {id}");
    //}

    public static void RegisterNodeType<T>() where T : Node
    {
        var typeId = GetTypeId<T>()
            ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
        s_nodeTypeIdStore.Add(typeId);

        if (!typeId.IsAbstract)
        {
            s_nodeFactoryStoreS.Add(typeId.Sid, parent => (Activator.CreateInstance(typeof(T), parent) as T)!);
            s_nodeFactoryStoreN.Add(typeId.Nid, parent => (Activator.CreateInstance(typeof(T), parent) as T)!);
        }
    }

    //public static void RegisterAutomationType<T>() where T : Automation
    //{
    //    var typeId = GetTypeId<T>()
    //        ?? throw new InvalidOperationException($"Type {typeof(T).Name} does not have a TypeId attribute.");
    //    s_autoType.Add(typeId, (Puppet parent, I2dTime time)
    //        => (Activator.CreateInstance(typeof(T), parent, time) as T)!);
    //}

    //public static bool HasAutomationType(string name)
    //{
    //    return s_autoType.ContainsKey(name);
    //}

    public static bool HasNodeType(string guid)
    {
        return s_nodeTypeIdStore.Any(item => item.Sid == guid);
    }

    public static bool HasNodeType(uint nid)
    {
        return s_nodeTypeIdStore.Any(item => item.Nid == nid);
    }

    private static TypeIdAttribute? GetTypeId<T>()
    {
        var attributes = typeof(T).GetCustomAttributes(typeof(TypeIdAttribute), false);
        if (attributes.Length > 0)
        {
            return (TypeIdAttribute)attributes[0];
        }
        return null;
    }

    public static TypeIdAttribute GetTypeId(object obj)
    {
        var attributes = obj.GetType().GetCustomAttributes(typeof(TypeIdAttribute), false);
        return (TypeIdAttribute)attributes[0];
    }

    private static void InInitNodes()
    {
        RegisterNodeType<Node>();
        RegisterNodeType<Composite>();
        RegisterNodeType<Deformer>();
        RegisterNodeType<LatticeDeformer>();
        RegisterNodeType<MeshDeformer>();
        RegisterNodeType<Drawable>();
        RegisterNodeType<Part>();
        RegisterNodeType<AnimatedPart>();
        RegisterNodeType<Driver>();
        RegisterNodeType<SimplePhysics>();
    }

    //private static void InInitAutomations()
    //{
    //    RegisterAutomationType<PhysicsAutomation>();
    //    RegisterAutomationType<SineAutomation>();
    //}

    public static void Init()
    {
        if (s_nodeTypeIdStore.Count == 0)
        {
            InInitNodes();
        }
        //if (s_autoType.Count == 0)
        //{
        //    InInitAutomations();
        //}
    }
}
