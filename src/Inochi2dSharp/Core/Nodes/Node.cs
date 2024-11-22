using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes;

public record MatrixHolder
{
    public Matrix4x4 Matrix;

    public MatrixHolder(Matrix4x4 matrix) => Matrix = matrix;
}

public class Node
{
    public Puppet Puppet
    { 
        get => Parent != null ? Parent.Puppet : _puppet;
        set => _puppet = value;
    }

    private Puppet _puppet;

    /// <summary>
    /// Gets the parent of this node
    /// </summary>
    public Node? Parent { get; private set; }

    /// <summary>
    /// Gets a list of this node's children
    /// </summary>
    public List<Node> Children { get; private set; } = [];
    /// <summary>
    /// Returns the unique identifier for this node
    /// </summary>
    public uint UUID { get; private set; }

    /// <summary>
    /// Gets the relative Z sorting
    /// </summary>
    public float RelZSort
    {
        get => _zsort;
    }

    /// <summary>
    /// Gets the basis zSort offset.
    /// </summary>
    public float ZSortBase
    { 
        get => Parent != null ? Parent.ZSort : 0;
    }

    /// <summary>
    /// Z sorting
    /// </summary>
    public float ZSort
    {
        get => ZSortBase + RelZSort + offsetSort;
        set => _zsort = value;
    }

    /// <summary>
    /// the Z sorting without parameter offsets
    /// </summary>
    public float ZSortNoOffset
    { 
        get => ZSortBase + RelZSort;
    }

    private float _zsort;

    /// <summary>
    /// Lock translation to root
    /// </summary>
    public bool LockToRoot
    {
        get => _lockToRoot;
        set
        {
            // Automatically handle converting lock space and proper world space.
            if (value && !_lockToRoot)
            {
                localTransform.Translation = transformNoLock().Translation;
            }
            else if (!value && _lockToRoot)
            {
                localTransform.Translation = localTransform.Translation - Parent!.transformNoLock().Translation;
            }

            _lockToRoot = value;

        }
    }

    private bool _lockToRoot;

    public string NodePath { get; private set; }

    protected bool preProcessed = false;
    protected bool postProcessed = false;

    /// <summary>
    /// The offset to the transform to apply
    /// </summary>
    protected Transform offsetTransform;

    /// <summary>
    /// The offset to apply to sorting
    /// </summary>
    protected float offsetSort = 0f;

    protected unsafe Matrix4x4* oneTimeTransform = null;

    public MatrixHolder? overrideTransformMatrix = null;

    //Matrix4x4*
    public unsafe Func<Vector2[], Vector2[], IntPtr, (Vector2[], IntPtr)>? preProcessFilter = null;
    //Matrix4x4*
    public unsafe Func<Vector2[], Vector2[], IntPtr, (Vector2[], IntPtr)>? postProcessFilter = null;

    /// <summary>
    /// Whether the node is enabled for rendering
    /// Disabled nodes will not be drawn.
    /// This happens recursively
    /// </summary>
    /// <returns></returns>
    public bool RenderEnabled
    {
        get
        {
            if (Parent != null) return Parent.RenderEnabled && enabled;
            return enabled;
        }
    }

    /// <summary>
    /// Whether the node is enabled
    /// </summary>
    public bool enabled = true;

    /// <summary>
    /// Visual name of the node
    /// </summary>
    public string name = "Unnamed Node";


    /// <summary>
    /// The local transform of the node
    /// </summary>
    public Transform localTransform;

    /// <summary>
    /// The cached world space transform of the node
    /// </summary>
    public Transform globalTransform;

    public bool recalculateTransform = true;

    /// <summary>
    /// Constructs a new puppet root node
    /// </summary>
    /// <param name="puppet"></param>
    public Node(Puppet puppet)
    {
        _puppet = puppet;
    }

    /// <summary>
    /// Constructs a new node
    /// </summary>
    /// <param name="parent"></param>
    public Node(Node? parent = null) : this(NodeHelper.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new node with an UUID
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Node(uint uuid, Node? parent = null)
    {
        Parent = parent;
        UUID = uuid;
    }

    /// <summary>
    /// Send mask reset request one node up
    /// </summary>
    protected void ResetMask()
    {
        Parent?.ResetMask();
    }

    protected virtual void SerializeSelf(JObject obj, bool recursive)
    {
        obj.Add("uuid", UUID);
        obj.Add("name", name);
        obj.Add("type", TypeId());
        obj.Add("enabled", enabled);
        obj.Add("zsort", _zsort);

        var obj1 = new JObject();
        localTransform.Serialize(obj1);
        obj.Add("transform", obj1);
        obj.Add("lockToRoot", LockToRoot);

        if (recursive && Children.Count > 0)
        {
            var list = new JArray();
            foreach (var child in Children)
            {
                // Skip Temporary nodes
                if (child is TmpNode) continue;
                // Serialize permanent nodes
                var obj2 = new JObject();
                child.Serialize(obj2);
                list.Add(obj2);
            }
            obj.Add("children", list);
        }
    }

    protected unsafe void PreProcess()
    {
        if (preProcessed)
            return;
        preProcessed = true;
        if (preProcessFilter != null)
        {
            overrideTransformMatrix = null;
            var matrix = Parent != null ? Parent.Transform().Matrix : Matrix4x4.Identity;
            var temp = localTransform.Translation;
            var temp1 = offsetTransform.Translation;
            var filterResult = preProcessFilter([new(temp.X, temp.Y)], [new(temp1.X, temp1.Y)], new(&matrix));
            if (filterResult.Item1.Length > 0)
            {
                offsetTransform.Translation = new(filterResult.Item1[0], offsetTransform.Translation.Z);
                transformChanged();
            }
        }
    }

    protected unsafe void PostProcess()
    {
        if (postProcessed)
            return;
        postProcessed = true;
        if (postProcessFilter != null)
        {
            overrideTransformMatrix = null;
            var matrix = Parent != null ? Parent.Transform().Matrix : Matrix4x4.Identity;
            var temp = localTransform.Translation;
            var temp1 = offsetTransform.Translation;
            var filterResult = postProcessFilter([new(temp.X, temp.Y)], [new(temp1.X, temp1.Y)], new(&matrix));
            if (filterResult.Item1.Length > 0)
            {
                offsetTransform.Translation = new(filterResult.Item1[0], offsetTransform.Translation.Z);
                transformChanged();
                overrideTransformMatrix = new MatrixHolder(Transform().Matrix);
            }
        }
    }

    /// <summary>
    /// This node's type ID
    /// </summary>
    /// <returns></returns>
    public virtual string TypeId() { return "Node"; }

    /// <summary>
    /// The transform in world space
    /// </summary>
    /// <param name="ignoreParam"></param>
    /// <returns></returns>
    public Transform Transform(bool ignoreParam = false)
    {
        if (recalculateTransform)
        {
            localTransform.Update();
            offsetTransform.Update();

            if (!ignoreParam)
            {
                if (LockToRoot)
                    globalTransform = localTransform.CalcOffset(offsetTransform) * Puppet.root.localTransform;
                else if (Parent != null)
                    globalTransform = localTransform.CalcOffset(offsetTransform) * Parent.Transform();
                else
                    globalTransform = localTransform.CalcOffset(offsetTransform);

                recalculateTransform = false;
            }
            else
            {

                if (LockToRoot)
                    globalTransform = localTransform * Puppet.root.localTransform;
                else if (Parent != null)
                    globalTransform = localTransform * Parent.Transform();
                else
                    globalTransform = localTransform;

                recalculateTransform = false;
            }
        }

        return globalTransform;
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform transformLocal()
    {
        localTransform.Update();

        return localTransform.CalcOffset(offsetTransform);
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform transformNoLock()
    {
        localTransform.Update();

        if (Parent != null) return localTransform * Parent.Transform();
        return localTransform;
    }

    /// <summary>
    /// Calculates the relative position between 2 nodes and applies the offset.
    /// You should call this before reparenting nodes.
    /// </summary>
    /// <param name="to"></param>
    public void setRelativeTo(Node to)
    {
        setRelativeTo(to.transformNoLock().Matrix);
        _zsort = ZSortNoOffset - to.ZSortNoOffset;
    }

    /// <summary>
    /// Calculates the relative position between this node and a matrix and applies the offset.
    /// This does not handle zSorting. Pass a Node for that.
    /// </summary>
    /// <param name="to"></param>
    public void setRelativeTo(Matrix4x4 to)
    {
        localTransform.Translation = getRelativePosition(to, transformNoLock().Matrix);
        localTransform.Update();
    }

    /// <summary>
    /// Gets a relative position for 2 matrices
    /// </summary>
    /// <param name="m1"></param>
    /// <param name="m2"></param>
    /// <returns></returns>
    public static Vector3 getRelativePosition(Matrix4x4 m1, Matrix4x4 m2)
    {
        // Calculate the inverse of the first matrix
        Matrix4x4.Invert(m1, out var m1Inverse);

        // Multiply the inverse of m1 with m2
        var cm = m1Inverse * m2;
        return new Vector3(cm[0, 3], cm[1, 3], cm[2, 3]);
    }

    /// <summary>
    /// Gets a relative position for 2 matrices
    /// Inverse order of getRelativePosition
    /// </summary>
    /// <param name="m1"></param>
    /// <param name="m2"></param>
    /// <returns></returns>
    public static Vector3 getRelativePositionInv(Matrix4x4 m1, Matrix4x4 m2)
    {
        // Calculate the inverse of the first matrix
        Matrix4x4.Invert(m1, out var m1Inverse);

        // Multiply the inverse of m1 with m2
        var cm = m2 * m1Inverse;
        return new Vector3(cm[0, 3], cm[1, 3], cm[2, 3]);
    }

    /// <summary>
    /// Gets the path to the node.
    /// </summary>
    /// <returns></returns>
    public string getNodePath()
    {
        if (NodePath.Length > 0) return NodePath;

        var pathSegments = new StringBuilder();
        pathSegments.Append('/');
        Node? parent = this;
        while (parent != null)
        {
            pathSegments.Append(parent.name).Append('/');
            parent = parent.Parent;
        }

        NodePath = pathSegments.ToString();
        return NodePath;
    }

    /// <summary>
    /// Gets the depth of this node
    /// </summary>
    /// <returns></returns>
    public int depth()
    {
        int depthV = 0;
        Node? parent = this;
        while (parent != null)
        {
            depthV++;
            parent = parent.Parent;
        }
        return depthV;
    }

    /// <summary>
    /// Removes all children from this node
    /// </summary>
    public void clearChildren()
    {
        foreach (var child in Children) {
            child.Parent = null;
        }
        this.Children = [];
    }

    /// <summary>
    ///  Adds a node as a child of this node.
    /// </summary>
    /// <param name="child"></param>
    public void addChild(Node child)
    {
        child.Parent = this;
    }


    /// <summary>
    /// Sets the parent of this node
    /// </summary>
    /// <param name="node"></param>
    public void parent(Node node)
    {
        insertInto(node, OFFSET_END);
    }

    public int getIndexInParent()
    {
        return Parent!.Children.IndexOf(this);
    }

    public int getIndexInNode(Node n)
    {
        return n.Children.IndexOf(this);
    }

    public int OFFSET_START = int.MinValue;
    public int OFFSET_END = int.MaxValue;

    public void insertInto(Node node, int offset)
    {
        NodePath = "";
        // Remove ourselves from our current parent if we are
        // the child of one already.
        if (Parent != null)
        {
            // Try to find ourselves in our parent
            // note idx will be -1 if we can't be found
            var idx = Parent.Children.IndexOf(this);
            if (idx >= 0)
            {
                throw new Exception("Invalid parent-child relationship!");
            }

            // Remove ourselves
            Parent.Children.RemoveAt(idx);
        }

        // If we want to become parentless we need to handle that
        // seperately, as null parents have no children to update
        if (node is null)
        {
            Parent = null;
            return;
        }

        // Update our relationship with our new parent
        Parent = node;

        // Update position
        if (offset == OFFSET_START)
        {
            Parent.Children.Insert(0, this);
        }
        else if (offset == OFFSET_END || offset >= Parent.Children.Count
            )
        {
            Parent.Children.Add(this);
        }
        else
        {
            Parent.Children.Insert(offset, this);
        }
        Puppet?.rescanNodes();
    }

    /// <summary>
    /// Return whether this node supports a parameter
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public bool hasParam(string key)
    {
        switch (key)
        {
            case "zSort":
            case "transform.t.x":
            case "transform.t.y":
            case "transform.t.z":
            case "transform.r.x":
            case "transform.r.y":
            case "transform.r.z":
            case "transform.s.x":
            case "transform.s.y":
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the default offset value
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    float getDefaultValue(string key)
    {
        switch (key)
        {
            case "zSort":
            case "transform.t.x":
            case "transform.t.y":
            case "transform.t.z":
            case "transform.r.x":
            case "transform.r.y":
            case "transform.r.z":
                return 0;
            case "transform.s.x":
            case "transform.s.y":
                return 1;
            default:
                return 0;
        }
    }

    /// <summary>
    /// Sets offset value
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    bool setValue(string key, float value)
    {
        switch (key)
        {
            case "zSort":
                offsetSort += value;
                return true;
            case "transform.t.x":
                offsetTransform.Translation.X += value;
                transformChanged();
                return true;
            case "transform.t.y":
                offsetTransform.Translation.Y += value;
                transformChanged();
                return true;
            case "transform.t.z":
                offsetTransform.Translation.Z += value;
                transformChanged();
                return true;
            case "transform.r.x":
                offsetTransform.Rotation.X += value;
                transformChanged();
                return true;
            case "transform.r.y":
                offsetTransform.Rotation.Y += value;
                transformChanged();
                return true;
            case "transform.r.z":
                offsetTransform.Rotation.Z += value;
                transformChanged();
                return true;
            case "transform.s.x":
                offsetTransform.Scale.X *= value;
                transformChanged();
                return true;
            case "transform.s.y":
                offsetTransform.Scale.Y *= value;
                transformChanged();
                return true;
            default: return false;
        }
    }

    /// <summary>
    /// Scale an offset value, given an axis and a scale
    /// 
    /// If axis is -1, apply magnitude and sign to signed properties.
    /// If axis is 0 or 1, apply magnitude only unless the property is
    /// signed and aligned with that axis.
    /// 
    /// Note that scale adjustments are not considered aligned,
    /// since we consider preserving aspect ratio to be the user
    /// intent by default.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <param name="axis"></param>
    /// <param name="scale"></param>
    /// <returns></returns>
    public float scaleValue(string key, float value, int axis, float scale)
    {
        if (axis == -1) return value * scale;

        float newVal = MathF.Abs(scale) * value;
        switch (key)
        {
            case "transform.r.z": // Z-rotation is XY-mirroring
                newVal = scale * value;
                break;
            case "transform.r.y": // Y-rotation is X-mirroring
            case "transform.t.x":
                if (axis == 0) newVal = scale * value;
                break;
            case "transform.r.x": // X-rotation is Y-mirroring
            case "transform.t.y":
                if (axis == 1) newVal = scale * value;
                break;
            default:
                break;
        }
        return newVal;
    }

    public float getValue(string key)
    {
        return key switch
        {
            "zSort" => offsetSort,
            "transform.t.x" => offsetTransform.Translation.X,
            "transform.t.y" => offsetTransform.Translation.Y,
            "transform.t.z" => offsetTransform.Translation.Z,
            "transform.r.x" => offsetTransform.Rotation.X,
            "transform.r.y" => offsetTransform.Rotation.Y,
            "transform.r.z" => offsetTransform.Rotation.Z,
            "transform.s.x" => offsetTransform.Scale.X,
            "transform.s.y" => offsetTransform.Scale.Y,
            _ => 0,
        };
    }

    /// <summary>
    /// Draws this node and it's subnodes
    /// </summary>
    public void draw()
    {
        if (!RenderEnabled) return;

        foreach (var child in Children) 
        {
            child.draw();
        }
    }

    /// <summary>
    /// Draws this node.
    /// </summary>
    public virtual void drawOne() { }

    public void reconstruct()
    {
        foreach (var child in Children.ToArray())
        {
            child.reconstruct();
        }
    }

    /// <summary>
    /// Finalizes this node and any children
    /// </summary>
    public void finalize()
    {
        foreach (var child in Children)
        {
            child.finalize();
        }
    }

    public virtual void beginUpdate()
    {
        preProcessed = false;
        postProcessed = false;

        offsetSort = 0;
        offsetTransform.Clear();

        // Iterate through children
        foreach (var child in Children)
        {
            child.beginUpdate();
        }
    }

    /// <summary>
    /// Updates the node
    /// </summary>
    public virtual void update()
    {
        PreProcess();

        if (!enabled) return;

        foreach (var child in Children)
        {
            child.update();
        }
        PostProcess();
    }

    /// <summary>
    /// Marks this node's transform (and its descendents') as dirty
    /// </summary>
    public void transformChanged()
    {
        recalculateTransform = true;

        foreach (var child in Children)
        {
            child.transformChanged();
        }
    }


    public override string ToString()
    {
        return name;
    }

    /// <summary>
    /// Allows serializing a node (with pretty serializer)
    /// </summary>
    /// <param name=""></param>
    public void Serialize(JObject serializer)
    {
        SerializeSelf(serializer);
    }

    /// <summary>
    ///  Deserializes node from Fghj formatted JSON data.
    /// </summary>
    /// <param name="data"></param>
    protected virtual void Deserialize(JObject data)
    {
        var temp = data["uuid"];
        if (temp == null)
        {
            return;
        }
        UUID = (uint)temp;
        temp = data["name"];
        if (temp != null && temp.HasValues)
        {
            name = temp.ToString();
        }
        temp = data["enabled"];
        if (temp == null)
        {
            return;
        }
        enabled = (bool)temp;

        temp = data["zsort"];
        if (temp == null)
        {
            return;
        }
        _zsort = (float)temp;

        temp = data["transform"];
        if (temp is not JObject obj)
        {
            return;
        }
        localTransform = new();
        localTransform.Deserialize(obj);

        temp = data["lockToRoot"];
        if (temp == null)
        {
            return;
        }
        LockToRoot = (bool)temp;

        temp = data["children"];
        if (temp is not JArray array)
        {
            return;
        }

        // Pre-populate our children with the correct types
        foreach (var child in array) {
            if (child is not JObject obj1)
            {
                continue;
            }
            // Fetch type from json
            string type = child["type"]!.ToString();

            // Skips unknown node types
            // TODO: A logging system that shows a warning for this?
            if (!NodeHelper.HasNodeType(type)) continue;

            // instantiate it
            var n = NodeHelper.InstantiateNode(type, this);
            n.Deserialize(obj1);
        }
    }

    /// <summary>
    /// Force sets the node's ID
    /// THIS IS NOT A SAFE OPERATION.
    /// </summary>
    /// <param name="uuid"></param>
    public void forceSetUUID(uint uuid)
    {
        UUID = uuid;
    }

    public Rect getCombinedBoundsRect(bool reupdate = false, bool countPuppet = false)
    {
        var combinedBounds = getCombinedBounds(reupdate, countPuppet);
        return new(
            combinedBounds.X,
            combinedBounds.Y,
            combinedBounds.Z - combinedBounds.X,
            combinedBounds.W - combinedBounds.Y
        );
    }

    public Vector4 getInitialBoundsSize()
    {
        var tr = Transform();
        return new(tr.Translation.X, tr.Translation.Y, tr.Translation.X, tr.Translation.Y);
    }

    /// <summary>
    /// Gets the combined bounds of the node
    /// </summary>
    /// <param name="reupdate"></param>
    /// <param name="countPuppet"></param>
    /// <returns></returns>
    public Vector4 getCombinedBounds(bool reupdate = false, bool countPuppet = false)
    {
        var combined = getInitialBoundsSize();

        // Get Bounds as drawable
        if (this is Drawable drawable)
        {
            if (reupdate) drawable.updateBounds();
            combined = drawable.bounds;
        }

        foreach (var child in Children) {
            var cbounds = child.getCombinedBounds(reupdate);
            if (cbounds.X < combined.X) combined.X = cbounds.X;
            if (cbounds.Y < combined.Y) combined.Y = cbounds.Y;
            if (cbounds.Z > combined.Z) combined.Z = cbounds.Z;
            if (cbounds.W > combined.W) combined.W = cbounds.W;
        }

        if (countPuppet)
        {
            var temp = Puppet.transform.matrix * new Vector4(combined.X, combined.Y, 0, 1);
            var temp1 = Puppet.transform.matrix * new Vector4(combined.Z, combined.W, 0, 1);
            return new(temp.X, temp.Y, temp1.X, temp1.Y);
        }
        else
        {
            return combined;
        }
    }

    /// <summary>
    /// Gets whether nodes can be reparented
    /// </summary>
    /// <param name="to"></param>
    /// <returns></returns>
    public bool canReparent(Node to)
    {
        Node? tmp = to;
        while (tmp != null)
        {
            if (tmp.UUID == UUID) return false;

            // Check next up
            tmp = tmp.Parent;
        }
        return true;
    }

    /// <summary>
    /// Draws orientation of the node
    /// </summary>
    public void drawOrientation()
    {
        var trans = Transform().Matrix;
        inDbgLineWidth(4);

        // X
        inDbgSetBuffer([vec3(0, 0, 0), vec3(32, 0, 0)], [0, 1]);
        inDbgDrawLines(vec4(1, 0, 0, 0.7), trans);

        // Y
        inDbgSetBuffer([vec3(0, 0, 0), vec3(0, -32, 0)], [0, 1]);
        inDbgDrawLines(vec4(0, 1, 0, 0.7), trans);

        // Z
        inDbgSetBuffer([vec3(0, 0, 0), vec3(0, 0, -32)], [0, 1]);
        inDbgDrawLines(vec4(0, 0, 1, 0.7), trans);

        inDbgLineWidth(1);
    }

    /// <summary>
    /// Draws bounds
    /// </summary>
    public void drawBounds()
    {
        vec4 bounds = this.getCombinedBounds;

        float width = bounds.z - bounds.x;
        float height = bounds.w - bounds.y;
        inDbgSetBuffer([
            vec3(bounds.x, bounds.y, 0),
            vec3(bounds.x + width, bounds.y, 0),

            vec3(bounds.x + width, bounds.y, 0),
            vec3(bounds.x + width, bounds.y+height, 0),

            vec3(bounds.x + width, bounds.y+height, 0),
            vec3(bounds.x, bounds.y+height, 0),

            vec3(bounds.x, bounds.y+height, 0),
            vec3(bounds.x, bounds.y, 0),
        ]);
        inDbgLineWidth(3);
        if (oneTimeTransform! is null)
            inDbgDrawLines(vec4(.5, .5, .5, 1), (*oneTimeTransform));
        else
            inDbgDrawLines(vec4(.5, .5, .5, 1));
        inDbgLineWidth(1);
    }

    public void setOneTimeTransform(mat4* transform)
    {
        oneTimeTransform = transform;

        foreach (c; children) {
            c.setOneTimeTransform(transform);
        }
    }

    public unsafe mat4* getOneTimeTransform()
    {
        return oneTimeTransform;
    }

    /// <summary>
    /// set new Parent
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pOffset"></param>
    public void reparent(Node parent, ulong pOffset)
    {
        void unsetGroup(Node node)
        {
            node.postProcessFilter = null;
            node.preProcessFilter = null;
            auto group = cast(MeshGroup)node;
            if (group is null)
            {
                foreach (child; node.children) {
                    unsetGroup(child);
                }
            }
        }

        unsetGroup(this);

        if (parent! is null)
            setRelativeTo(parent);
        insertInto(parent, pOffset);
        auto c = this;
        for (auto p = parent; p! is null; p = p.parent, c = c.parent)
        {
            p.setupChild(c);
        }
    }

    public void setupChild(Node child) { }

    public mat4 getDynamicMatrix()
    {
        if (overrideTransformMatrix! is null)
        {
            return overrideTransformMatrix.Matrix;
        }
        else
        {
            return transform.matrix;
        }
    }
}
