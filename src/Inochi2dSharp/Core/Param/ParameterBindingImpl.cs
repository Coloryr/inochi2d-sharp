using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public abstract class ParameterBindingImpl<T> : ParameterBinding
{
    /// <summary>
    /// Node reference (for deserialization)
    /// </summary>
    private uint nodeRef;

    private InterpolateMode interpolateMode_ = InterpolateMode.Linear;

    /// <summary>
    /// Parent Parameter owning this binding
    /// </summary>
    public Parameter parameter;

    /// <summary>
    /// Reference to what parameter we're binding to
    /// </summary>
    public BindTarget target;

    /// <summary>
    /// The value at each 2D keypoint
    /// </summary>
    public T[][] values;

    /// <summary>
    /// Whether the value at each 2D keypoint is user-set
    /// </summary>
    public bool[][] isSet_;

    public ParameterBindingImpl(Parameter parameter)
    {
        this.parameter = parameter;
    }

    public ParameterBindingImpl(Parameter parameter, Node targetNode, string paramName) {
        this.parameter = parameter;
        target = new()
        {
            node = targetNode,
            paramName = paramName
        };

        clear();
    }

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public override BindTarget getTarget()
    {
        return target;
    }

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public override string getName()
    {
        return target.paramName;
    }

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Node getNode()
    {
        return target.node;
    }

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public override uint getNodeUUID()
    {
        return nodeRef;
    }

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public override bool[][] getIsSet()
    {
        return isSet_;
    }

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public override uint getSetCount()
    {
        uint count = 0;
        for (int x = 0; x < isSet_.Length; x++)
        {
            for (int y = 0; y < isSet_[x].Length; y++)
            {
                if (isSet_[x][y]) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Serializes a binding
    /// </summary>
    /// <param name="serializer"></param>
    public override void serialize(JObject serializer)
    {
        serializer.Add("node", target.node.UUID);
        serializer.Add("param_name", target.paramName);
        serializer.Add("values", values.ToToken());
        serializer.Add("isSet", isSet_.ToToken());
        serializer.Add("interpolate_mode", interpolateMode_.ToString());
    }

    /// <summary>
    /// Deserializes a binding
    /// </summary>
    /// <param name="data"></param>
    public override void deserialize(JObject data)
    {
        var temp = data["node"];
        if (temp != null)
        {
            nodeRef = (uint)temp;
        }
        temp = data["param_name"];
        if (temp != null)
        {
            target.paramName = temp.ToString();
        }

        temp = data["values"];
        if (temp is JArray array)
        {
            values = array.ToArray<T>();
        }

        temp = data["isSet"];
        if (temp is JArray array1)
        {
            isSet_ = array1.ToArray<bool>();
        }

        temp = data["interpolate_mode"];
        if (temp == null || !Enum.TryParse<InterpolateMode>(temp.ToString(), out interpolateMode_))
        {
            interpolateMode_ = InterpolateMode.Linear;
        }

        int xCount = parameter.axisPointCount(0);
        int yCount = parameter.axisPointCount(1);

        if (this.values.Length != xCount)
        {
            throw new Exception("Mismatched X value count");
        }
        foreach (var i in this.values)
        {
            if (i.Length != yCount)
            {
                throw new Exception("Mismatched Y value count");
            }
        }

        if (this.isSet_.Length != xCount)
        {
            throw new Exception("Mismatched X isSet_ count");  
        }
        foreach (var i in this.isSet_) 
        {
            if (i.Length != yCount)
            {
                throw new Exception("Mismatched Y isSet_ count");
            }
        }
    }

    public override void reconstruct(Puppet puppet)
    { 
    
    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public override void finalize(Puppet puppet)
    {
        //        writefln("finalize binding %s", this.getName());

        this.target.node = puppet.find<Node>(nodeRef);
        //        writefln("node for %d = %x", nodeRef, &(target.node));
    }

    /// <summary>
    /// Clear all keypoint data
    /// </summary>
    public override void clear()
    {
        int xCount = parameter.axisPointCount(0);
        int yCount = parameter.axisPointCount(1);

        values = new T[xCount][];
        isSet_ = new bool[xCount][];
        for (int x = 0; x < xCount; x++)
        {
            isSet_[x] = new bool[yCount];

            values[x] = new T[yCount];
            for (int y = 0; y < yCount; y++)
            {
                clearValue(ref values[x][y]);
            }
        }
    }

    public void clearValue(ref T i)
    {
        // Default: no-op
    }

    /// <summary>
    /// Gets the value at the specified point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public T getValue(Vector2Uint point)
    {
        return values[point.X][point.Y];
    }

    /// <summary>
    /// Sets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    /// <param name="value"></param>
    public void setValue(Vector2Uint point, T value)
    {
        values[point.X][point.Y] = value;
        isSet_[point.X][point.Y] = true;

        reInterpolate();
    }

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public override void setCurrent(Vector2Uint point)
    {
        isSet_[point.X][point.Y] = true;

        reInterpolate();
    }

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public override void unset(Vector2Uint point)
    {
        clearValue(ref values[point.X][point.Y]);
        isSet_[point.X][point.Y] = false;

        reInterpolate();
    }

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public override void reset(Vector2Uint point)
    {
        clearValue(ref values[point.X][point.Y]);
        isSet_[point.X][point.Y] = true;

        reInterpolate();
    }

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public override bool isSet(Vector2Uint index)
    {
        return isSet_[index.X][index.Y];
    }

    /// <summary>
    /// Flip the keypoints on an axis
    /// </summary>
    /// <param name="axis"></param>
    public override void reverseAxis(int axis)
    {
        if (axis == 0)
        {
            values = values.Reverse().ToArray();
            isSet_ = isSet_.Reverse().ToArray();
        }
        else
        {
            for (int i = 0; i < values.Length; i++)
            {
                values[i] = values[i].Reverse().ToArray();
            }
            for (int i = 0; i < isSet_.Length; i++)
            {
                isSet_[i] = isSet_[i].Reverse().ToArray();
            }
        }
    }

    /// <summary>
    /// Re-calculate interpolation
    /// </summary>
    public override void reInterpolate()
    {
        int xCount = parameter.axisPointCount(0);
        int yCount = parameter.axisPointCount(1);

        // Currently valid points
        var valid = new List<bool[]>();
        int validCount = 0;
        int totalCount = xCount * yCount;

        // Initialize validity map to user-set points
        for (int x = 0; x < xCount; x++)
        {
            valid.Add([.. isSet_[x]]);
            for (int y = 0; y < yCount; y++)
            {
                if (isSet_[x][y]) validCount++;
            }
        }

        // If there are zero valid points, just clear ourselves
        if (validCount == 0)
        {
            clear();
            return;
        }

        // Whether any given point was just set
        bool[][] newlySet = new bool[xCount][];

        // List of indices to commit
        var commitPoints = new List<Vector2Uint>();

        // Used by extendAndIntersect for x/y factor
        float[][] interpDistance;
        interpDistance.length = xCount;
        foreach (x; 0..xCount) {
            interpDistance[x].length = yCount;
        }

        // Current interpolation axis
        bool yMajor = false;

        // Helpers to handle interpolation across both axes more easily
        uint majorCnt()
        {
            if (yMajor) return yCount;
            else return xCount;
        }
        uint minorCnt()
        {
            if (yMajor) return xCount;
            else return yCount;
        }
        bool isValid(uint maj, uint min)
        {
            if (yMajor) return valid[min][maj];
            else return valid[maj][min];
        }
        bool isNewlySet(uint maj, uint min)
        {
            if (yMajor) return newlySet[min][maj];
            else return newlySet[maj][min];
        }
        T get(uint maj, uint min)
        {
            if (yMajor) return values[min][maj];
            else return values[maj][min];
        }
        float getDistance(uint maj, uint min)
        {
            if (yMajor) return interpDistance[min][maj];
            else return interpDistance[maj][min];
        }
        void reset(uint maj, uint min, T val, float distance = 0)
        {
            if (yMajor)
            {
                //debug writefln("set (%d, %d) -> %s", min, maj, val);
                assert(!valid[min][maj]);
                values[min][maj] = val;
                interpDistance[min][maj] = distance;
                newlySet[min][maj] = true;
            }
            else
            {
                //debug writefln("set (%d, %d) -> %s", maj, min, val);
                assert(!valid[maj][min]);
                values[maj][min] = val;
                interpDistance[maj][min] = distance;
                newlySet[maj][min] = true;
            }
        }
        void set(uint maj, uint min, T val, float distance = 0)
        {
            reset(maj, min, val, distance);
            if (yMajor) commitPoints ~= vec2u(min, maj);
            else commitPoints ~= vec2u(maj, min);
        }
        float axisPoint(uint idx)
        {
            if (yMajor) return parameter.axisPoints[0][idx];
            else return parameter.axisPoints[1][idx];
        }
        T interp(uint maj, uint left, uint mid, uint right)
        {
            float leftOff = axisPoint(left);
            float midOff = axisPoint(mid);
            float rightOff = axisPoint(right);
            float off = (midOff - leftOff) / (rightOff - leftOff);

            //writefln("interp %d %d %d %d -> %f %f %f %f", maj, left, mid, right,
            //leftOff, midOff, rightOff, off);
            return get(maj, left) * (1 - off) + get(maj, right) * off;
        }

        void interpolate1D2D(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            foreach (i; 0..majorCnt()) {
                uint l = 0;
                uint cnt = minorCnt();

                // Find first element set
                for (; l < cnt && !isValid(i, l); l++) { }

                // Empty row, we're done
                if (l >= cnt) continue;

                while (true)
                {
                    // Advance until before a missing element
                    for (; l < cnt - 1 && isValid(i, l + 1); l++) { }

                    // Reached right side, done
                    if (l >= (cnt - 1)) break;

                    // Find next set element
                    uint r = l + 1;
                    for (; r < cnt && !isValid(i, r); r++) { }

                    // If we ran off the edge, we're done
                    if (r >= cnt) break;

                    // Interpolate between the pair of valid elements
                    foreach (m; (l + 1)..r) {
                        T val = interp(i, l, m, r);

                        // If we're running the second stage of intersecting 1D interpolation
                        if (secondPass && isNewlySet(i, m))
                        {
                            // Found an intersection, do not commit the previous points
                            if (!detectedIntersections)
                            {
                                //debug writefln("Intersection at %d, %d", i, m);
                                commitPoints.length = 0;
                            }
                            // Average out the point at the intersection
                            set(i, m, (val + get(i, m)) * 0.5f);
                            // From now on we're only computing intersection points
                            detectedIntersections = true;
                        }
                        // If we've found no intersections so far, continue with normal
                        // 1D interpolation.
                        if (!detectedIntersections)
                            set(i, m, val);
                    }

                    // Look for the next pair
                    l = r;
                }
            }
        }

        void extrapolateCorners()
        {
            if (yCount <= 1 || xCount <= 1) return;

            void extrapolateCorner(uint baseX, uint baseY, uint offX, uint offY)
            {
                T base = values[baseX][baseY];
                T dX = values[baseX + offX][baseY] + (base * -1f);
                T dY = values[baseX][baseY + offY] + (base * -1f);
                values[baseX + offX][baseY + offY] = base + dX + dY;
                commitPoints ~= vec2u(baseX + offX, baseY + offY);
            }

            foreach (x; 0..xCount - 1) {
                foreach (y; 0..yCount - 1) {
                    if (valid[x][y] && valid[x + 1][y] && valid[x][y + 1] && !valid[x + 1][y + 1])
                        extrapolateCorner(x, y, 1, 1);
                    else if (valid[x][y] && valid[x + 1][y] && !valid[x][y + 1] && valid[x + 1][y + 1])
                        extrapolateCorner(x + 1, y, -1, 1);
                    else if (valid[x][y] && !valid[x + 1][y] && valid[x][y + 1] && valid[x + 1][y + 1])
                        extrapolateCorner(x, y + 1, 1, -1);
                    else if (!valid[x][y] && valid[x + 1][y] && valid[x][y + 1] && valid[x + 1][y + 1])
                        extrapolateCorner(x + 1, y + 1, -1, -1);
                }
            }
        }

        void extendAndIntersect(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            void setOrAverage(uint maj, uint min, T val, float origin)
            {
                float minDist = abs(axisPoint(min) - origin);
                // Same logic as in interpolate1D2D
                if (secondPass && isNewlySet(maj, min))
                {
                    // Found an intersection, do not commit the previous points
                    if (!detectedIntersections)
                    {
                        commitPoints.length = 0;
                    }
                    float majDist = getDistance(maj, min);
                    float frac = minDist / (minDist + majDist * majDist / minDist);
                    // Interpolate the point at the intersection
                    set(maj, min, val * (1 - frac) + get(maj, min) * frac);
                    // From now on we're only computing intersection points
                    detectedIntersections = true;
                }
                // If we've found no intersections so far, continue with normal
                // 1D extension.
                if (!detectedIntersections)
                {
                    set(maj, min, val, minDist);
                }
            }

            foreach (i; 0..majorCnt()) {
                uint j;
                uint cnt = minorCnt();

                // Find first element set
                for (j = 0; j < cnt && !isValid(i, j); j++) { }

                // Empty row, we're done
                if (j >= cnt) continue;

                // Replicate leftwards
                T val = get(i, j);
                float origin = axisPoint(j);
                foreach (k; 0..j)
                    setOrAverage(i, k, val, origin);

                // Find last element set
                for (j = cnt - 1; j < cnt && !isValid(i, j); j--) { }

                // Replicate rightwards
                val = get(i, j);
                origin = axisPoint(j);
                foreach (k; (j + 1)..cnt)
                    setOrAverage(i, k, val, origin);
            }
        }

        while (true)
        {
            foreach (i; commitPoints) {
                assert(!valid[i.x][i.y], "trying to double-set a point");
                valid[i.x][i.y] = true;
                validCount++;
            }
            commitPoints.length = 0;

            // Are we done?
            if (validCount == totalCount) break;

            // Reset the newlySet array
            foreach (x; 0..xCount) {
                newlySet[x].length = 0;
                newlySet[x].length = yCount;
            }

            // Try 1D interpolation in the X-Major direction
            interpolate1D2D(false);
            // Try 1D interpolation in the Y-Major direction, with intersection detection
            // If this finds an intersection with the above, it will fall back to
            // computing *only* the intersecting points as the average of the interpolated values.
            // If that happens, the next loop will re-try normal 1D interpolation.
            interpolate1D2D(true);
            // Did we get work done? If so, commit and loop
            if (commitPoints.length > 0) continue;

            // Now try corner extrapolation
            extrapolateCorners();
            // Did we get work done? If so, commit and loop
            if (commitPoints.length > 0) continue;

            // Running out of options. Expand out points in both axes outwards, but if
            // two expansions intersect then compute the average and commit only intersections.
            // This works like interpolate1D2D, in two passes, one per axis, changing behavior
            // once an intersection is detected.
            extendAndIntersect(false);
            extendAndIntersect(true);
            // Did we get work done? If so, commit and loop
            if (commitPoints.length > 0) continue;

            // Should never happen
            break;
        }

        // The above algorithm should be guaranteed to succeed in all cases.
        enforce(validCount == totalCount, "Interpolation failed to complete");
    }

    T interpolate(vec2u leftKeypoint, vec2 offset)
    {
        switch (interpolateMode_)
        {
            case InterpolateMode.Nearest:
                return interpolateNearest(leftKeypoint, offset);
            case InterpolateMode.Linear:
                return interpolateLinear(leftKeypoint, offset);
            case InterpolateMode.Cubic:
                return interpolateCubic(leftKeypoint, offset);
            default: assert(0);
        }
    }

    T interpolateNearest(vec2u leftKeypoint, vec2 offset)
    {
        ulong px = leftKeypoint.x + ((offset.x >= 0.5) ? 1 : 0);
        if (parameter.isVec2)
        {
            ulong py = leftKeypoint.y + ((offset.y >= 0.5) ? 1 : 0);
            return values[px][py];
        }
        else
        {
            return values[px][0];
        }
    }

    T interpolateLinear(vec2u leftKeypoint, vec2 offset)
    {
        T p0, p1;

        if (parameter.isVec2)
        {
            T p00 = values[leftKeypoint.x][leftKeypoint.y];
            T p01 = values[leftKeypoint.x][leftKeypoint.y + 1];
            T p10 = values[leftKeypoint.x + 1][leftKeypoint.y];
            T p11 = values[leftKeypoint.x + 1][leftKeypoint.y + 1];
            p0 = p00.lerp(p01, offset.y);
            p1 = p10.lerp(p11, offset.y);
        }
        else
        {
            p0 = values[leftKeypoint.x][0];
            p1 = values[leftKeypoint.x + 1][0];
        }

        return p0.lerp(p1, offset.x);
    }

    T interpolateCubic(vec2u leftKeypoint, vec2 offset)
    {
        T p0, p1, p2, p3;

        T bicubicInterp(vec2u left, float xt, float yt)
        {
            T p01, p02, p03, p04;
            T[4] pOut;

            size_t xlen = values.length - 1;
            size_t ylen = values[0].length - 1;
            ptrdiff_t xkp = cast(ptrdiff_t)leftKeypoint.x;
            ptrdiff_t ykp = cast(ptrdiff_t)leftKeypoint.y;

            foreach (y; 0..4) {
                size_t yp = clamp(ykp + y - 1, 0, ylen);

                p01 = values[max(xkp - 1, 0)][yp];
                p02 = values[xkp][yp];
                p03 = values[xkp + 1][yp];
                p04 = values[min(xkp + 2, xlen)][yp];
                pOut[y] = cubic(p01, p02, p03, p04, xt);
            }

            return cubic(pOut[0], pOut[1], pOut[2], pOut[3], yt);
        }

        if (parameter.isVec2)
        {
            return bicubicInterp(leftKeypoint, offset.x, offset.y);
        }
        else
        {
            ptrdiff_t xkp = cast(ptrdiff_t)leftKeypoint.x;
            size_t xlen = values.length - 1;

            p0 = values[max(xkp - 1, 0)][0];
            p1 = values[xkp][0];
            p2 = values[xkp + 1][0];
            p3 = values[min(xkp + 2, xlen)][0];
            return cubic(p0, p1, p2, p3, offset.x);
        }
    }

    override
    void apply(vec2u leftKeypoint, vec2 offset)
    {
        applyToTarget(interpolate(leftKeypoint, offset));
    }

    override
    void insertKeypoints(uint axis, uint index)
    {
        assert(axis == 0 || axis == 1);

        if (axis == 0)
        {
            uint yCount = parameter.axisPointCount(1);

            values.insertInPlace(index, cast(T[])[]);
            values[index].length = yCount;
            isSet_.insertInPlace(index, cast(bool[])[]);
            isSet_[index].length = yCount;
        }
        else if (axis == 1)
        {
            foreach (ref i; this.values) {
                i.insertInPlace(index, T.init);
            }
            foreach (ref i; this.isSet_) {
                i.insertInPlace(index, false);
            }
        }

        reInterpolate();
    }

    override
    void moveKeypoints(uint axis, uint oldindex, uint newindex)
    {
        assert(axis == 0 || axis == 1);

        if (axis == 0)
        {
            {
                auto swap = values[oldindex];
                values = values.remove(oldindex);
                values.insertInPlace(newindex, swap);
            }

            {
                auto swap = isSet_[oldindex];
                isSet_ = isSet_.remove(oldindex);
                isSet_.insertInPlace(newindex, swap);
            }
        }
        else if (axis == 1)
        {
            foreach (ref i; this.values) {
                {
                    auto swap = i[oldindex];
                    i = i.remove(oldindex);
                    i.insertInPlace(newindex, swap);
                }
            }
            foreach (i; this.isSet_) {
                {
                    auto swap = i[oldindex];
                    i = i.remove(oldindex);
                    i.insertInPlace(newindex, swap);
                }
            }
        }

        reInterpolate();
    }

    override
    void deleteKeypoints(uint axis, uint index)
    {
        assert(axis == 0 || axis == 1);

        if (axis == 0)
        {
            values = values.remove(index);
            isSet_ = isSet_.remove(index);
        }
        else if (axis == 1)
        {
            foreach (i; 0..this.values.length) {
                values[i] = values[i].remove(index);
            }
            foreach (i; 0..this.isSet_.length) {
                isSet_[i] = isSet_[i].remove(index);
            }
        }

        reInterpolate();
    }

    override void scaleValueAt(vec2u index, int axis, float scale)
    {
        /* Default to just scalar scale */
        setValue(index, getValue(index) * scale);
    }

    override void extrapolateValueAt(vec2u index, int axis)
    {
        vec2 offset = parameter.getKeypointOffset(index);

        switch (axis)
        {
            case -1: offset = vec2(1, 1) - offset; break;
            case 0: offset.x = 1 - offset.x; break;
            case 1: offset.y = 1 - offset.y; break;
            default: assert(false, "bad axis");
        }

        vec2u srcIndex;
        vec2 subOffset;
        parameter.findOffset(offset, srcIndex, subOffset);

        T srcVal = interpolate(srcIndex, subOffset);

        setValue(index, srcVal);
        scaleValueAt(index, axis, -1);
    }

    override void copyKeypointToBinding(vec2u src, ParameterBinding other, vec2u dest)
    {
        if (!isSet(src))
        {
            other.unset(dest);
        }
        else if (auto o = cast(ParameterBindingImpl!T)(other)) {
            o.setValue(dest, getValue(src));
        } else
        {
            assert(false, "ParameterBinding class mismatch");
        }
    }

    override void swapKeypointWithBinding(vec2u src, ParameterBinding other, vec2u dest)
    {
        if (auto o = cast(ParameterBindingImpl!T)(other)) {
            bool thisSet = isSet(src);
            bool otherSet = other.isSet(dest);
            T thisVal = getValue(src);
            T otherVal = o.getValue(dest);

            // Swap directly, to avoid clobbering by update
            o.values[dest.x][dest.y] = thisVal;
            o.isSet_[dest.x][dest.y] = thisSet;
            values[src.x][src.y] = otherVal;
            isSet_[src.x][src.y] = otherSet;

            reInterpolate();
            o.reInterpolate();
        } else
        {
            assert(false, "ParameterBinding class mismatch");
        }
    }

    /**
        Get the interpolation mode
    */
    override InterpolateMode interpolateMode()
    {
        return interpolateMode_;
    }

    /**
        Set the interpolation mode
    */
    override void interpolateMode(InterpolateMode mode)
    {
        interpolateMode_ = mode;
    }

    /**
        Apply parameter to target node
    */
    abstract void applyToTarget(T value);
}
