using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Nodes.MeshGroups;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Nodes;

[TypeId("Node")]
public class Node : IDisposable
{
    public Puppet Puppet
    {
        get => _parent != null ? _parent.Puppet : _puppet;
        set => _puppet = value;
    }

    private Puppet _puppet;

    /// <summary>
    /// The parent of this node
    /// </summary>
    public Node? Parent
    {
        get => _parent;
        set => InsertInto(value, OFFSET_END);
    }

    private Node? _parent;

    /// <summary>
    /// A list of this node's children
    /// </summary>
    public List<Node> Children = [];
    /// <summary>
    /// Returns the unique identifier for this node
    /// </summary>
    public uint UUID;

    /// <summary>
    /// The relative Z sorting
    /// </summary>
    public float RelZSort
    {
        get => _zsort;
    }

    /// <summary>
    /// The basis zSort offset.
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
        get => ZSortBase + RelZSort + OffsetSort;
        set => _zsort = value;
    }

    /// <summary>
    /// The Z sorting without parameter offsets
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
                LocalTransform.Translation = TransformNoLock().Translation;
            }
            else if (!value && _lockToRoot)
            {
                LocalTransform.Translation -= Parent!.TransformNoLock().Translation;
            }

            _lockToRoot = value;

        }
    }

    private bool _lockToRoot;

    public string NodePath;

    protected bool PreProcessed = false;
    protected bool PostProcessed = false;

    /// <summary>
    /// The offset to the transform to apply
    /// </summary>
    public Transform OffsetTransform = new();

    /// <summary>
    /// The offset to apply to sorting
    /// </summary>
    protected float OffsetSort = 0f;

    public Matrix4x4? OneTimeTransform;

    public MatrixHolder? OverrideTransformMatrix = null;

    public delegate (Vector2[]?, Matrix4x4?) ProcessFilter(List<Vector2> a, Vector2[] b, ref Matrix4x4 c);

    //Matrix4x4*
    public ProcessFilter? PreProcessFilter = null;
    //Matrix4x4*
    public ProcessFilter? PostProcessFilter = null;

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
    public string Name = "Unnamed Node";

    /// <summary>
    /// The local transform of the node
    /// </summary>
    public Transform LocalTransform = new();

    /// <summary>
    /// The cached world space transform of the node
    /// </summary>
    public Transform GlobalTransform = new();

    public bool RecalculateTransform = true;

    protected readonly I2dCore _core;

    /// <summary>
    /// Constructs a new puppet root node
    /// </summary>
    /// <param name="puppet"></param>
    public Node(I2dCore core, Puppet puppet)
    {
        _core = core;
        _puppet = puppet;
    }

    /// <summary>
    /// Constructs a new node
    /// </summary>
    /// <param name="parent"></param>
    public Node(I2dCore core, Node? parent = null) : this(core, core.InCreateUUID(), parent)
    {
        _core = core;
    }

    /// <summary>
    /// Constructs a new node with an UUID
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public Node(I2dCore core, uint uuid, Node? parent = null)
    {
        _core = core;
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

    protected virtual void SerializeSelfImpl(JsonObject obj, bool recursive = true)
    {
        obj.Add("uuid", UUID);
        obj.Add("name", Name);
        obj.Add("type", TypeId());
        obj.Add("enabled", enabled);
        obj.Add("zsort", _zsort);

        var obj1 = new JsonObject();
        LocalTransform.Serialize(obj1);
        obj.Add("transform", obj1);
        obj.Add("lockToRoot", LockToRoot);

        if (recursive && Children.Count > 0)
        {
            var list = new JsonArray();
            foreach (var child in Children)
            {
                // Skip Temporary nodes
                if (child is TmpNode) continue;
                // Serialize permanent nodes
                var obj2 = new JsonObject();
                child.SerializeSelf(obj2);
                list.Add(obj2);
            }
            obj.Add("children", list);
        }
    }

    protected virtual void SerializeSelf(JsonObject obj)
    {
        SerializeSelfImpl(obj, true);
    }

    public unsafe virtual void PreProcess()
    {
        if (PreProcessed)
            return;
        PreProcessed = true;
        if (PreProcessFilter != null)
        {
            OverrideTransformMatrix = null;
            var matrix = Parent != null ? Parent.Transform().Matrix : Matrix4x4.Identity;
            var temp = LocalTransform.Translation;
            var temp1 = OffsetTransform.Translation;
            var filterResult = PreProcessFilter([new(temp.X, temp.Y)], [new(temp1.X, temp1.Y)], ref matrix);
            if (filterResult.Item1?.Length > 0)
            {
                OffsetTransform.Translation = new(filterResult.Item1[0], OffsetTransform.Translation.Z);
                TransformChanged();
            }
        }
    }

    public virtual void PostProcess()
    {
        if (PostProcessed)
            return;
        PostProcessed = true;
        if (PostProcessFilter != null)
        {
            OverrideTransformMatrix = null;
            var matrix = Parent != null ? Parent.Transform().Matrix : Matrix4x4.Identity;
            var temp = LocalTransform.Translation;
            var temp1 = OffsetTransform.Translation;
            var filterResult = PostProcessFilter([new(temp.X, temp.Y)], [new(temp1.X, temp1.Y)], ref matrix);
            if (filterResult.Item1?.Length > 0)
            {
                OffsetTransform.Translation = new(filterResult.Item1[0], OffsetTransform.Translation.Z);
                TransformChanged();
                OverrideTransformMatrix = new MatrixHolder(Transform().Matrix);
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
        if (RecalculateTransform)
        {
            LocalTransform.Update();
            OffsetTransform.Update();

            if (!ignoreParam)
            {
                if (LockToRoot)
                    GlobalTransform = LocalTransform.CalcOffset(OffsetTransform) * Puppet.Root.LocalTransform;
                else if (Parent != null)
                    GlobalTransform = LocalTransform.CalcOffset(OffsetTransform) * Parent.Transform();
                else
                    GlobalTransform = LocalTransform.CalcOffset(OffsetTransform);

                RecalculateTransform = false;
            }
            else
            {

                if (LockToRoot)
                    GlobalTransform = LocalTransform * Puppet.Root.LocalTransform;
                else if (Parent != null)
                    GlobalTransform = LocalTransform * Parent.Transform();
                else
                    GlobalTransform = LocalTransform;

                RecalculateTransform = false;
            }
        }

        return GlobalTransform;
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform TransformLocal()
    {
        LocalTransform.Update();

        return LocalTransform.CalcOffset(OffsetTransform);
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform TransformNoLock()
    {
        LocalTransform.Update();

        if (Parent != null) return LocalTransform * Parent.Transform();
        return LocalTransform;
    }

    /// <summary>
    /// Calculates the relative position between 2 nodes and applies the offset.
    /// You should call this before reparenting nodes.
    /// </summary>
    /// <param name="to"></param>
    public void SetRelativeTo(Node to)
    {
        SetRelativeTo(to.TransformNoLock().Matrix);
        _zsort = ZSortNoOffset - to.ZSortNoOffset;
    }

    /// <summary>
    /// Calculates the relative position between this node and a matrix and applies the offset.
    /// This does not handle zSorting. Pass a Node for that.
    /// </summary>
    /// <param name="to"></param>
    public void SetRelativeTo(Matrix4x4 to)
    {
        LocalTransform.Translation = GetRelativePosition(to, TransformNoLock().Matrix);
        LocalTransform.Update();
    }

    /// <summary>
    /// Gets a relative position for 2 matrices
    /// </summary>
    /// <param name="m1"></param>
    /// <param name="m2"></param>
    /// <returns></returns>
    public static Vector3 GetRelativePosition(Matrix4x4 m1, Matrix4x4 m2)
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
    public static Vector3 GetRelativePositionInv(Matrix4x4 m1, Matrix4x4 m2)
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
    public string GetNodePath()
    {
        if (NodePath.Length > 0) return NodePath;

        var pathSegments = new StringBuilder();
        pathSegments.Append('/');
        Node? parent = this;
        while (parent != null)
        {
            pathSegments.Append(parent.Name).Append('/');
            parent = parent.Parent;
        }

        NodePath = pathSegments.ToString();
        return NodePath;
    }

    /// <summary>
    /// Gets the depth of this node
    /// </summary>
    /// <returns></returns>
    public int Depth()
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
    public void ClearChildren()
    {
        foreach (var child in Children)
        {
            child._parent = null;
        }
        Children = [];
    }

    /// <summary>
    ///  Adds a node as a child of this node.
    /// </summary>
    /// <param name="child"></param>
    public void AddChild(Node child)
    {
        child.Parent = this;
    }

    public int GetIndexInParent()
    {
        return _parent!.Children.IndexOf(this);
    }

    public int GetIndexInNode(Node n)
    {
        return n.Children.IndexOf(this);
    }

    public int OFFSET_START = int.MinValue;
    public int OFFSET_END = int.MaxValue;

    public void InsertInto(Node? node, int offset)
    {
        NodePath = "";
        // Remove ourselves from our current parent if we are
        // the child of one already.
        if (_parent != null)
        {
            // Try to find ourselves in our parent
            // note idx will be -1 if we can't be found
            var idx = _parent.Children.IndexOf(this);
            if (idx < 0)
            {
                throw new Exception("Invalid parent-child relationship!");
            }

            // Remove ourselves
            _parent.Children.RemoveAt(idx);
        }

        // If we want to become parentless we need to handle that
        // seperately, as null parents have no children to update
        if (node is null)
        {
            _parent = null;
            return;
        }

        // Update our relationship with our new parent
        _parent = node;

        // Update position
        if (offset == OFFSET_START)
        {
            _parent.Children.Insert(0, this);
        }
        else if (offset == OFFSET_END || offset >= _parent.Children.Count
            )
        {
            _parent.Children.Add(this);
        }
        else
        {
            _parent.Children.Insert(offset, this);
        }
        Puppet?.RescanNodes();
    }

    /// <summary>
    /// Return whether this node supports a parameter
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual bool HasParam(string key)
    {
        return key switch
        {
            "zSort" or "transform.t.x" or "transform.t.y" or "transform.t.z" or "transform.r.x" or "transform.r.y" or "transform.r.z" or "transform.s.x" or "transform.s.y" => true,
            _ => false,
        };
    }

    /// <summary>
    /// Gets the default offset value
    /// </summary>
    /// <param name="key"></param>
    /// <returns></returns>
    public virtual float GetDefaultValue(string key)
    {
        return key switch
        {
            "zSort" or "transform.t.x" or "transform.t.y" or "transform.t.z" or "transform.r.x" or "transform.r.y" or "transform.r.z" => 0,
            "transform.s.x" or "transform.s.y" => 1,
            _ => (float)0,
        };
    }

    /// <summary>
    /// Sets offset value
    /// </summary>
    /// <param name="key"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public virtual bool SetValue(string key, float value)
    {
        switch (key)
        {
            case "zSort":
                OffsetSort += value;
                return true;
            case "transform.t.x":
                OffsetTransform.Translation.X += value;
                TransformChanged();
                return true;
            case "transform.t.y":
                OffsetTransform.Translation.Y += value;
                TransformChanged();
                return true;
            case "transform.t.z":
                OffsetTransform.Translation.Z += value;
                TransformChanged();
                return true;
            case "transform.r.x":
                OffsetTransform.Rotation.X += value;
                TransformChanged();
                return true;
            case "transform.r.y":
                OffsetTransform.Rotation.Y += value;
                TransformChanged();
                return true;
            case "transform.r.z":
                OffsetTransform.Rotation.Z += value;
                TransformChanged();
                return true;
            case "transform.s.x":
                OffsetTransform.Scale.X *= value;
                TransformChanged();
                return true;
            case "transform.s.y":
                OffsetTransform.Scale.Y *= value;
                TransformChanged();
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
    public static float ScaleValue(string key, float value, int axis, float scale)
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

    public virtual float GetValue(string key)
    {
        return key switch
        {
            "zSort" => OffsetSort,
            "transform.t.x" => OffsetTransform.Translation.X,
            "transform.t.y" => OffsetTransform.Translation.Y,
            "transform.t.z" => OffsetTransform.Translation.Z,
            "transform.r.x" => OffsetTransform.Rotation.X,
            "transform.r.y" => OffsetTransform.Rotation.Y,
            "transform.r.z" => OffsetTransform.Rotation.Z,
            "transform.s.x" => OffsetTransform.Scale.X,
            "transform.s.y" => OffsetTransform.Scale.Y,
            _ => 0,
        };
    }

    /// <summary>
    /// Draws this node and it's subnodes
    /// </summary>
    public virtual void Draw()
    {
        if (!RenderEnabled) return;

        foreach (var child in Children)
        {
            child.Draw();
        }
    }

    /// <summary>
    /// Draws this node.
    /// </summary>
    public virtual void DrawOne() { }

    public void Reconstruct()
    {
        foreach (var child in Children.ToArray())
        {
            child.Reconstruct();
        }
    }

    /// <summary>
    /// Finalizes this node and any children
    /// </summary>
    public virtual void JsonLoadDone()
    {
        foreach (var child in Children)
        {
            child.JsonLoadDone();
        }
    }

    public virtual void BeginUpdate()
    {
        PreProcessed = false;
        PostProcessed = false;

        OffsetSort = 0;
        OffsetTransform.Clear();

        // Iterate through children
        foreach (var child in Children)
        {
            child.BeginUpdate();
        }
    }

    /// <summary>
    /// Updates the node
    /// </summary>
    public virtual void Update()
    {
        PreProcess();

        if (!enabled) return;

        foreach (var child in Children)
        {
            child.Update();
        }
        PostProcess();
    }

    /// <summary>
    /// Marks this node's transform (and its descendents') as dirty
    /// </summary>
    public void TransformChanged()
    {
        RecalculateTransform = true;

        foreach (var child in Children)
        {
            child.TransformChanged();
        }
    }

    public override string ToString()
    {
        return Name;
    }

    /// <summary>
    /// Allows serializing a node (with pretty serializer)
    /// </summary>
    /// <param name=""></param>
    public virtual void SerializePartial(JsonObject obj, bool recursive = true)
    {
        SerializeSelfImpl(obj, recursive);
    }

    /// <summary>
    ///  Deserializes node from Fghj formatted JSON data.
    /// </summary>
    /// <param name="data"></param>
    public virtual void Deserialize(JsonElement data)
    {
        LocalTransform = new();
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "uuid" && item.Value.ValueKind != JsonValueKind.Null)
            {
                UUID = item.Value.GetUInt32();
            }
            else if (item.Name == "name" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Name = item.Value.GetString()!;
            }
            else if (item.Name == "enabled" && item.Value.ValueKind != JsonValueKind.Null)
            {
                enabled = item.Value.GetBoolean(); ;
            }
            else if (item.Name == "zsort" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _zsort = item.Value.GetSingle();
            }
            else if (item.Name == "transform" && item.Value.ValueKind == JsonValueKind.Object)
            {
                LocalTransform.Deserialize(item.Value);
            }
            else if (item.Name == "lockToRoot" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LockToRoot = item.Value.GetBoolean(); ;
            }
            else if (item.Name == "children" && item.Value.ValueKind == JsonValueKind.Array)
            {
                // Pre-populate our children with the correct types
                foreach (JsonElement child in item.Value.EnumerateArray())
                {
                    if (!child.TryGetProperty("type", out var type1) || type1.ValueKind == JsonValueKind.Null)
                    {
                        continue;
                    }
                    // Fetch type from json
                    var type = type1.GetString()!;

                    // Skips unknown node types
                    // TODO: A logging system that shows a warning for this?
                    if (!TypeList.HasNodeType(type)) continue;

                    // instantiate it
                    var n = TypeList.InstantiateNode(type, _core, this);
                    n.Deserialize(child);
                }
            }
        }
    }

    /// <summary>
    /// Force sets the node's ID
    /// THIS IS NOT A SAFE OPERATION.
    /// </summary>
    /// <param name="uuid"></param>
    public void ForceSetUUID(uint uuid)
    {
        UUID = uuid;
    }

    public Rect GetCombinedBoundsRect(bool reupdate = false, bool countPuppet = false)
    {
        var combinedBounds = GetCombinedBounds(reupdate, countPuppet);
        return new(
            combinedBounds.X,
            combinedBounds.Y,
            combinedBounds.Z - combinedBounds.X,
            combinedBounds.W - combinedBounds.Y
        );
    }

    public Vector4 GetInitialBoundsSize()
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
    public Vector4 GetCombinedBounds(bool reupdate = false, bool countPuppet = false)
    {
        var combined = GetInitialBoundsSize();

        // Get Bounds as drawable
        if (this is Drawable drawable)
        {
            if (reupdate) drawable.UpdateBounds();
            combined = drawable.Bounds;
        }

        foreach (var child in Children)
        {
            var cbounds = child.GetCombinedBounds(reupdate);
            if (cbounds.X < combined.X) combined.X = cbounds.X;
            if (cbounds.Y < combined.Y) combined.Y = cbounds.Y;
            if (cbounds.Z > combined.Z) combined.Z = cbounds.Z;
            if (cbounds.W > combined.W) combined.W = cbounds.W;
        }

        if (countPuppet)
        {
            var temp = Puppet.Transform.Matrix.Multiply(new Vector4(combined.X, combined.Y, 0, 1));
            var temp1 = Puppet.Transform.Matrix.Multiply(new Vector4(combined.Z, combined.W, 0, 1));
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
    public bool CanReparent(Node to)
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
    public void DrawOrientation()
    {
        var trans = Transform().Matrix;
        _core.InDbgLineWidth(4);

        // X
        _core.InDbgSetBuffer([new Vector3(0, 0, 0), new Vector3(32, 0, 0)], [0, 1]);
        _core.InDbgDrawLines(new Vector4(1, 0, 0, 0.7f), trans);

        // Y
        _core.InDbgSetBuffer([new Vector3(0, 0, 0), new Vector3(0, -32, 0)], [0, 1]);
        _core.InDbgDrawLines(new Vector4(0, 1, 0, 0.7f), trans);

        // Z
        _core.InDbgSetBuffer([new Vector3(0, 0, 0), new Vector3(0, 0, -32)], [0, 1]);
        _core.InDbgDrawLines(new Vector4(0, 0, 1, 0.7f), trans);

        _core.InDbgLineWidth(1);
    }

    /// <summary>
    /// Draws bounds
    /// </summary>
    public virtual void DrawBounds()
    {
        var bounds = GetCombinedBounds();

        float width = bounds.Z - bounds.X;
        float height = bounds.W - bounds.Y;
        _core.InDbgSetBuffer([
            new Vector3(bounds.X, bounds.Y, 0),
            new Vector3(bounds.X + width, bounds.Y, 0),

            new Vector3(bounds.X + width, bounds.Y, 0),
            new Vector3(bounds.X + width, bounds.Y+height, 0),

            new Vector3(bounds.X + width, bounds.Y+height, 0),
            new Vector3(bounds.X, bounds.Y+height, 0),

            new Vector3(bounds.X, bounds.Y+height, 0),
            new Vector3(bounds.X, bounds.Y, 0),
        ]);
        _core.InDbgLineWidth(3);
        if (OneTimeTransform != null)
            _core.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1), OneTimeTransform.Value);
        else
            _core.InDbgDrawLines(new Vector4(0.5f, 0.5f, 0.5f, 1));
        _core.InDbgLineWidth(1);
    }

    public virtual void SetOneTimeTransform(Matrix4x4 transform)
    {
        OneTimeTransform = transform;

        foreach (var c in Children)
        {
            c.SetOneTimeTransform(transform);
        }
    }

    public unsafe Matrix4x4? GetOneTimeTransform()
    {
        return OneTimeTransform;
    }

    /// <summary>
    /// set new Parent
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pOffset"></param>
    public void Reparent(Node parent, ulong pOffset)
    {
        static void UnsetGroup(Node node)
        {
            node.PostProcessFilter = null;
            node.PreProcessFilter = null;
            if (node is not MeshGroup)
            {
                foreach (var child in node.Children)
                {
                    UnsetGroup(child);
                }
            }
        }

        UnsetGroup(this);

        if (parent != null)
            SetRelativeTo(parent);
        InsertInto(parent, (int)pOffset);
        Node? c = this;
        for (var p = parent; p != null; p = p.Parent, c = c?.Parent)
        {
            p.SetupChild(c);
        }
    }

    public virtual void SetupChild(Node? child)
    {

    }

    public Matrix4x4 GetDynamicMatrix()
    {
        if (OverrideTransformMatrix != null)
        {
            return OverrideTransformMatrix.Matrix;
        }
        else
        {
            return Transform().Matrix;
        }
    }

    public virtual void Dispose()
    {

    }
}
