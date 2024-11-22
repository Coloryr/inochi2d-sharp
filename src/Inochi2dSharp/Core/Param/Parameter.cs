using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public class Parameter : IDisposable
{
    private Combinator iadd;
    private Combinator imul;

    /// <summary>
    /// Unique ID of parameter
    /// </summary>
    public uint uuid;

    /// <summary>
    /// Name of the parameter
    /// </summary>
    public string name;

    /// <summary>
    /// Optimized indexable name generated at runtime
    /// DO NOT SERIALIZE THIS.
    /// </summary>
    public string indexableName;

    /// <summary>
    /// Whether this parameter updates the model
    /// </summary>
    public bool active = true;

    /// <summary>
    /// The current parameter value
    /// </summary>
    public Vector2 value = new(0);

    /// <summary>
    /// The previous internal value offset
    /// </summary>
    public Vector2 lastInternal = new(0);

    /// <summary>
    /// Parameter merge mode
    /// <see cref="ParamMergeMode"/>
    /// </summary>
    public string mergeMode;

    /// <summary>
    /// The default value
    /// </summary>
    public Vector2 defaults = new(0);

    /// <summary>
    /// Whether the parameter is 2D
    /// </summary>
    public bool isVec2;

    /// <summary>
    /// The parameter's minimum bounds
    /// </summary>
    public Vector2 min = new(0, 0);

    /// <summary>
    /// The parameter's maximum bounds
    /// </summary>
    public Vector2 max = new(1, 1);

    /// <summary>
    /// Position of the keypoints along each axis
    /// </summary>
    public float[][] axisPoints = [[0, 1], [0, 1]];

    /// <summary>
    /// Binding to targets
    /// </summary>
    public List<ParameterBinding> bindings = [];

    public Parameter()
    { 
        
    }

    public Parameter(string name, bool isVec2)
    {
        this.uuid = NodeHelper.InCreateUUID();
        this.name = name;
        this.isVec2 = isVec2;
        if (!isVec2)
            axisPoints[1] = [0];

        this.makeIndexable();
    }

    /// <summary>
    /// Gets the value normalized to the internal range (0.0->1.0)
    /// </summary>
    /// <returns></returns>
    public Vector2 normalizedValue()
    {
        return this.mapValue(value);
    }

    /// <summary>
    /// Sets the value normalized up from the internal range (0.0->1.0)
    /// to the user defined range.
    /// </summary>
    /// <param name="value"></param>
    public void normalizedValue(Vector2 value)
    {
        this.value = new Vector2(
            value.X * (max.X - min.X) + min.X,
            value.Y * (max.Y - min.Y) + min.Y
        );
    }

    /// <summary>
    /// Clone this parameter
    /// </summary>
    /// <returns></returns>
    public Parameter Copy()
    {
        var newParam = new Parameter(name + " (Copy)", isVec2)
        {
            min = min,
            max = max,
            axisPoints = [.. axisPoints]
        };

        foreach (var binding in bindings)
        {
            var newBinding = newParam.createBinding(
                binding.getNode(),
                binding.getName(),
                false
            );
            newBinding.interpolateMode = binding.interpolateMode;
            for (uint x = 0; x < axisPointCount(0); x++)
            {
                for (uint y = 0; y < axisPointCount(1); y++)
                {
                    binding.copyKeypointToBinding(new Vector2Uint(x, y), newBinding, new Vector2Uint(x, y));
                }
            }
            newParam.addBinding(newBinding);
        }

        return newParam;
    }

    /// <summary>
    /// Deserializes a parameter
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JObject data)
    {
        var temp = data["uuid"];
        if (temp != null)
        {
            uuid = (uint)temp;
        }

        temp = data["name"];
        if (temp != null)
        {
            name = temp.ToString();
        }

        temp = data["is_vec2"];
        if (temp != null)
        {
            isVec2 = (bool)temp;
        }

        temp = data["min"];
        if (temp != null)
        {
            min = temp.ToVector2();
        }

        temp = data["max"];
        if (temp != null)
        {
            max = temp.ToVector2();
        }

        temp = data["axis_points"];
        if (temp is JArray array)
        {
            axisPoints = array.ToArray<float>();
        }

        temp = data["defaults"];
        if (temp != null)
        {
            defaults = temp.ToVector2();
        }

        temp = data["merge_mode"];
        if (temp != null)
        {
            mergeMode = temp.ToString();
        }

        temp = data["bindings"];
        if (temp != null)
        {
            foreach (JObject child in temp) 
            {
                var temp1 = child["param_name"];
                // Skip empty children
                if (temp1 == null) continue;

                string paramName = temp1.ToString();

                if (paramName == "deform")
                {
                    var binding = new DeformationParameterBinding(this);
                    binding.deserialize(child);
                    bindings.Add(binding);
                }
                else
                {
                    var binding = new ValueParameterBinding(this);
                    binding.deserialize(child);
                    bindings.Add(binding);
                }
            }
        }
    }

    public void reconstruct(Puppet puppet)
    {
        foreach (var binding in bindings) 
        {
            binding.reconstruct(puppet);
        }
    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public void finalize(Puppet puppet)
    {
        makeIndexable();
        value = defaults;

        var validBindingList = new List<ParameterBinding>();
        foreach (var binding in bindings) {
            if (puppet.findNode(binding.getNodeUUID())) {
                binding.finalize(puppet);
                validBindingList.Add(binding);
            }
        }

        bindings = validBindingList;
    }

    void findOffset(vec2 offset, out vec2u index, out vec2 outOffset)
    {
        void interpAxis(uint axis, float val, out uint index, out float offset)
        {
            float[] pos = axisPoints[axis];

            foreach (i; 0..pos.length - 1) {
                if (pos[i + 1] > val || i == (pos.length - 2))
                {
                    index = cast(uint)i;
                    offset = (val - pos[i]) / (pos[i + 1] - pos[i]);
                    return;
                }
            }
        }

        interpAxis(0, offset.x, index.x, outOffset.x);
        if (isVec2) interpAxis(1, offset.y, index.y, outOffset.y);
    }

    void update()
    {
        vec2u index;
        vec2 offset_;

        if (!active)
            return;

        lastInternal = (value + iadd.csum()) * imul.avg();

        findOffset(this.mapValue(lastInternal), index, offset_);
        foreach (binding; bindings) {
            binding.apply(index, offset_);
        }

        // Reset combinatorics
        iadd.clear();
        imul.clear();
    }

    void pushIOffset(vec2 offset, ParamMergeMode mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = mergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                this.value = offset;
                break;
            case ParamMergeMode.Additive:
                iadd.add(offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                imul.add(offset, 1);
                break;
            case ParamMergeMode.Weighted:
                imul.add(offset, weight);
                break;
            default: break;
        }
    }

    void pushIOffsetAxis(int axis, float offset, ParamMergeMode mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = mergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                this.value.vector[axis] = offset;
                break;
            case ParamMergeMode.Additive:
                iadd.add(axis, offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                imul.add(axis, offset, 1);
                break;
            case ParamMergeMode.Weighted:
                imul.add(axis, offset, weight);
                break;
            default: break;
        }
    }

    /**
        Get number of points for an axis
    */
    public uint axisPointCount(uint axis = 0)
    {
        return cast(uint)axisPoints[axis].length;
    }

    /**
        Move an axis point to a new offset
    */
    void moveAxisPoint(uint axis, uint oldidx, float newoff)
    {
        assert(oldidx > 0 && oldidx < this.axisPointCount(axis) - 1, "invalid point index");
        assert(newoff > 0 && newoff < 1, "offset out of bounds");
        if (isVec2)
            assert(axis <= 1, "bad axis");
        else
            assert(axis == 0, "bad axis");

        // Find the index at which to insert
        uint index;
        for (index = 1; index < axisPoints[axis].length; index++)
        {
            if (axisPoints[axis][index + 1] > newoff)
                break;
        }

        if (oldidx != index)
        {
            // BUG: Apparently deleting the oldindex and replacing it with newindex causes a crash.

            // Insert it into the new position in the list
            auto swap = axisPoints[oldidx];
            axisPoints[axis] = axisPoints[axis].remove(oldidx);
            axisPoints[axis].insertInPlace(index, swap);
            debug writeln("after move ", this.axisPointCount(0));
        }

        // Tell all bindings to reinterpolate
        foreach (binding; bindings) {
            binding.moveKeypoints(axis, oldidx, index);
        }
    }

    /**
        Add a new axis point at the given offset
    */
    void insertAxisPoint(uint axis, float off)
    {
        assert(off > 0 && off < 1, "offset out of bounds");
        if (isVec2)
            assert(axis <= 1, "bad axis");
        else
            assert(axis == 0, "bad axis");

        // Find the index at which to insert
        uint index;
        for (index = 1; index < axisPoints[axis].length; index++)
        {
            if (axisPoints[axis][index] > off)
                break;
        }

        // Insert it into the position list
        axisPoints[axis].insertInPlace(index, off);

        // Tell all bindings to insert space into their arrays
        foreach (binding; bindings) {
            binding.insertKeypoints(axis, index);
        }
    }

    /**
        Delete a specified axis point by index
    */
    void deleteAxisPoint(uint axis, uint index)
    {
        if (isVec2)
            assert(axis <= 1, "bad axis");
        else
            assert(axis == 0, "bad axis");

        assert(index > 0, "cannot delete axis point at 0");
        assert(index < (axisPoints[axis].length - 1), "cannot delete axis point at 1");

        // Remove the keypoint
        axisPoints[axis] = axisPoints[axis].remove(index);

        // Tell all bindings to remove it from their arrays
        foreach (binding; bindings) {
            binding.deleteKeypoints(axis, index);
        }
    }

    /**
        Flip the mapping across an axis
    */
    void reverseAxis(uint axis)
    {
        axisPoints[axis].reverse();
        foreach (ref i; axisPoints[axis]) {
            i = 1 - i;
        }
        foreach (binding; bindings) {
            binding.reverseAxis(axis);
        }
    }

    /**
        Get the offset (0..1) of a specified keypoint index
    */
    vec2 getKeypointOffset(vec2u index)
    {
        return vec2(axisPoints[0][index.x], axisPoints[1][index.y]);
    }

    /**
        Get the value at a specified keypoint index
    */
    vec2 getKeypointValue(vec2u index)
    {
        return unmapValue(getKeypointOffset(index));
    }

    /**
        Maps an input value to an offset (0.0->1.0)
    */
    vec2 mapValue(vec2 value)
    {
        vec2 range = max - min;
        vec2 tmp = (value - min);
        vec2 off = vec2(tmp.x / range.x, tmp.y / range.y);

        vec2 clamped = vec2(
            clamp(off.x, 0, 1),
            clamp(off.y, 0, 1),
        );
        return clamped;
    }

    /**
        Maps an offset (0.0->1.0) to a value
    */
    vec2 unmapValue(vec2 offset)
    {
        vec2 range = max - min;
        return vec2(range.x * offset.x, range.y * offset.y) + min;
    }

    /**
        Maps an input value to an offset (0.0->1.0) for an axis
    */
    float mapAxis(uint axis, float value)
    {
        vec2 input = min;
        if (axis == 0) input.x = value;
        else input.y = value;
        vec2 output = mapValue(input);
        if (axis == 0) return output.x;
        else return output.y;
    }

    /**
        Maps an internal value (0.0->1.0) to the input range for an axis
    */
    float unmapAxis(uint axis, float offset)
    {
        vec2 input = min;
        if (axis == 0) input.x = offset;
        else input.y = offset;
        vec2 output = unmapValue(input);
        if (axis == 0) return output.x;
        else return output.y;
    }

    /**
        Gets the axis point closest to a given offset
    */
    uint getClosestAxisPointIndex(uint axis, float offset)
    {
        uint closestPoint = 0;
        float closestDist = float.infinity;

        foreach (i, pointVal; axisPoints[axis]) {
            float dist = abs(pointVal - offset);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPoint = cast(uint)i;
            }
        }

        return closestPoint;
    }

    /**
        Find the keypoint closest to the current value
    */
    vec2u findClosestKeypoint()
    {
        return findClosestKeypoint(value);
    }

    /**
        Find the keypoint closest to a value
    */
    vec2u findClosestKeypoint(vec2 value)
    {
        vec2 mapped = mapValue(value);
        uint x = getClosestAxisPointIndex(0, mapped.x);
        uint y = getClosestAxisPointIndex(1, mapped.y);

        return vec2u(x, y);
    }

    /**
        Find the keypoint closest to the current value
    */
    vec2 getClosestKeypointValue()
    {
        return getKeypointValue(findClosestKeypoint());
    }

    /**
        Find the keypoint closest to a value
    */
    vec2 getClosestKeypointValue(vec2 value)
    {
        return getKeypointValue(findClosestKeypoint(value));
    }

    /**
        Find a binding by node ref and name
    */
    ParameterBinding getBinding(Node n, string bindingName)
    {
        foreach (ref binding; bindings) {
            if (binding.getNode() != n) continue;
            if (binding.getName == bindingName) return binding;
        }
        return null;
    }

    /**
        Check if a binding exists for a given node and name
    */
    bool hasBinding(Node n, string bindingName)
    {
        foreach (ref binding; bindings) {
            if (binding.getNode() != n) continue;
            if (binding.getName == bindingName) return true;
        }
        return false;
    }

    /**
        Check if any bindings exists for a given node
    */
    bool hasAnyBinding(Node n)
    {
        foreach (ref binding; bindings) {
            if (binding.getNode() == n) return true;
        }
        return false;
    }

    /**
        Create a new binding (without adding it) for a given node and name
    */
    ParameterBinding createBinding(Node n, string bindingName, bool setZero = true)
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
            vec2u zeroIndex = findClosestKeypoint(vec2(0, 0));
            vec2 zero = getKeypointValue(zeroIndex);
            if (abs(zero.x) < 0.001 && abs(zero.y) < 0.001) b.reset(zeroIndex);
        }

        return b;
    }

    /**
        Find a binding if it exists, or create and add a new one, and return it
    */
    ParameterBinding getOrAddBinding(Node n, string bindingName, bool setZero = true)
    {
        ParameterBinding binding = getBinding(n, bindingName);
        if (binding is null)
        {
            binding = createBinding(n, bindingName, setZero);
            addBinding(binding);
        }
        return binding;
    }

    /**
        Add a new binding (must not exist)
    */
    void addBinding(ParameterBinding binding)
    {
        assert(!hasBinding(binding.getNode, binding.getName));
        bindings ~= binding;
    }

    /**
        Remove an existing binding by ref
    */
    void removeBinding(ParameterBinding binding)
    {
        import std.algorithm.searching : countUntil;
        import std.algorithm.mutation : remove;
        ptrdiff_t idx = bindings.countUntil(binding);
        if (idx >= 0)
        {
            bindings = bindings.remove(idx);
        }
    }

    void makeIndexable()
    {
        import std.uni: toLower;
        indexableName = name.toLower;
    }

    /// <summary>
    /// Serializes a parameter
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JObject serializer)
    {
        serializer.Add("uuid", uuid);
        serializer.Add("name", name);
        serializer.Add("is_vec2", isVec2);
        serializer.Add("min", min.ToToken());
        serializer.Add("max", max.ToToken());
        serializer.Add("defaults", defaults.ToToken());
        serializer.Add("axis_points", axisPoints.ToToken());
        serializer.Add("merge_mode", mergeMode);
        var list = new JArray();
        foreach (var item in bindings)
        {
            var obj = new JObject();
            item.serialize(obj);
            list.Add(item);
        }
        serializer.Add("bindings", list);
    }

    public void Dispose()
    {
        NodeHelper.InUnloadUUID(uuid);
    }
}
