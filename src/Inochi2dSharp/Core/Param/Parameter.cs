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
    public List<List<float>> axisPoints = [[0, 1], [0, 1]];

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
            axisPoints = array.ToFloatList();
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
        foreach (var binding in bindings) 
        {
            if (puppet.find<Node>(binding.getNodeUUID()) != null) 
            {
                binding.finalize(puppet);
                validBindingList.Add(binding);
            }
        }

        bindings = validBindingList;
    }

    public void findOffset(Vector2 offset, out Vector2Uint index, out Vector2 outOffset)
    {
        index = new();
        outOffset = new();
        void interpAxis(int axis, float val, out uint index, out float offset)
        {
            var pos = axisPoints[axis];

            for (int i = 0; i < pos.Count - 1; i++)
            {
                if (pos[i + 1] > val || i == (pos.Count - 2))
                {
                    index = (uint)i;
                    offset = (val - pos[i]) / (pos[i + 1] - pos[i]);
                    return;
                }
            }

            index = 0;
            offset = 0;
        }

        interpAxis(0, offset.X, out index.X, out outOffset.X);
        if (isVec2) interpAxis(1, offset.Y, out index.Y, out outOffset.Y);
    }

    public void update()
    {
        if (!active)
            return;

        lastInternal = (value + iadd.Csum()) * imul.Avg();

        findOffset(this.mapValue(lastInternal), out var index, out var offset_);
        foreach (var binding in bindings)
        {
            binding.apply(index, offset_);
        }

        // Reset combinatorics
        iadd.clear();
        imul.clear();
    }

    public void pushIOffset(Vector2 offset, string mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = mergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                this.value = offset;
                break;
            case ParamMergeMode.Additive:
                iadd.Add(offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                imul.Add(offset, 1);
                break;
            case ParamMergeMode.Weighted:
                imul.Add(offset, weight);
                break;
            default: break;
        }
    }

    public void pushIOffsetAxis(int axis, float offset, string mode = ParamMergeMode.Passthrough, float weight = 1)
    {
        if (mode == ParamMergeMode.Passthrough) mode = mergeMode;
        switch (mode)
        {
            case ParamMergeMode.Forced:
                value[axis] = offset;
                break;
            case ParamMergeMode.Additive:
                iadd.Add(axis, offset, 1);
                break;
            case ParamMergeMode.Multiplicative:
                imul.Add(axis, offset, 1);
                break;
            case ParamMergeMode.Weighted:
                imul.Add(axis, offset, weight);
                break;
            default: break;
        }
    }

    /// <summary>
    /// Get number of points for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <returns></returns>
    public int axisPointCount(int axis = 0)
    {
        return axisPoints[axis].Count;
    }

    /// <summary>
    /// Move an axis point to a new offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="oldidx"></param>
    /// <param name="newoff"></param>
    public void moveAxisPoint(int axis, int oldidx, float newoff)
    {
        if (oldidx <= 0 && oldidx >= this.axisPointCount(axis) - 1)
        {
            throw new Exception("invalid point index");  
        }
        if (newoff <= 0 && newoff >= 1)
        {
            throw new Exception("offset out of bounds");
        }
        if (isVec2 )
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
        int index;
        for (index = 1; index < axisPoints[axis].Count; index++)
        {
            if (axisPoints[axis][index + 1] > newoff)
                break;
        }

        if (oldidx != index)
        {
            // BUG: Apparently deleting the oldindex and replacing it with newindex causes a crash.

            // Insert it into the new position in the list
            float swap = axisPoints[axis][oldidx];
            var tempList = new List<float>(axisPoints[axis]);
            tempList.RemoveAt(oldidx);
            tempList.Insert(index, swap);

            axisPoints[axis] = [.. tempList];
#if DEBUG
            Console.WriteLine("after move ", axisPointCount(0));
#endif
        }

        // Tell all bindings to reinterpolate
        foreach (var binding in bindings) 
        {
            binding.moveKeypoints(axis, oldidx, index);
        }
    }

    /// <summary>
    /// Add a new axis point at the given offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="off"></param>
    public void insertAxisPoint(int axis, float off)
    {
        if (off <= 0 || off >= 1)
        {
            throw new Exception("offset out of bounds");
        }
        if (isVec2)
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
        int index;
        for (index = 1; index < axisPoints[axis].Count; index++)
        {
            if (axisPoints[axis][index] > off)
                break;
        }

        // Insert it into the position list
        axisPoints[axis].Insert(index, off);

        // Tell all bindings to insert space into their arrays
        foreach (var binding in bindings)
        {
            binding.insertKeypoints(axis, index);
        }
    }

    /// <summary>
    /// Delete a specified axis point by index
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="index"></param>
    public void deleteAxisPoint(int axis, int index)
    {
        if (isVec2)
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
        if (index >= (axisPoints[axis].Count - 1))
        {
            throw new Exception("cannot delete axis point at 1");
        }

        // Remove the keypoint
        axisPoints[axis].RemoveAt(index);

        // Tell all bindings to remove it from their arrays
        foreach (var binding in bindings) 
        {
            binding.deleteKeypoints(axis, index);
        }
    }

    /// <summary>
    /// Flip the mapping across an axis
    /// </summary>
    /// <param name="axis"></param>
    public void reverseAxis(int axis)
    {
        axisPoints[axis].Reverse();
        for (var i=0;i<axisPoints[axis].Count;i++) 
        {
            axisPoints[axis][i] = 1 - axisPoints[axis][i];
        }
        foreach (var binding in bindings)
        {
            binding.reverseAxis(axis);
        }
    }

    /// <summary>
    /// Get the offset (0..1) of a specified keypoint index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector2 getKeypointOffset(Vector2Uint index)
    {
        return new(axisPoints[0][(int)index.X], axisPoints[1][(int)index.Y]);
    }

    /// <summary>
    /// Get the value at a specified keypoint index
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public Vector2 getKeypointValue(Vector2Uint index)
    {
        return unmapValue(getKeypointOffset(index));
    }

    /// <summary>
    /// Maps an input value to an offset (0.0->1.0)
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2 mapValue(Vector2 value)
    {
        var range = max - min;
        var tmp = (value - min);
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
    public Vector2 unmapValue(Vector2 offset)
    {
        Vector2 range = max - min;
        return new Vector2(range.X * offset.X, range.Y * offset.Y) + min;
    }

    /// <summary>
    /// Maps an input value to an offset (0.0->1.0) for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public float mapAxis(int axis, float value)
    {
        Vector2 input = min;
        if (axis == 0) input.X = value;
        else input.Y = value;
        Vector2 output = mapValue(input);
        if (axis == 0) return output.X;
        else return output.Y;
    }

    /// <summary>
    /// Maps an internal value (0.0->1.0) to the input range for an axis
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public float unmapAxis(uint axis, float offset)
    {
        Vector2 input = min;
        if (axis == 0) input.X = offset;
        else input.Y = offset;
        Vector2 output = unmapValue(input);
        if (axis == 0) return output.X;
        else return output.Y;
    }

    /// <summary>
    /// Gets the axis point closest to a given offset
    /// </summary>
    /// <param name="axis"></param>
    /// <param name="offset"></param>
    /// <returns></returns>
    public uint getClosestAxisPointIndex(int axis, float offset)
    {
        uint closestPoint = 0;
        float closestDist = float.NegativeInfinity;

        for(int i=0; i< axisPoints[axis].Count; i++) 
        {
            var pointVal = axisPoints[axis][i];
            float dist = float.Abs(pointVal - offset);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestPoint = (uint)i;
            }
        }

        return closestPoint;
    }

    /// <summary>
    /// Find the keypoint closest to the current value
    /// </summary>
    /// <returns></returns>
    public Vector2Uint findClosestKeypoint()
    {
        return findClosestKeypoint(value);
    }

    /**
        Find the keypoint closest to a value
    */
    public Vector2Uint findClosestKeypoint(Vector2 value)
    {
        Vector2 mapped = mapValue(value);
        uint x = getClosestAxisPointIndex(0, mapped.X);
        uint y = getClosestAxisPointIndex(1, mapped.Y);

        return new Vector2Uint(x, y);
    }

    /// <summary>
    /// Find the keypoint closest to the current value
    /// </summary>
    /// <returns></returns>
    public Vector2 getClosestKeypointValue()
    {
        return getKeypointValue(findClosestKeypoint());
    }

    /// <summary>
    /// Find the keypoint closest to a value
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public Vector2 getClosestKeypointValue(Vector2 value)
    {
        return getKeypointValue(findClosestKeypoint(value));
    }

    /// <summary>
    /// Find a binding by node ref and name
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <returns></returns>
    public ParameterBinding? getBinding(Node n, string bindingName)
    {
        foreach (var binding in bindings)
        {
            if (binding.getNode() != n) continue;
            if (binding.getName() == bindingName) return binding;
        }
        return null;
    }

    /// <summary>
    /// Check if a binding exists for a given node and name
    /// </summary>
    /// <param name="n"></param>
    /// <param name="bindingName"></param>
    /// <returns></returns>
    public bool hasBinding(Node n, string bindingName)
    {
        foreach (var binding in bindings) 
        {
            if (binding.getNode() != n) continue;
            if (binding.getName() == bindingName) return true;
        }
        return false;
    }

    /// <summary>
    /// Check if any bindings exists for a given node
    /// </summary>
    /// <param name="n"></param>
    /// <returns></returns>
    public bool hasAnyBinding(Node n)
    {
        foreach (var binding in bindings) 
        {
            if (binding.getNode() == n) return true;
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
    public ParameterBinding createBinding(Node n, string bindingName, bool setZero = true)
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
            var zeroIndex = findClosestKeypoint(new Vector2(0, 0));
            var zero = getKeypointValue(zeroIndex);
            if (float.Abs(zero.X) < 0.001 && float.Abs(zero.Y) < 0.001) b.reset(zeroIndex);
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
    public ParameterBinding? getOrAddBinding(Node n, string bindingName, bool setZero = true)
    {
        var binding = getBinding(n, bindingName);
        if (binding is null)
        {
            binding = createBinding(n, bindingName, setZero);
            addBinding(binding);
        }
        return binding;
    }

    /// <summary>
    /// Add a new binding (must not exist)
    /// </summary>
    /// <param name="binding"></param>
    public void addBinding(ParameterBinding binding)
    {
        if (hasBinding(binding.getNode(), binding.getName()))
        {
            throw new Exception("binding is exist");
        }
        bindings.Add(binding);
    }

    /// <summary>
    /// Remove an existing binding by ref
    /// </summary>
    /// <param name="binding"></param>
    public void removeBinding(ParameterBinding binding)
    {
        bindings.Remove(binding);
    }

    public void makeIndexable()
    {
        indexableName = name.ToLower();
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
