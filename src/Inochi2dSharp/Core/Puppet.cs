using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Animations;
using Inochi2dSharp.Core.Automations;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Composites;
using Inochi2dSharp.Core.Nodes.Drivers;
using Inochi2dSharp.Core.Nodes.MeshGroups;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core;

/// <summary>
/// A puppet
/// </summary>
public class Puppet
{
    public const uint NO_THUMBNAIL = uint.MaxValue;

    /// <summary>
    /// An internal puppet root node
    /// </summary>
    private Node puppetRootNode;

    /// <summary>
    /// A list of parts that are not masked by other parts for Z sorting
    /// </summary>
    public List<Node> rootParts { get; init; } = [];

    /// <summary>
    /// A list of drivers that need to run to update the puppet
    /// </summary>
    public List<Driver> drivers { get; init; } = [];

    /// <summary>
    /// A list of parameters that are driven by drivers
    /// </summary>
    public Dictionary<Parameter, Driver> drivenParameters { get; init; } = [];

    /// <summary>
    /// A dictionary of named animations
    /// </summary>
    public Dictionary<string, Animation> animations { get; init; } = [];

    /// <summary>
    /// Meta information about this puppet
    /// </summary>
    public PuppetMeta meta;

    /// <summary>
    /// Global physics settings for this puppet
    /// </summary>
    public PuppetPhysics physics;

    /// <summary>
    /// The root node of the puppet
    /// </summary>
    public Node root;

    /// <summary>
    /// Parameters
    /// </summary>
    public List<Parameter> parameters = [];

    /// <summary>
    /// Automations
    /// </summary>
    public List<Automation> automation = [];

    /// <summary>
    /// INP Texture slots for this puppet
    /// </summary>
    public List<Texture> textureSlots = [];

    /// <summary>
    /// Extended vendor data
    /// </summary>
    public Dictionary<string, byte[]> extData;

    /// <summary>
    /// Whether parameters should be rendered
    /// </summary>
    public bool renderParameters = true;

    /// <summary>
    /// Whether drivers should run
    /// </summary>
    public bool enableDrivers = true;

    /// <summary>
    /// Puppet render transform
    /// 
    /// This transform does not affect physics
    /// </summary>
    public Transform transform;

    /// <summary>
    /// Creates a new puppet from nothing ()
    /// </summary>
    public Puppet()
    {
        puppetRootNode = new Node(this);
        meta = new PuppetMeta();
        physics = new PuppetPhysics();
        root = new Node(puppetRootNode)
        {
            name = "Root"
        };
        transform = new Transform();
    }

    /// <summary>
    /// Creates a new puppet from a node tree
    /// </summary>
    /// <param name="root"></param>
    public Puppet(Node root)
    {
        this.meta = new PuppetMeta();
        this.physics = new PuppetPhysics();
        this.root = root;
        this.puppetRootNode = new Node(this);
        this.root.name = "Root";
        this.scanParts(this.root, true);
        transform = new Transform();
        this.selfSort();
    }

    public void scanPartsRecurse(Node node, bool driversOnly = false)
    {
        // Don't need to scan null nodes
        if (node is null) return;

        // Collect Drivers
        if (node is Driver part)
        {
            drivers.Add(part);
            foreach (Parameter param in part.getAffectedParameters())
            {
                drivenParameters[param] = part;
            }
        }
        else if (!driversOnly)
        {
            // Collect drawable nodes only if we aren't inside a Composite node

            if (node is Composite composite)
            {
                // Composite nodes handle and keep their own root node list, as such we should just draw them directly
                composite.scanParts();
                rootParts.Add(composite);

                // For this subtree, only look for Drivers
                driversOnly = true;
            }
            else if (node is Part part1)
            {
                // Collect Part nodes
                rootParts.Add(part1);
            }
            // Non-part nodes just need to be recursed through,
            // they don't draw anything.
        }

        // Recurse through children nodes
        foreach (var child in node.Children)
        {
            scanPartsRecurse(child, driversOnly);
        }
    }

    public void scanParts(Node node, bool reparent = false)
    {
        // We want rootParts to be cleared so that we
        // don't draw the same part multiple times
        // and if the node tree changed we want to reflect those changes
        // not the old node tree.
        rootParts.Clear();

        // Same for drivers
        drivers.Clear();

        drivenParameters.Clear();

        this.scanPartsRecurse(node);

        // To make sure the GC can collect any nodes that aren't referenced
        // anymore, we clear its children first, then assign its new child
        // to our "new" root node. In some cases the root node will be
        // quite different.
        if (reparent)
        {
            if (puppetRootNode != null)
                puppetRootNode.clearChildren();
            node.Parent = puppetRootNode;
        }
    }

    public void selfSort()
    {
        rootParts.Sort((a, b) => a.ZSort.CompareTo(b.ZSort));
    }

    public Node? findNode(Node n, string name)
    {
        // Name matches!
        if (n.name == name) return n;

        // Recurse through children
        foreach (var child in n.Children)
        {
            if (findNode(child, name) is { } c) return c;
        }

        // Not found
        return null;
    }

    public Node? findNode(Node n, uint uuid)
    {

        // Name matches!
        if (n.UUID == uuid) return n;

        // Recurse through children
        foreach (var child in n.Children) {
            if (findNode(child, uuid) is { } c) return c;
        }

        // Not found
        return null;
    }

    /// <summary>
    /// Updates the nodes
    /// </summary>
    public void update()
    {
        transform.Update();

        // Update Automators
        foreach (var auto_ in automation)
        {
            auto_.update();
        }

        root.beginUpdate();

        if (renderParameters)
        {

            // Update parameters
            foreach (var parameter in parameters) {

                if (!enableDrivers || !drivenParameters.ContainsKey(parameter))
                    parameter.update();
            }
        }

        // Ensure the transform tree is updated
        root.transformChanged();

        if (renderParameters && enableDrivers)
        {
            // Update parameter/node driver nodes (e.g. physics)
            foreach (var driver in drivers)
            {
                driver.updateDriver();
            }
        }

        // Update nodes
        root.update();
    }

    /// <summary>
    /// Reset drivers/physics nodes
    /// </summary>
    public void resetDrivers()
    {
        foreach (var driver in drivers)
        {
            driver.reset();
        }

        // Update so that the timestep gets reset.
        Inochi2d.InUpdate();
    }

    /// <summary>
    /// Returns the index of a parameter by name
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public int findParameterIndex(string name)
    {
        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            if (parameter.name == name)
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
    public Parameter? findParameter(uint uuid)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.uuid == uuid)
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
    public bool getIsNodeBound(Node n)
    {
        foreach (var parameter in parameters)
        {
            if (parameter.hasAnyBinding(n)) return true;
        }
        return false;
    }

    /// <summary>
    /// Draws the puppet
    /// </summary>
    public void draw()
    {
        this.selfSort();

        foreach (var rootPart in rootParts)
        {
            if (!rootPart.RenderEnabled) continue;
            rootPart.drawOne();
        }
    }

    /// <summary>
    /// Removes a parameter from this puppet
    /// </summary>
    /// <param name="param"></param>
    public void removeParameter(Parameter param)
    {
        var idx = parameters.IndexOf(param);
        if (idx >= 0)
        {
            parameters.RemoveAt(idx);
        }
    }

    /// <summary>
    /// Rescans the puppet's nodes
    /// 
    /// Run this every time you change the layout of the puppet's node tree
    /// </summary>
    public void rescanNodes()
    {
        this.scanParts(root, false);
    }

    /// <summary>
    /// Updates the texture state for all texture slots.
    /// </summary>
    public void updateTextureState()
    {
        // Update filtering mode for texture slots
        foreach (var texutre in textureSlots)
        {
            texutre.SetFiltering(meta.PreservePixels ? Filtering.Point : Filtering.Linear);
        }
    }

    /// <summary>
    /// Finds Node by its name
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="name"></param>
    /// <returns></returns>
    public T? find<T>(string name) where T : Node
    {
        return (T?)findNode(root, name);
    }

    /// <summary>
    /// Finds Node by its unique id
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public T? find<T>(uint uuid) where T : Node
    {
        return (T?)findNode(root, uuid);
    }

    /// <summary>
    /// Returns all the parts in the puppet
    /// </summary>
    /// <returns></returns>
    public Part[] getAllParts()
    {
        return findNodesType<Part>(root);
    }

    /// <summary>
    /// Finds nodes based on their type
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="n"></param>
    /// <returns></returns>
    public T[] findNodesType<T>(Node node) where T : Node
    {
        var nodes = new List<T>();

        if (node is T item)
        {
            nodes.Add(item);
        }

        // Recurse through children
        foreach (var child in node.Children)
        {
            nodes.AddRange(findNodesType<T>(child));
        }

        return [.. nodes];
    }

    /// <summary>
    /// Adds a texture to a new slot if it doesn't already exist within this puppet
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public uint addTextureToSlot(Texture texture)
    {
        // Add texture if we can't find it.
        if (!textureSlots.Contains(texture)) textureSlots.Add(texture);
        return (uint)textureSlots.Count - 1;
    }

    /// <summary>
    /// Populate texture slots with all visible textures in the model
    /// </summary>
    public void populateTextureSlots()
    {
        if (textureSlots.Count > 0) textureSlots.Clear();

        foreach (var part in getAllParts())
        {
            foreach (var texture in part.textures)
            {
                if (texture != null) this.addTextureToSlot(texture);
            }
        }
    }

    /// <summary>
    /// Finds a texture by its runtime UUID
    /// </summary>
    /// <param name="uuid"></param>
    /// <returns></returns>
    public Texture? findTextureByRuntimeUUID(uint uuid)
    {
        foreach (var slot in textureSlots)
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
    public void setThumbnail(Texture texture)
    {
        if (meta.ThumbnailId == NO_THUMBNAIL)
        {
            meta.ThumbnailId = addTextureToSlot(texture);
        }
        else
        {
            textureSlots[(int)meta.ThumbnailId] = texture;
        }
    }

    /// <summary>
    /// Gets the texture slot index for a texture
    /// 
    /// returns -1 if none was found
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    public int getTextureSlotIndexFor(Texture texture)
    {
        return textureSlots.IndexOf(texture);
    }

    /// <summary>
    /// Clears this puppet's thumbnail
    /// 
    /// By default it does not delete the texture assigned, pass in true to delete texture
    /// </summary>
    /// <param name="deleteTexture"></param>
    public void clearThumbnail(bool deleteTexture = false)
    {

        if (deleteTexture)
        {
            textureSlots.RemoveAll(texture => texture.Id == meta.ThumbnailId);
        }
        meta.ThumbnailId = NO_THUMBNAIL;
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

        return ToStringBranch(root, 0);
    }

    public void serializeSelf(JObject serializer)
    {
        serializer.Add("meta", new JObject(meta));
        serializer.Add("physics", new JObject(physics));
        var obj = new JObject();
        root.serializePartial(obj);
        serializer.Add("nodes", obj);
        var list = new JArray();
        foreach (var item in parameters)
        {
            var obj1 = new JObject();
            item.Serialize(obj1);
            list.Add(obj1);
        }
        serializer.Add("param", list);
        list = [];
        foreach (var item in automation)
        {
            var obj1 = new JObject();
            item.serialize(obj1);
            list.Add(obj1);
        }
        serializer.Add("automation", list);
        list = [];
        foreach (var item in animations)
        {
            var obj1 = new JObject();
            item.Value.serialize(obj1);
            list.Add(new JProperty(item.Key, obj1));
        }
        serializer.Add("animations", list);
    }

    /// <summary>
    /// Serializes a puppet
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializeSelf(serializer);
    }

    /// <summary>
    /// Deserializes a puppet
    /// </summary>
    /// <param name="data"></param>
    public void deserialize(JObject data)
    {
        var temp = data["meta"];
        if (temp != null)
        {
            meta = temp.ToObject<PuppetMeta>()!;
        }

        temp = data["physics"];
        if (temp != null)
        {
            physics = temp.ToObject<PuppetPhysics>()!;
        }

        temp = data["nodes"];
        if (temp is JObject obj)
        {
            root = new();
            root.Deserialize(obj);
        }

        temp = data["param"];
        if (temp != null)
        {
            // Allow parameter loading to be overridden (for Inochi Creator)
            foreach (JObject key in temp)
            {
                var param = new Parameter();
                param.Deserialize(key);
                parameters.Add(param);
            }
        }

        // Deserialize automation
        temp = data["automation"];
        if (temp is JArray array)
        {
            foreach (JObject key in array.Cast<JObject>())
            {
                string type = key["type"]!.ToString();

                if (AutomationHelper.HasAutomationType(type))
                {
                    var auto_ = AutomationHelper.InstantiateAutomation(type, this);
                    auto_.deserialize(key);
                    automation.Add(auto_);
                }
            }
        }

        temp = data["animations"];
        if (temp != null)
        {
            foreach (JProperty obj1 in temp.Cast<JProperty>())
            {
                var item = new Animation();
                item.deserialize(obj1.Value as JObject);
                animations.Add(obj1.Name, item);
            }
        }
        finalizeDeserialization(data);
    }

    public void reconstruct()
    {
        root.reconstruct();
        foreach (var parameter in parameters.ToArray())
        {
            parameter.reconstruct(this);
        }
        foreach (var automation_ in automation.ToArray())
        {
            automation_.reconstruct(this);
        }
        foreach (var animation in animations.ToArray())
        {
            animation.Value.reconstruct(this);
        }
    }

    public void finalize()
    {
        root.Puppet = this;
        root.name = "Root";
        puppetRootNode = new Node(this);

        // Finally update link etc.
        root.finalize();
        foreach (var parameter in parameters)
        {
            parameter.finalize(this);
        }
        foreach (var automation_ in automation)
        {
            automation_.finalize(this);
        }
        foreach (var animation in animations)
        {
            animation.Value.finalize(this);
        }
        scanParts(root, true);
        selfSort();
    }

    /// <summary>
    /// Finalizer
    /// </summary>
    /// <param name="data"></param>
    public void finalizeDeserialization(JObject data)
    {
        // reconstruct object path so that object is located at final position
        reconstruct();
        finalize();
    }


    public void applyDeformToChildren()
    {
        var nodes = findNodesType<MeshGroup>(root);
        foreach (var node in nodes) 
        {
            node.applyDeformToChildren(parameters);
        }
    }

    /// <summary>
    /// Gets the combined bounds of the puppet
    /// </summary>
    /// <param name="reupdate"></param>
    /// <returns></returns>
    public Vector4 getCombinedBounds(bool reupdate = false)
    {
        return root.getCombinedBounds(reupdate, true);
    }
}
