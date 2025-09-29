using System.Numerics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes.Drawables;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes;

/// <summary>
/// A node in the Inochi2D rendering tree
/// </summary>
[TypeId("Node", 0x00000000)]
public class Node : IDisposable
{
    public const int OFFSET_START = int.MinValue;
    public const int OFFSET_END = int.MaxValue;

    private Puppet _puppet;
    private Node? _parent;
    private Guid _guid;
    private float _zsort;
    private bool _lockToRoot;
    private string _nodePath;
    private uint _nid;

    /// <summary>
    /// The Node's numeric ID
    /// </summary>
    protected uint Nid => _nid;

    protected bool RecalculateTransform = true;
    protected bool PreProcessed = false;
    protected bool PostProcessed = false;

    /// <summary>
    /// The offset to the transform to apply
    /// </summary>
    protected Transform TransformOffset;

    /// <summary>
    /// The offset to apply to sorting
    /// </summary>
    protected float OffsetSort = 0f;

    /// <summary>
    /// The offset to apply to sorting
    /// </summary>
    protected float offsetSort = 0f;

    /// <summary>
    /// Whether the node is enabled
    /// </summary>
    public bool enabled = true;

    /// <summary>
    /// Visual name of the node
    /// </summary>
    public string Name = "Unnamed Node";

    /// <summary>
    /// The Node's Type ID
    /// </summary>
    public TypeIdAttribute TypeId => TypeList.GetTypeId(this);

    /// <summary>
    /// The node's GUID.
    /// </summary>
    public Guid Guid => _guid;

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
            if (_parent != null)
            {
                return _parent.RenderEnabled && enabled;
            }
            return enabled;
        }
    }

    /// <summary>
    /// The relative Z sorting
    /// </summary>
    public float RelZSort
    {
        get => _zsort;
        set => _zsort = value;
    }

    /// <summary>
    /// The basis zSort offset.
    /// </summary>
    public float ZSortBase
    {
        get => _parent != null ? _parent._zsort : 0;
    }

    /// <summary>
    /// The Z sorting without parameter offsets
    /// </summary>
    public float ZSortNoOffset
    {
        get => ZSortBase + RelZSort;
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
    /// The local transform of the node
    /// </summary>
    public Transform LocalTransform = new();

    /// <summary>
    /// The cached world space transform of the node
    /// </summary>
    public Transform GlobalTransform = new();

    /// <summary>
    /// A list of this node's children
    /// </summary>
    public readonly List<Node> Children = [];

    /// <summary>
    /// The parent of this node
    /// </summary>
    public Node? Parent
    {
        get => _parent;
        set => InsertInto(value, OFFSET_END);
    }

    public Puppet Puppet
    {
        get => _parent != null ? _parent.Puppet : _puppet;
        set => _puppet = value;
    }

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
                LocalTransform.Translation -= _parent?.TransformNoLock().Translation ?? new Vector3();
            }

            _lockToRoot = value;

        }
    }

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
    public Node(Node? parent = null) : this(Guid.NewGuid(), parent)
    {

    }

    /// <summary>
    /// Constructs a new node with an UUID
    /// </summary>
    /// <param name="guid"></param>
    /// <param name="parent"></param>
    public Node(Guid guid, Node? parent = null)
    {
        _parent = parent;
        _guid = guid;
    }

    /// <summary>
    /// The transform in world space
    /// </summary>
    /// <param name="ignoreParam"></param>
    /// <returns></returns>
    public Transform Transform(bool ignoreParam = false)
    {
        if (!ignoreParam)
        {
            if (RecalculateTransform)
            {
                LocalTransform.Update();
                TransformOffset.Update();

                if (_lockToRoot)
                    GlobalTransform = LocalTransform.CalcOffset(TransformOffset) * _puppet.Root.LocalTransform;
                else if (_parent != null)
                    GlobalTransform = LocalTransform.CalcOffset(TransformOffset) * _parent.Transform();
                else
                    GlobalTransform = LocalTransform.CalcOffset(TransformOffset);

                RecalculateTransform = false;
            }

            return GlobalTransform;
        }
        else
        {
            Transform mts;
            if (_lockToRoot)
                mts = LocalTransform * _puppet.Root.LocalTransform;
            else if (_parent != null)
                mts = LocalTransform * _parent.Transform();
            else
                mts = LocalTransform;

            return mts;
        }
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform TransformLocal()
    {
        LocalTransform.Update();

        return LocalTransform.CalcOffset(TransformOffset);
    }

    /// <summary>
    /// The transform in world space without locking
    /// </summary>
    /// <returns></returns>
    public Transform TransformNoLock()
    {
        LocalTransform.Update();

        if (_parent != null)
        {
            return LocalTransform * _parent.Transform();
        }
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
        if (_nodePath.Length > 0) return _nodePath;

        var pathSegments = new StringBuilder();
        pathSegments.Append('/');
        Node? parent = this;
        while (parent != null)
        {
            pathSegments.Append(parent.Name).Append('/');
            parent = parent.Parent;
        }

        _nodePath = pathSegments.ToString();
        return _nodePath;
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
        Children.Clear();
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

    public void InsertInto(Node? node, int offset)
    {
        _nodePath = "";
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
    /// Applies an offset to the Node's transform.
    /// </summary>
    /// <param name="other">The transform to offset the current global transform by.</param>
    public virtual void OffsetTransform(Transform other)
    {
        GlobalTransform = GlobalTransform.CalcOffset(other);
        GlobalTransform.Update();
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
            _ => 0f,
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
                TransformOffset.Translation.X += value;
                TransformChanged();
                return true;
            case "transform.t.y":
                TransformOffset.Translation.Y += value;
                TransformChanged();
                return true;
            case "transform.t.z":
                TransformOffset.Translation.Z += value;
                TransformChanged();
                return true;
            case "transform.r.x":
                TransformOffset.Rotation.X += value;
                TransformChanged();
                return true;
            case "transform.r.y":
                TransformOffset.Rotation.Y += value;
                TransformChanged();
                return true;
            case "transform.r.z":
                TransformOffset.Rotation.Z += value;
                TransformChanged();
                return true;
            case "transform.s.x":
                TransformOffset.Scale.X *= value;
                TransformChanged();
                return true;
            case "transform.s.y":
                TransformOffset.Scale.Y *= value;
                TransformChanged();
                return true;
            default: return false;
        }
    }

    /// <summary>
    /// Scale an offset value, given an axis and a scale
    /// <br/>
    /// If axis is -1, apply magnitude and sign to signed properties.
    /// If axis is 0 or 1, apply magnitude only unless the property is
    /// signed and aligned with that axis.
    /// <br/>
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
            "transform.t.x" => TransformOffset.Translation.X,
            "transform.t.y" => TransformOffset.Translation.Y,
            "transform.t.z" => TransformOffset.Translation.Z,
            "transform.r.x" => TransformOffset.Rotation.X,
            "transform.r.y" => TransformOffset.Rotation.Y,
            "transform.r.z" => TransformOffset.Rotation.Z,
            "transform.s.x" => TransformOffset.Scale.X,
            "transform.s.y" => TransformOffset.Scale.Y,
            _ => 0,
        };
    }

    /// <summary>
    /// Update sequence run before the main update sequence.
    /// </summary>
    /// <param name="drawList"></param>
    public virtual void PreUpdate(DrawList drawList)
    {
        TransformOffset.Clear();
        offsetSort = 0;

        if (!enabled) return;
        foreach (var child in Children)
        {
            child.PreUpdate(drawList);
        }
    }

    /// <summary>
    /// Updates the node
    /// </summary>
    public virtual void Update(float delta, DrawList drawList)
    {
        if (!enabled) return;

        foreach (var child in Children)
        {
            child.Update(delta, drawList);
        }
    }

    /// <summary>
    /// Update sequence run after the main update sequence.
    /// </summary>
    /// <param name="drawList"></param>
    public virtual void PostUpdate(DrawList drawList)
    {
        if (!enabled) return;
        foreach (var child in Children)
        {
            child.PostUpdate(drawList);
        }
    }

    /// <summary>
    /// Draws this node and it's subnodes
    /// </summary>
    public virtual void Draw(float delta, DrawList drawList)
    {

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

    public virtual void Serialize(JsonObject obj, bool recursive = true)
    {
        obj.Add("guid", _guid.ToString());
        obj.Add("name", Name);
        obj.Add("type", TypeId.Sid);
        obj.Add("enabled", enabled);
        obj.Add("zsort", _zsort);

        var obj1 = new JsonObject();
        LocalTransform.Serialize(obj1);
        obj.Add("transform", obj1);
        obj.Add("lockToRoot", LockToRoot);

        if (!recursive)
            return;

        var list = new JsonArray();
        foreach (var child in Children)
        {
            var obj2 = new JsonObject();
            child.Serialize(obj2);
            list.Add(obj2);
        }
        obj.Add("children", list);
    }

    /// <summary>
    ///  Deserializes node from Fghj formatted JSON data.
    /// </summary>
    /// <param name="data"></param>
    public virtual void Deserialize(JsonElement data)
    {
        _guid = data.GetGuid("uuid", "guid");
        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "name" && item.Value.ValueKind != JsonValueKind.Null)
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
                LocalTransform = new();
                LocalTransform.Deserialize(item.Value);
            }
            else if (item.Name == "lockToRoot" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LockToRoot = item.Value.GetBoolean();
            }
            else if (item.Name == "children" && item.Value.ValueKind == JsonValueKind.Array)
            {
                // Pre-populate our children with the correct types
                foreach (JsonElement child in item.Value.EnumerateArray())
                {
                    if (!child.TryGetProperty("type", out var type1) || type1.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }
                    // Fetch type from json
                    var type = type1.GetString()!;

                    // Skips unknown node types
                    // TODO: A logging system that shows a warning for this?
                    if (!TypeList.HasNodeType(type)) continue;

                    // instantiate it
                    var n = TypeList.InstantiateNode(type, this);
                    n.Deserialize(child);
                }
            }
        }
    }

    /// <summary>
    /// Reconstructs a child.
    /// </summary>
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
    public virtual void Finalized()
    {
        _nid = TypeId.Nid;
        foreach (var child in Children)
        {
            child.Finalized();
        }
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
            //if (reupdate) drawable.UpdateBounds();
            //combined = drawable.Bounds;
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
            if (tmp._guid == _guid) return false;

            // Check next up
            tmp = tmp.Parent;
        }
        return true;
    }

    /// <summary>
    /// set new Parent
    /// </summary>
    /// <param name="parent"></param>
    /// <param name="pOffset"></param>
    public void Reparent(Node parent, ulong pOffset)
    {
        if (parent != null)
            SetRelativeTo(parent);
        InsertInto(parent, (int)pOffset);
    }

    public virtual void Dispose()
    {

    }
}
