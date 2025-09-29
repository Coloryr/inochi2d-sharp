using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Animations;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Nodes.Drawables;
using Inochi2dSharp.Core.Nodes.Drivers;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core;

/// <summary>
/// A puppet
/// </summary>
public class Puppet : IDisposable
{
    /// <summary>
    /// The drawlist that the puppet passes to its nodes.
    /// </summary>
    private readonly DrawList _drawList;

    /// <summary>
    /// An internal puppet root node
    /// </summary>
    private Node _puppetRootNode;

    /// <summary>
    /// A list of parts that are not masked by other parts for Z sorting
    /// </summary>
    private readonly List<Node> _rootParts = [];

    /// <summary>
    /// A list of drivers that need to run to update the puppet
    /// </summary>
    private readonly List<Driver> _drivers = [];

    /// <summary>
    /// A list of parameters that are driven by drivers
    /// </summary>
    private readonly Dictionary<Parameter, Driver> _drivenParameters = [];

    /// <summary>
    /// A dictionary of named animations
    /// </summary>
    private readonly Dictionary<string, Animation> _animations = [];

    /// <summary>
    /// Meta information about this puppet
    /// </summary>
    public PuppetMeta Meta { get; private set; }

    /// <summary>
    /// Global physics settings for this puppet
    /// </summary>
    public PuppetPhysics Physics { get; private set; }

    /// <summary>
    /// The root node of the puppet
    /// </summary>
    public Node Root { get; private set; }

    /// <summary>
    /// Parameters
    /// </summary>
    public List<Parameter> Parameters = [];

    /// <summary>
    /// INP Texture slots for this puppet
    /// </summary>
    public TextureCache TextureCache;

    /// <summary>
    /// Extended vendor data
    /// </summary>
    public Dictionary<string, byte[]> ExtData = [];

    /// <summary>
    /// Whether parameters should be rendered
    /// </summary>
    public bool RenderParameters = true;

    /// <summary>
    /// Whether drivers should run
    /// </summary>
    public bool EnableDrivers = true;

    /// <summary>
    /// Puppet render transform
    /// <br/>
    /// This transform does not affect physics
    /// </summary>
    public Transform Transform;
    /// <summary>
    /// The active draw list for the puppet.
    /// </summary>
    public DrawList DrawList => _drawList;

    /// <summary>
    /// Creates a new puppet from nothing ()
    /// </summary>
    public Puppet(TextureCache? cache = null)
    {
        _puppetRootNode = new Node(this);
        Root = new Node(_puppetRootNode)
        {
            Name = "Root"
        };
        Meta = new PuppetMeta();
        Physics = new PuppetPhysics();
        Transform = new Transform();

        TextureCache = cache ?? new();
        _drawList = new();
    }

    /// <summary>
    /// Creates a new puppet from a node tree
    /// </summary>
    /// <param name="root"></param>
    public Puppet(Node root)
    {
        Meta = new PuppetMeta();
        Physics = new PuppetPhysics();
        Root = root;
        _puppetRootNode = new Node(this);
        Root.Name = "Root";
        ScanParts(Root, true);
        Transform = new Transform();
        SelfSort();

        TextureCache = new();
        _drawList = new();
    }

    private void ScanPartsRecurse(Node node, bool driversOnly = false)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Collect Drivers
        if (node is Driver part)
        {
            _drivers.Add(part);
            foreach (Parameter param in part.AffectedParameters)
            {
                _drivenParameters[param] = part;
            }
        }
        else if (!driversOnly)
        {
            // Collect drawable nodes only if we aren't inside a Composite node

            if (node is Composite composite)
            {
                // Composite nodes handle and keep their own root node list, as such we should just draw them directly
                composite.ScanParts();
                _rootParts.Add(composite);

                // For this subtree, only look for Drivers
                driversOnly = true;
            }
            else if (node is Part part1)
            {
                // Collect Part nodes
                _rootParts.Add(part1);
            }
            // Non-part nodes just need to be recursed through,
            // they don't draw anything.
        }

        // Recurse through children nodes
        foreach (var child in node.Children)
        {
            ScanPartsRecurse(child, driversOnly);
        }
    }

    private void ScanParts(Node node, bool reparent = false)
    {
        // We want rootParts to be cleared so that we
        // don't draw the same part multiple times
        // and if the node tree changed we want to reflect those changes
        // not the old node tree.
        _rootParts.Clear();

        // Same for drivers
        _drivers.Clear();
        _drivenParameters.Clear();

        ScanPartsRecurse(node);

        // To make sure the GC can collect any nodes that aren't referenced
        // anymore, we clear its children first, then assign its new child
        // to our "new" root node. In some cases the root node will be
        // quite different.
        if (reparent)
        {
            _puppetRootNode?.ClearChildren();
            node.Parent = _puppetRootNode;
        }
    }

    private void SelfSort()
    {
        _rootParts.Sort((a, b) => b.ZSort.CompareTo(a.ZSort));
    }

    private static Node? FindNode(Node n, string name)
    {
        // Name matches!
        if (n.Name == name) return n;

        // Recurse through children
        foreach (var child in n.Children)
        {
            if (FindNode(child, name) is { } c) return c;
        }

        // Not found
        return null;
    }

    private static Node? FindNode(Node n, Guid guid)
    {
        // Name matches!
        if (n.Guid == guid) return n;

        // Recurse through children
        foreach (var child in n.Children)
        {
            if (FindNode(child, guid) is { } c) return c;
        }

        // Not found
        return null;
    }

    /// <summary>
    /// Serializes a puppet into an existing object.
    /// </summary>
    /// <param name="obj"></param>
    public void Serialize(JsonObject obj)
    {
        var obj1 = new JsonObject();
        Meta.Serialize(obj1);
        obj["meta"] = obj1;

        obj1 = [];
        Physics.Serialize(obj1);
        obj.Add("physics", obj1);

        obj1 = [];
        Root.Serialize(obj1);
        obj.Add("nodes", obj1);

        var list = new JsonArray();
        foreach (var item in Parameters)
        {
            obj1 = [];
            item.Serialize(obj1);
            list.Add(obj1);
        }
        obj.Add("param", list);

        obj1 = [];
        foreach (var item in _animations)
        {
            var obj2 = new JsonObject();
            item.Value.Serialize(obj2);
            obj1.Add(item.Key, obj1);
        }
        obj.Add("animations", obj1);
    }

    /// <summary>
    /// Deserializes a puppet
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        // Invalid type.
        if (data.ValueKind != JsonValueKind.Object)
        {
            return;
        }
        Meta = new();
        Physics = new();
        Root = new();
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "meta" && item.Value.ValueKind == JsonValueKind.Object)
            {
                Meta.Deserialize(item.Value);
            }
            else if (item.Name == "physics" && item.Value.ValueKind == JsonValueKind.Object)
            {
                Physics.Deserialize(item.Value);
            }
            else if (item.Name == "nodes" && item.Value.ValueKind == JsonValueKind.Object)
            {
                Root.Deserialize(item.Value);
            }
            else if (item.Name == "param" && item.Value.ValueKind == JsonValueKind.Array)
            {
                // Allow parameter loading to be overridden (for Inochi Creator)
                foreach (JsonElement key in item.Value.EnumerateArray())
                {
                    var param = new Parameter();
                    param.Deserialize(key);
                    Parameters.Add(param);
                }
            }
            else if (item.Name == "animations" && item.Value.ValueKind == JsonValueKind.Object)
            {
                foreach (var obj4 in item.Value.EnumerateObject())
                {
                    if (obj4.Value.ValueKind == JsonValueKind.Object)
                    {
                        var item1 = new Animation();
                        item1.Deserialize(obj4.Value);
                        _animations.Add(obj4.Name, item1);
                    }
                }
            }
        }
        Reconstruct();
        Finalized();
    }

    protected void Reconstruct()
    {
        Root.Reconstruct();
        foreach (var parameter in Parameters.ToArray())
        {
            parameter.Reconstruct(this);
        }
        foreach (var animation in _animations.ToArray())
        {
            animation.Value.Reconstruct(this);
        }
    }

    protected void Finalized()
    {
        Root.Puppet = this;
        Root.Name = "Root";
        _puppetRootNode = new Node(this);

        // Finally update link etc.
        Root.Finalized();
        foreach (var parameter in Parameters)
        {
            parameter.Finalized(this);
        }
        foreach (var animation in _animations)
        {
            animation.Value.Finalized(this);
        }
        ScanParts(Root, true);
        SelfSort();
    }

    /// <summary>
    /// Updates the nodes
    /// </summary>
    public void Update(float delta)
    {
        _drawList.Clear();
        Transform.Update();
        Root.PreUpdate(_drawList);

        if (RenderParameters)
        {
            // Update parameters
            foreach (var parameter in Parameters)
            {
                if (!EnableDrivers || !_drivenParameters.ContainsKey(parameter))
                    parameter.Update();
            }
        }

        // Ensure the transform tree is updated
        Root.TransformChanged();

        if (RenderParameters && EnableDrivers)
        {
            // Update parameter/node driver nodes (e.g. physics)
            foreach (var driver in _drivers)
            {
                driver.UpdateDriver(delta);
            }
        }

        // Update nodes
        Root.Update(delta, _drawList);
        Root.PostUpdate(_drawList);
    }

    /// <summary>
    /// Reset drivers/physics nodes
    /// </summary>
    public void ResetDrivers()
    {
        foreach (var driver in _drivers)
        {
            driver.Reset();
        }
    }

    /// <summary>
    /// Returns the index of a parameter by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int FindParameterIndex(string name)
    {
        for (int i = 0; i < Parameters.Count; i++)
        {
            var parameter = Parameters[i];
            if (parameter.Name == name)
            {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// Returns a parameter by Guid
    /// </summary>
    /// <param name="guid"></param>
    /// <returns></returns>
    public Parameter? FindParameter(Guid guid)
    {
        foreach (var parameter in Parameters)
        {
            if (parameter.Guid == guid)
            {
                return parameter;
            }
        }
        return null;
    }

    /// <summary>
    /// Gets if a node is bound to ANY parameter.
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public bool GetIsNodeBound(Node n)
    {
        foreach (var parameter in Parameters)
        {
            if (parameter.HasAnyBinding(n)) return true;
        }
        return false;
    }

    /// <summary>
    /// Draws the puppet
    /// </summary>
    public void Draw(float delta)
    {
        SelfSort();

        foreach (var rootPart in _rootParts)
        {
            if (!rootPart.RenderEnabled)
            {
                continue;
            }
            rootPart.Draw(delta, _drawList);
        }
    }

    /// <summary>
    /// Removes a parameter from this puppet
    /// </summary>
    /// <param name="param"></param>
    public void RemoveParameter(Parameter param)
    {
        var idx = Parameters.IndexOf(param);
        if (idx >= 0)
        {
            Parameters.RemoveAt(idx);
        }
    }

    /// <summary>
    /// Rescans the puppet's nodes
    /// 
    /// Run this every time you change the layout of the puppet's node tree
    /// </summary>
    public void RescanNodes()
    {
        ScanParts(Root, false);
    }

    /// <summary>
    /// Finds Node by its name
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T? Find<T>(string name) where T : Node
    {
        return (T?)FindNode(Root, name);
    }

    /// <summary>
    /// Finds Node by its unique id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public T? Find<T>(Guid uuid) where T : Node
    {
        return (T?)FindNode(Root, uuid);
    }

    /// <summary>
    /// Returns all the parts in the puppet
    /// </summary>
    /// <returns></returns>
    public Part[] FetAllParts()
    {
        return FindNodesType<Part>(Root);
    }

    /// <summary>
    /// Finds nodes based on their type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="n"></param>
    /// <returns></returns>
    public T[] FindNodesType<T>(Node node) where T : Node
    {
        var nodes = new List<T>();

        if (node is T item)
        {
            nodes.Add(item);
        }

        // Recurse through children
        foreach (var child in node.Children)
        {
            nodes.AddRange(FindNodesType<T>(child));
        }

        return [.. nodes];
    }

    /// <summary>
    /// Adds a texture to a new slot if it doesn't already exist within this puppet
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public int AddTextureToSlot(Texture texture)
    {
        return TextureCache.Add(texture);
    }

    /// <summary>
    /// Sets thumbnail of this puppet
    /// </summary>
    /// <param name="texture"></param>
    public void SetThumbnail(Texture texture)
    {
        Meta.ThumbnailId = (uint)TextureCache.Add(texture);
    }

    /// <summary>
    /// Gets the texture slot index for a texture
    /// <br/>
    /// returns -1 if none was found
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public int GetTextureSlotIndexFor(Texture texture)
    {
        return TextureCache.Find(texture);
    }

    /// <summary>
    /// This cursed toString implementation outputs the puppet's
    /// nodetree as a pretty printed tree.
    /// <br/>
    /// Please use a graphical viewer instead of this if you can,
    /// eg. Inochi Creator.
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        var lineSet = new List<bool>();

        string ToStringBranch(Node n, int indent, bool showLines = true)
        {
            lineSet.Add(n.Children.Count > 0);

            string GetLineSet()
            {
                if (indent == 0) return "";
                var sb = new StringBuilder();
                for (int i = 1; i < lineSet.Count; i++)
                {
                    sb.Append(lineSet[i - 1] ? "│ " : "  ");
                }
                return sb.ToString();
            }

            string iden = GetLineSet();
            var s = new StringBuilder();
            s.AppendFormat("{0}[{1}] {2} <{3}>\n", n.Children.Count > 0 ? "╭─" : "", n.TypeId.Sid, n.Name, n.Guid.ToString());

            for (int i = 0; i < n.Children.Count; i++)
            {
                var child = n.Children[i];
                string term = "├→";
                if (i == n.Children.Count - 1)
                {
                    term = "╰→";
                    lineSet[indent] = false;
                }
                s.AppendFormat("{0}{1}{2}", iden, term, ToStringBranch(child, indent + 1));
            }

            lineSet.RemoveAt(lineSet.Count - 1);

            return s.ToString();
        }

        return ToStringBranch(Root, 0);
    }

    public string GetParametersString()
    {
        var str = new StringBuilder();

        int nameLength = Parameters.Max(d => d.Name.Length);
        int isVec2Length = Parameters.Max(d => d.IsVec2.ToString().Length);
        int nowLength = Parameters.Max(d => d.Value.ToString().Length);
        int minLength = Parameters.Max(d => d.Min.ToString().Length);
        int defaultLength = Parameters.Max(d => d.Defaults.ToString().Length);
        int maxLength = Parameters.Max(d => d.Max.ToString().Length);

        int arg1 = int.Max(nameLength, minLength - 4);
        int arg2 = int.Max(isVec2Length, defaultLength);
        int arg3 = int.Max(nowLength, maxLength);

        foreach (var data in Parameters)
        {
            string formattedOutput =
                $"Name: {data.Name.PadRight(arg1)} " +
                $"IsVec2: {data.IsVec2.ToString().PadRight(arg2)}  " +
                $"Now: {data.Value.ToString().PadRight(arg3)}\n" +
                $"     Min: {data.Min.ToString().PadRight(arg1 - 4)} " +
                $"Default: {data.Defaults.ToString().PadRight(arg2)} " +
                $"Max: {data.Max.ToString().PadRight(arg3)}";

            str.AppendLine(formattedOutput);
        }

        return str.ToString();
    }

    public void Dispose()
    {
        _animations.Clear();
        Parameters.Clear();
        foreach (var item in _rootParts)
        {
            item.Dispose();
        }
        _rootParts.Clear();
        foreach (var item in _drivers)
        {
            item.Dispose();
        }
        _drivers.Clear();
        if (Root != null)
        {
            Root.Dispose();
            Root = null!;
        }
        if (_puppetRootNode != null)
        {
            _puppetRootNode.Dispose();
            _puppetRootNode = null!;
        }
    }

    /// <summary>
    /// Gets the internal root parts array 
    /// <br/>
    /// Do note that some root parts may be Composites instead.
    /// </summary>
    /// <returns></returns>
    public List<Node> GetRootParts()
    {
        return _rootParts;
    }

    /// <summary>
    /// Gets a list of drivers
    /// </summary>
    /// <returns></returns>
    public List<Driver> GetDrivers()
    {
        return _drivers;
    }

    /// <summary>
    /// Gets a mapping from parameters to their drivers
    /// </summary>
    /// <returns></returns>
    public Dictionary<Parameter, Driver> GetParameterDrivers()
    {
        return _drivenParameters;
    }

    /// <summary>
    /// Gets the animation dictionary
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, Animation> GetAnimations()
    {
        return _animations;
    }

    /// <summary>
    /// Gets the combined bounds of the puppet
    /// </summary>
    /// <param name="reupdate"></param>
    /// <returns></returns>
    public Vector4 GetCombinedBounds(bool reupdate = false)
    {
        return Root.GetCombinedBounds(reupdate, true);
    }
}
