using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Core.Nodes.Drivers;

namespace Inochi2dSharp.Core;

/// <summary>
/// A puppet
/// </summary>
public class Puppet
{
    /// <summary>
    /// An internal puppet root node
    /// </summary>
    public Node puppetRootNode;

    /// <summary>
    /// A list of parts that are not masked by other parts for Z sorting
    /// </summary>
    public Node[] rootParts;

    /// <summary>
    /// A list of drivers that need to run to update the puppet
    /// </summary>
    public Driver[] drivers;

    /// <summary>
    /// A list of parameters that are driven by drivers
    /// </summary>
    public Driver[Parameter] drivenParameters;

    /// <summary>
    /// A dictionary of named animations
    /// </summary>
    public Animation[string] animations;

    public void scanPartsRecurse(ref Node node, bool driversOnly = false)
    {

        // Don't need to scan null nodes
        if (node is null) return;

        // Collect Drivers
        if (Driver part = cast(Driver)node) {
            drivers ~= part;
            foreach (Parameter param; part.getAffectedParameters())
                drivenParameters[param] = part;
        } else if (!driversOnly)
        {
            // Collect drawable nodes only if we aren't inside a Composite node

            if (Composite composite = cast(Composite)node) {
                // Composite nodes handle and keep their own root node list, as such we should just draw them directly
                composite.scanParts();
                rootParts ~= composite;

                // For this subtree, only look for Drivers
                driversOnly = true;
            } else if (Part part = cast(Part)node) {
                // Collect Part nodes
                rootParts ~= part;
            }
            // Non-part nodes just need to be recursed through,
            // they don't draw anything.
        }

        // Recurse through children nodes
        foreach (child; node.children) {
            scanPartsRecurse(child, driversOnly);
        }
    }

    public void scanParts(bool reparent = false)
    {

        // We want rootParts to be cleared so that we
        // don't draw the same part multiple times
        // and if the node tree changed we want to reflect those changes
        // not the old node tree.
        rootParts = [];

        // Same for drivers
        drivers = [];
        drivenParameters.clear();

        this.scanPartsRecurse(node);

        // To make sure the GC can collect any nodes that aren't referenced
        // anymore, we clear its children first, then assign its new child
        // to our "new" root node. In some cases the root node will be
        // quite different.
        if (reparent) {
            if (puppetRootNode! is null) puppetRootNode.clearChildren();
            node.parent = puppetRootNode;
        }
    }

    public void selfSort()
    {
        sort!((a, b) => cmp(
            a.zSort,
            b.zSort) > 0, SwapStrategy.stable)(rootParts);
    }

    public Node findNode(Node n, string name)
    {

        // Name matches!
        if (n.name == name) return n;

        // Recurse through children
        foreach (child; n.children) {
            if (Node c = findNode(child, name)) return c;
        }

        // Not found
        return null;
    }

    public Node findNode(Node n, uint uuid)
    {

        // Name matches!
        if (n.uuid == uuid) return n;

        // Recurse through children
        foreach (child; n.children) {
            if (Node c = findNode(child, uuid)) return c;
        }

        // Not found
        return null;
    }
}
