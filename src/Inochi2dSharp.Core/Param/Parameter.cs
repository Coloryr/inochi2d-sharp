using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Core.Param;

public class Parameter
{
    private readonly Combinator _iadd = new();
    private readonly Combinator _imul = new();

    /// <summary>
    /// Unique ID of parameter
    /// </summary>
    public Guid Guid;

    /// <summary>
    /// Name of the parameter
    /// </summary>
    public string Name;

    /// <summary>
    /// Optimized indexable name generated at runtime
    /// <br/>
    /// DO NOT SERIALIZE THIS.
    /// </summary>
    public string IndexableName;

    /// <summary>
    /// Whether this parameter updates the model
    /// </summary>
    public bool Active = true;

    /// <summary>
    /// The current parameter value
    /// </summary>
    public Vector2 Value = new(0);

    /// <summary>
    /// The previous internal value offset
    /// </summary>
    public Vector2 LastInternal = new(0);

    /// <summary>
    /// Parameter merge mode
    /// </summary>
    public ParamMergeMode MergeMode;

    /// <summary>
    /// The default value
    /// </summary>
    public Vector2 Defaults = new(0);

    /// <summary>
    /// Whether the parameter is 2D
    /// </summary>
    public bool IsVec2;

    /// <summary>
    /// The parameter's minimum bounds
    /// </summary>
    public Vector2 Min = new(0, 0);

    /// <summary>
    /// The parameter's maximum bounds
    /// </summary>
    public Vector2 Max = new(1, 1);

    /// <summary>
    /// Position of the keypoints along each axis
    /// </summary>
    public float[][] AxisPoints = [[0, 1], [0, 1]];

    /// <summary>
    /// Binding to targets
    /// </summary>
    public List<ParameterBinding> Bindings = [];

    /// <summary>
    /// Gets the value normalized to the internal range (0.0->1.0)
    /// </summary>
    /// <returns></returns>
    public Vector2 NormalizedValue
    {
        get
        {
            return MapValue(Value);
        }
        set
        {
            Value = new Vector2(
                value.X * (Max.X - Min.X) + Min.X,
                value.Y * (Max.Y - Min.Y) + Min.Y
            );
        }
    }

    public Parameter()
    {
        
    }

    public Parameter(string name, bool isVec2)
    {
        Guid = Guid.NewGuid();
        Name = name;
        IsVec2 = isVec2;
        if (!isVec2)
        {
            AxisPoints[1] = [0];
        }

        MakeIndexable();
    }

    /// <summary>
    /// Clone this parameter
    /// </summary>
    /// <returns></returns>
    public Parameter Dup()
    {
        var newParam = new Parameter(Name + " (Copy)", IsVec2)
        {
            Min = Min,
            Max = Max,
            AxisPoints = [.. AxisPoints]
        };

        foreach (var binding in Bindings)
        {
            var newBinding = newParam.CreateBinding(
                binding.GetNode(),
                binding.GetName(),
                false
            );
            newBinding.InterpolateMode = binding.InterpolateMode;
            for (uint x = 0; x < AxisPointCount(0); x++)
            {
                for (uint y = 0; y < AxisPointCount(1); y++)
                {
                    binding.CopyKeypointToBinding(new Vector2UInt(x, y), newBinding, new Vector2UInt(x, y));
                }
            }
            newParam.AddBinding(newBinding);
        }

        return newParam;
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JsonObject serializer)
    {
        serializer.Add("uuid", Guid);
        serializer.Add("name", Name);
        serializer.Add("is_vec2", IsVec2);
        serializer.Add("min", Min.ToToken());
        serializer.Add("max", Max.ToToken());
        serializer.Add("defaults", Defaults.ToToken());
        serializer.Add("axis_points", AxisPoints.ToToken());
        serializer.Add("merge_mode", MergeMode.ToString().ToLower());
        var list = new JsonArray();
        foreach (var item in Bindings)
        {
            var obj = new JsonObject();
            item.Serialize(obj);
            list.Add(item);
        }
        serializer.Add("bindings", list);
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JsonElement data)
    {
        Guid = data.GetGuid("uuid", "guid");

        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "name" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Name = item.Value.GetString()!;
            }
            else if (item.Name == "is_vec2" && item.Value.ValueKind != JsonValueKind.Null)
            {
                IsVec2 = item.Value.GetBoolean(); ;
            }
            else if (item.Name == "min" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Min = item.Value.ToVector2();
            }
            else if (item.Name == "max" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Max = item.Value.ToVector2();
            }
            else if (item.Name == "axis_points" && item.Value.ValueKind == JsonValueKind.Array)
            {
                AxisPoints = item.Value.ToArray<float>();
            }
            else if (item.Name == "defaults" && item.Value.ValueKind == JsonValueKind.Array)
            {
                Defaults = item.Value.ToVector2();
            }
            else if (item.Name == "merge_mode" && item.Value.ValueKind is JsonValueKind.String)
            {
                MergeMode = item.Value.GetString()!.ToMergeMode();
            }
            else if (item.Name == "bindings" && item.Value.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    // Skip empty children
                    if (item1.TryGetProperty("param_name", out var temp1) && temp1.ValueKind != JsonValueKind.Null)
                    {
                        var paramName = temp1.GetString();

                        if (paramName == "deform")
                        {
                            var binding = new DeformationParameterBinding(this);
                            binding.Deserialize(item1);
                            Bindings.Add(binding);
                        }
                        else
                        {
                            var binding = new ValueParameterBinding(this);
                            binding.Deserialize(item1);
                            Bindings.Add(binding);
                        }
                    }
                }
            }
        }
    }

    public void Reconstruct(Puppet puppet)
    {
        foreach (var binding in Bindings)
        {
            binding.Reconstruct(puppet);
        }
    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public void Finalized(Puppet puppet)
    {
        MakeIndexable();
        Value = Defaults;

        var validBindingList = new List<ParameterBinding>();
        foreach (var binding in Bindings)
        {
            if (puppet.Find<Node>(binding.GetNodeUUID()) != null)
            {
                binding.Finalize(puppet);
                validBindingList.Add(binding);
            }
        }

        Bindings = validBindingList;
    }

    public void FindOffset(Vector2 offset, out Vector2UInt index, out Vector2 outOffset)
    {
        index = new();
        outOffset = new();
        void InterpAxis(int axis, float val, out uint index, out float offset)
        {
            var pos = AxisPoints[axis];

            for (int i = 0; i < pos.Length - 1; i++)
            {
                if (pos[i + 1] > val || i == (pos.Length - 2))
                {
                    index = (uint)i;
                    offset = (val - pos[i]) / (pos[i + 1] - pos[i]);
                    return;
                }
            }

            index = 0;
            offset = 0;
        }

        InterpAxis(0, offset.X, out index.X, out outOffset.X);
        if (IsVec2) InterpAxis(1, offset.Y, out index.Y, out outOffset.Y);
    }

    public void Update()
    {
        if (!Active)
            return;

        LastInternal = (Value + _iadd.Csum()) * _imul.Avg();

        FindOffset(MapValue(LastInternal), out var index, out var offset_);
        foreach (var binding in Bindings)
        {
            binding.Apply(index, offset_);
        }

        // Reset combinatorics
        _iadd.Clear();
        _imul.Clear();
    }

    public void PushIOffset(Vector2 offset, ParamMergeMode mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = MergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                Value = offset;
                break;
            case ParamMergeMode.Additive:
                _iadd.Add(offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                _imul.Add(offset, 1);
                break;
            case ParamMergeMode.Weighted:
                _imul.Add(offset, weight);
                break;
            default: break;
        }
    }

    public void PushIOffsetAxis(int axis, float offset, ParamMergeMode mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = MergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                Value[axis] = offset;
                break;
            case ParamMergeMode.Additive:
                _iadd.Add(axis, offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                _imul.Add(axis, offset, 1);
                break;
            case ParamMergeMode.Weighted:
                _imul.Add(axis, offset, weight);
                break;
            default: break;
        }
    }

    /// <summary>
    /// Get number of points for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int AxisPointCount(uint axis = 0)
    {
        return AxisPoints[axis].Length;
    }

    /// <summary>
    /// Move an axis point to a new offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="oldidx"></param>
    /// <param name="newoff"></param>
    public void MoveAxisPoint(uint axis, uint oldidx, float newoff)
    {
        if (oldidx <= 0 && oldidx >= AxisPointCount(axis) - 1)
        {
            throw new Exception("invalid point index");
        }
        if (newoff <= 0 && newoff >= 1)
        {
            throw new Exception("offset out of bounds");
        }
        if (IsVec2)
        {
            if (axis > 1)
            {
                throw new Exception("bad axis");
            }
        }
        else
        {
            if (axis != 0)
            {
                throw new Exception("bad axis");
            }
        }

        // Find the index at which to insert
        uint index;
        for (index = 1; index < AxisPoints[axis].Length; index++)
        {
            if (AxisPoints[axis][index + 1] > newoff)
                break;
        }

        if (oldidx != index)
        {
            // BUG: Apparently deleting the oldindex and replacing it with newindex causes a crash.

            // Insert it into the new position in the list
            (AxisPoints[axis][oldidx], AxisPoints[axis][index]) = (AxisPoints[axis][index], AxisPoints[axis][oldidx]);
        }

        // Tell all bindings to reinterpolate
        foreach (var binding in Bindings)
        {
            binding.MoveKeypoints(axis, oldidx, index);
        }
    }

    /// <summary>
    /// Add a new axis point at the given offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="off"></param>
    public void InsertAxisPoint(uint axis, float off)
    {
        if (off <= 0 || off >= 1)
        {
            throw new Exception("offset out of bounds");
        }
        if (IsVec2)
        {
            if (axis > 1)
            {
                throw new Exception("bad axis");
            }
        }
        else
        {
            if (axis != 0)
            {
                throw new Exception("bad axis");
            }
        }

        // Find the index at which to insert
        uint index;
        for (index = 1; index < AxisPoints[axis].Length; index++)
        {
            if (AxisPoints[axis][index] > off)
                break;
        }

        // Insert it into the position list
        var list = new List<float>(AxisPoints[axis]);
        list.Insert((int)index, off);
        AxisPoints[axis] = [.. list];

        // Tell all bindings to insert space into their arrays
        foreach (var binding in Bindings)
        {
            binding.InsertKeypoints(axis, index);
        }
    }

    /// <summary>
    /// Delete a specified axis point by index
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public void DeleteAxisPoint(uint axis, uint index)
    {
        if (IsVec2)
        {
            if (axis > 1)
            {
                throw new Exception("bad axis");
            }
        }
        else
        {
            if (axis != 0)
            {
                throw new Exception("bad axis");
            }
        }

        if (index <= 0)
        {
            throw new Exception("cannot delete axis point at 0");
        }
        if (index >= (AxisPoints[axis].Length - 1))
        {
            throw new Exception("cannot delete axis point at 1");
        }

        // Remove the keypoint
        var list = new List<float>(AxisPoints[axis]);
        list.RemoveAt((int)index);
        AxisPoints[axis] = [.. list];

        // Tell all bindings to remove it from their arrays
        foreach (var binding in Bindings)
        {
            binding.DeleteKeypoints(axis, index);
        }
    }

    /// <summary>
    /// Flip the mapping across an axis
    /// </summary>
    /// <param name="axis"></param>
    public void ReverseAxis(uint axis)
    {
        Array.Reverse(AxisPoints[axis]);
        for (var i = 0; i < AxisPoints[axis].Length; i++)
        {
            AxisPoints[axis][i] = 1 - AxisPoints[axis][i];
        }
        foreach (var binding in Bindings)
        {
            binding.ReverseAxis(axis);
        }
    }

    /// <summary>
    /// Get the offset (0..1) of a specified keypoint index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector2 GetKeypointOffset(Vector2UInt index)
    {
        return new(AxisPoints[0][index.X], AxisPoints[1][index.Y]);
    }

    /// <summary>
    /// Get the value at a specified keypoint index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector2 GetKeypointValue(Vector2UInt index)
    {
        return UnmapValue(GetKeypointOffset(index));
    }

    /// <summary>
    /// Maps an input value to an offset (0.0->1.0)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2 MapValue(Vector2 value)
    {
        var range = Max - Min;
        var tmp = value - Min;
        var off = new Vector2(tmp.X / range.X, tmp.Y / range.Y);

        var clamped = new Vector2(
            float.Clamp(off.X, 0, 1),
            float.Clamp(off.Y, 0, 1)
        );
        return clamped;
    }

    /// <summary>
    /// Maps an offset (0.0->1.0) to a value
    /// </summary>
    /// <param name="offset"></param>
    /// <returns></returns>
    public Vector2 UnmapValue(Vector2 offset)
    {
        Vector2 range = Max - Min;
        return new Vector2(range.X * offset.X, range.Y * offset.Y) + Min;
    }

    /// <summary>
    /// Maps an input value to an offset (0.0->1.0) for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public float MapAxis(int axis, float value)
    {
        Vector2 input = Min;
        if (axis == 0) input.X = value;
        else input.Y = value;
        Vector2 output = MapValue(input);
        if (axis == 0) return output.X;
        else return output.Y;
    }

    /// <summary>
    /// Maps an internal value (0.0->1.0) to the input range for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public float UnmapAxis(uint axis, float offset)
    {
        Vector2 input = Min;
        if (axis == 0) input.X = offset;
        else input.Y = offset;
        Vector2 output = UnmapValue(input);
        if (axis == 0) return output.X;
        else return output.Y;
    }

    /// <summary>
    /// Gets the axis point closest to a given offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public uint GetClosestAxisPointIndex(int axis, float offset)
    {
        uint closestPoint = 0;
        float closestDist = float.NegativeInfinity;

        for (uint i = 0; i < AxisPoints[axis].Length; i++)
        {
            var pointVal = AxisPoints[axis][i];
            float dist = float.Abs(pointVal - offset);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPoint = i;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// Find the keypoint closest to the current value
    /// </summary>
    /// <returns></returns>
    public Vector2UInt FindClosestKeypoint()
    {
        return FindClosestKeypoint(Value);
    }

    /// <summary>
    /// Find the keypoint closest to a value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2UInt FindClosestKeypoint(Vector2 value)
    {
        Vector2 mapped = MapValue(value);
        uint x = GetClosestAxisPointIndex(0, mapped.X);
        uint y = GetClosestAxisPointIndex(1, mapped.Y);

        return new Vector2UInt(x, y);
    }

    /// <summary>
    /// Find the keypoint closest to the current value
    /// </summary>
    /// <returns></returns>
    public Vector2 GetClosestKeypointValue()
    {
        return GetKeypointValue(FindClosestKeypoint());
    }

    /// <summary>
    /// Find the keypoint closest to a value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2 GetClosestKeypointValue(Vector2 value)
    {
        return GetKeypointValue(FindClosestKeypoint(value));
    }

    /// <summary>
    /// Find a binding by node ref and name
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <returns></returns>
    public ParameterBinding? GetBinding(Node n, string bindingName)
    {
        foreach (var binding in Bindings)
        {
            if (binding.GetNode() != n) continue;
            if (binding.GetName() == bindingName) return binding;
        }
        return null;
    }

    /// <summary>
    /// Check if a binding exists for a given node and name
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <returns></returns>
    public bool HasBinding(Node n, string bindingName)
    {
        foreach (var binding in Bindings)
        {
            if (binding.GetNode() != n) continue;
            if (binding.GetName() == bindingName) return true;
        }
        return false;
    }

    /// <summary>
    /// Check if any bindings exists for a given node
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public bool HasAnyBinding(Node n)
    {
        foreach (var binding in Bindings)
        {
            if (binding.GetNode() == n) return true;
        }
        return false;
    }

    /// <summary>
    /// Create a new binding (without adding it) for a given node and name
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <param name="setZero"></param>
    /// <returns></returns>
    public ParameterBinding CreateBinding(Node n, string bindingName, bool setZero = true)
    {
        ParameterBinding b;
        if (bindingName == "deform")
        {
            b = new DeformationParameterBinding(this, n, bindingName);
        }
        else
        {
            b = new ValueParameterBinding(this, n, bindingName);
        }

        if (setZero)
        {
            var zeroIndex = FindClosestKeypoint(new Vector2(0, 0));
            var zero = GetKeypointValue(zeroIndex);
            if (float.Abs(zero.X) < 0.001 && float.Abs(zero.Y) < 0.001)
            {
                b.Reset(zeroIndex);
            }
        }

        return b;
    }

    /// <summary>
    /// Find a binding if it exists, or create and add a new one, and return it
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <param name="setZero"></param>
    /// <returns></returns>
    public ParameterBinding? GetOrAddBinding(Node n, string bindingName, bool setZero = true)
    {
        var binding = GetBinding(n, bindingName);
        if (binding is null)
        {
            binding = CreateBinding(n, bindingName, setZero);
            AddBinding(binding);
        }
        return binding;
    }

    /// <summary>
    /// Add a new binding (must not exist)
    /// </summary>
    /// <param name="binding"></param>
    public void AddBinding(ParameterBinding binding)
    {
        if (HasBinding(binding.GetNode(), binding.GetName()))
        {
            throw new Exception("binding is exist");
        }
        Bindings.Add(binding);
    }

    /// <summary>
    /// Remove an existing binding by ref
    /// </summary>
    /// <param name="binding"></param>
    public void RemoveBinding(ParameterBinding binding)
    {
        Bindings.Remove(binding);
    }

    public void MakeIndexable()
    {
        IndexableName = Name.ToLower();
    }
}
