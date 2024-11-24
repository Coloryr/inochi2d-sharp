﻿using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Animations;
using Inochi2dSharp.Core.Automations;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Nodes.Drivers;
using Inochi2dSharp.Core.Nodes.MeshGroups;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core;

/// <summary>
/// A puppet
/// </summary>
public class Puppet : IDisposable
{
    private readonly I2dCore _core;

    public const uint NO_THUMBNAIL = uint.MaxValue;

    /// <summary>
    /// An internal puppet root node
    /// </summary>
    private Node _puppetRootNode;

    /// <summary>
    /// A list of parts that are not masked by other parts for Z sorting
    /// </summary>
    public List<Node> RootParts { get; init; } = [];

    /// <summary>
    /// A list of drivers that need to run to update the puppet
    /// </summary>
    public List<Driver> Drivers { get; init; } = [];

    /// <summary>
    /// A list of parameters that are driven by drivers
    /// </summary>
    public Dictionary<Parameter, Driver> DrivenParameters { get; init; } = [];

    /// <summary>
    /// A dictionary of named animations
    /// </summary>
    public Dictionary<string, Animation> Animations { get; init; } = [];

    /// <summary>
    /// Meta information about this puppet
    /// </summary>
    public PuppetMeta Meta;

    /// <summary>
    /// Global physics settings for this puppet
    /// </summary>
    public PuppetPhysics Physics;

    /// <summary>
    /// The root node of the puppet
    /// </summary>
    public Node Root;

    /// <summary>
    /// Parameters
    /// </summary>
    public List<Parameter> Parameters = [];

    /// <summary>
    /// Automations
    /// </summary>
    public List<Automation> Automation = [];

    /// <summary>
    /// INP Texture slots for this puppet
    /// </summary>
    public List<Texture> TextureSlots = [];

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
    /// 
    /// This transform does not affect physics
    /// </summary>
    public Transform Transform;

    /// <summary>
    /// Creates a new puppet from nothing ()
    /// </summary>
    public Puppet(I2dCore core)
    {
        _core = core;
        _puppetRootNode = new Node(core, this);
        Meta = new PuppetMeta();
        Physics = new PuppetPhysics();
        Root = new Node(core, _puppetRootNode)
        {
            name = "Root"
        };
        Transform = new Transform();
    }

    /// <summary>
    /// Creates a new puppet from a node tree
    /// </summary>
    /// <param name="root"></param>
    public Puppet(I2dCore core, Node root)
    {
        _core = core;
        Meta = new PuppetMeta();
        Physics = new PuppetPhysics();
        Root = root;
        _puppetRootNode = new Node(core, this);
        Root.name = "Root";
        ScanParts(Root, true);
        Transform = new Transform();
        SelfSort();
    }

    public void ScanPartsRecurse(Node node, bool driversOnly = false)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Collect Drivers
        if (node is Driver part)
        {
            Drivers.Add(part);
            foreach (Parameter param in part.GetAffectedParameters())
            {
                DrivenParameters[param] = part;
            }
        }
        else if (!driversOnly)
        {
            // Collect drawable nodes only if we aren't inside a Composite node

            if (node is Composite composite)
            {
                // Composite nodes handle and keep their own root node list, as such we should just draw them directly
                composite.ScanParts();
                RootParts.Add(composite);

                // For this subtree, only look for Drivers
                driversOnly = true;
            }
            else if (node is Part part1)
            {
                // Collect Part nodes
                RootParts.Add(part1);
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

    public void ScanParts(Node node, bool reparent = false)
    {
        // We want rootParts to be cleared so that we
        // don't draw the same part multiple times
        // and if the node tree changed we want to reflect those changes
        // not the old node tree.
        RootParts.Clear();

        // Same for drivers
        Drivers.Clear();

        DrivenParameters.Clear();

        ScanPartsRecurse(node);

        // To make sure the GC can collect any nodes that aren't referenced
        // anymore, we clear its children first, then assign its new child
        // to our "new" root node. In some cases the root node will be
        // quite different.
        if (reparent)
        {
            if (_puppetRootNode != null)
                _puppetRootNode.ClearChildren();
            node.Parent = _puppetRootNode;
        }
    }

    public void SelfSort()
    {
        RootParts.Sort((a, b) => b.ZSort.CompareTo(a.ZSort));
    }

    public Node? FindNode(Node n, string name)
    {
        // Name matches!
        if (n.name == name) return n;

        // Recurse through children
        foreach (var child in n.Children)
        {
            if (FindNode(child, name) is { } c) return c;
        }

        // Not found
        return null;
    }

    public Node? FindNode(Node n, uint uuid)
    {
        // Name matches!
        if (n.UUID == uuid) return n;

        // Recurse through children
        foreach (var child in n.Children)
        {
            if (FindNode(child, uuid) is { } c) return c;
        }

        // Not found
        return null;
    }

    /// <summary>
    /// Updates the nodes
    /// </summary>
    public void Update()
    {
        Transform.Update();

        // Update Automators
        foreach (var auto_ in Automation)
        {
            auto_.Update();
        }

        Root.BeginUpdate();

        if (RenderParameters)
        {

            // Update parameters
            foreach (var parameter in Parameters)
            {

                if (!EnableDrivers || !DrivenParameters.ContainsKey(parameter))
                    parameter.Update();
            }
        }

        // Ensure the transform tree is updated
        Root.TransformChanged();

        if (RenderParameters && EnableDrivers)
        {
            // Update parameter/node driver nodes (e.g. physics)
            foreach (var driver in Drivers)
            {
                driver.UpdateDriver();
            }
        }

        // Update nodes
        Root.Update();
    }

    /// <summary>
    /// Reset drivers/physics nodes
    /// </summary>
    public void ResetDrivers()
    {
        foreach (var driver in Drivers)
        {
            driver.Reset();
        }

        // Update so that the timestep gets reset.
        _core.I2dTime.InUpdate();
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
    /// Returns a parameter by UUID
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public Parameter? FindParameter(uint uuid)
    {
        foreach (var parameter in Parameters)
        {
            if (parameter.UUID == uuid)
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
    public void Draw()
    {
        SelfSort();

        foreach (var rootPart in RootParts)
        {
            if (!rootPart.RenderEnabled) continue;
            rootPart.DrawOne();
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
    /// Updates the texture state for all texture slots.
    /// </summary>
    public void UpdateTextureState()
    {
        // Update filtering mode for texture slots
        foreach (var texutre in TextureSlots)
        {
            texutre.SetFiltering(Meta.PreservePixels ? Filtering.Point : Filtering.Linear);
        }
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
    public T? Find<T>(uint uuid) where T : Node
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
    public uint AddTextureToSlot(Texture texture)
    {
        // Add texture if we can't find it.
        if (!TextureSlots.Contains(texture)) TextureSlots.Add(texture);
        return (uint)TextureSlots.Count - 1;
    }

    /// <summary>
    /// Populate texture slots with all visible textures in the model
    /// </summary>
    public void PopulateTextureSlots()
    {
        if (TextureSlots.Count > 0) TextureSlots.Clear();

        foreach (var part in FetAllParts())
        {
            foreach (var texture in part.textures)
            {
                if (texture != null) AddTextureToSlot(texture);
            }
        }
    }

    /// <summary>
    /// Finds a texture by its runtime UUID
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public Texture? FindTextureByRuntimeUUID(uint uuid)
    {
        foreach (var slot in TextureSlots)
        {
            if (slot.UUID != 0)
                return slot;
        }
        return null;
    }

    /// <summary>
    /// Sets thumbnail of this puppet
    /// </summary>
    /// <param name="texture"></param>
    public void SetThumbnail(Texture texture)
    {
        if (Meta.ThumbnailId == NO_THUMBNAIL)
        {
            Meta.ThumbnailId = AddTextureToSlot(texture);
        }
        else
        {
            TextureSlots[(int)Meta.ThumbnailId] = texture;
        }
    }

    /// <summary>
    /// Gets the texture slot index for a texture
    /// 
    /// returns -1 if none was found
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public int GetTextureSlotIndexFor(Texture texture)
    {
        return TextureSlots.IndexOf(texture);
    }

    /// <summary>
    /// Clears this puppet's thumbnail
    /// 
    /// By default it does not delete the texture assigned, pass in true to delete texture
    /// </summary>
    /// <param name="deleteTexture"></param>
    public void ClearThumbnail(bool deleteTexture = false)
    {

        if (deleteTexture)
        {
            TextureSlots.RemoveAll(texture => texture.Id == Meta.ThumbnailId);
        }
        Meta.ThumbnailId = NO_THUMBNAIL;
    }

    /// <summary>
    /// This cursed toString implementation outputs the puppet's
    /// nodetree as a pretty printed tree.
    /// 
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
            s.AppendFormat("{0}[{1}] {2} <{3}>\n", n.Children.Count > 0 ? "╭─" : "", n.TypeId(), n.name, n.UUID);

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

    public void SerializeSelf(JsonObject serializer)
    {
        var obj = new JsonObject();
        Meta.Serialize(obj);
        serializer.Add("meta", obj);
        obj = [];
        Physics.Serialize(obj);
        serializer.Add("physics", obj);
        obj = [];
        Root.SerializePartial(obj);
        serializer.Add("nodes", obj);
        var list = new JsonArray();
        foreach (var item in Parameters)
        {
            var obj1 = new JsonObject();
            item.Serialize(obj1);
            list.Add(obj1);
        }
        serializer.Add("param", list);
        list = [];
        foreach (var item in Automation)
        {
            var obj1 = new JsonObject();
            item.Serialize(obj1);
            list.Add(obj1);
        }
        serializer.Add("automation", list);
        obj = [];
        foreach (var item in Animations)
        {
            var obj1 = new JsonObject();
            item.Value.Serialize(obj1);
            obj.Add(item.Key, obj1);
        }
        serializer.Add("animations", obj);
    }

    /// <summary>
    /// Serializes a puppet
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        SerializeSelf(serializer);
    }

    /// <summary>
    /// Deserializes a puppet
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonObject data)
    {
        Meta = new();
        if (data.TryGetPropertyValue("meta", out var temp) && temp is JsonObject obj)
        {
            Meta.Deserialize(obj);
        }

        Physics = new();
        if (data.TryGetPropertyValue("physics", out temp) && temp is JsonObject obj1)
        {
            Physics.Deserialize(obj1);
        }

        Root = new(_core);
        if (data.TryGetPropertyValue("nodes", out temp) && temp is JsonObject obj2)
        {
            Root.Deserialize(obj2);
        }

        if (data.TryGetPropertyValue("param", out temp) && temp is JsonArray array)
        {
            // Allow parameter loading to be overridden (for Inochi Creator)
            foreach (JsonObject key in array.Cast<JsonObject>())
            {
                var param = new Parameter(_core);
                param.Deserialize(key);
                Parameters.Add(param);
            }
        }

        // Deserialize automation
        if (data.TryGetPropertyValue("automation", out temp) && temp is JsonArray array1)
        {
            foreach (JsonObject key in array1.Cast<JsonObject>())
            {
                if (key.TryGetPropertyValue("type", out var temp1) && temp1 != null)
                {
                    string type = temp1.GetValue<string>();

                    if (TypeList.HasAutomationType(type))
                    {
                        var auto_ = TypeList.InstantiateAutomation(type, this, _core.I2dTime);
                        auto_.Deserialize(key);
                        Automation.Add(auto_);
                    }
                }
               
            }
        }

        if (data.TryGetPropertyValue("animations", out temp) && temp is JsonObject obj3)
        {
            foreach (var obj4 in obj3.Cast<KeyValuePair<string, JsonObject>>())
            {
                var item = new Animation();
                item.Deserialize(obj4.Value);
                Animations.Add(obj4.Key, item);
            }
        }
        FinalizeDeserialization(data);
    }

    public void Reconstruct()
    {
        Root.Reconstruct();
        foreach (var parameter in Parameters.ToArray())
        {
            parameter.Reconstruct(this);
        }
        foreach (var automation_ in Automation.ToArray())
        {
            automation_.Reconstruct(this);
        }
        foreach (var animation in Animations.ToArray())
        {
            animation.Value.Reconstruct(this);
        }
    }

    public void JsonLoadDone()
    {
        Root.Puppet = this;
        Root.name = "Root";
        _puppetRootNode = new Node(_core, this);

        // Finally update link etc.
        Root.JsonLoadDone();
        foreach (var item in Parameters)
        {
            item.JsonLoadDone(this);
        }
        foreach (var item in Automation)
        {
            item.JsonLoadDone(this);
        }
        foreach (var item in Animations)
        {
            item.Value.JsonLoadDone(this);
        }
        ScanParts(Root, true);
        SelfSort();
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    /// <param name="data"></param>
    public void FinalizeDeserialization(JsonObject data)
    {
        // reconstruct object path so that object is located at final position
        Reconstruct();
        JsonLoadDone();
    }

    public void ApplyDeformToChildren()
    {
        var nodes = FindNodesType<MeshGroup>(Root);
        foreach (var node in nodes)
        {
            node.ApplyDeformToChildren(Parameters);
        }
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

    public void Dispose()
    {
        
    }
}
