using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Drivers;
using Inochi2dSharp.Core.Nodes.Parts;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core;

/// <summary>
/// A puppet
/// </summary>
public class Puppet
{
    /// <summary>
    /// An internal puppet root node
    /// </summary>
    private Node puppetRootNode;

    /// <summary>
    /// A list of parts that are not masked by other parts for Z sorting
    /// </summary>
    private List<Node> rootParts = [];

    /// <summary>
    /// A list of drivers that need to run to update the puppet
    /// </summary>
    private List<Driver> drivers = [];

    /// <summary>
    /// A list of parameters that are driven by drivers
    /// </summary>
    private Dictionary<Parameter, Driver> drivenParameters = [];

    /// <summary>
    /// A dictionary of named animations
    /// </summary>
    private Dictionary<string, Animation> animations = [];

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
    public Parameter[] parameters;

    /// <summary>
    /// Automations
    /// </summary>
    public Automation[] automation;

    /// <summary>
    /// INP Texture slots for this puppet
    /// </summary>
    public Texture[] textureSlots;

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
            else if (node is Part part) 
            {
                // Collect Part nodes
                rootParts.Add(part);
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
        rootParts = [];

        // Same for drivers
        drivers = [];
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
}
