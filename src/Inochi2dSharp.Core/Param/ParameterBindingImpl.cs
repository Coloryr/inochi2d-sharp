using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp.Core.Param;

/// <summary>
/// A binding to a parameter, of a given value type
/// </summary>
/// <typeparam name="T"></typeparam>
public abstract class ParameterBindingImpl<T> : ParameterBinding
{
    /// <summary>
    /// Node reference (for deserialization)
    /// </summary>
    private Guid nodeRef;

    /// <summary>
    /// Parent Parameter owning this binding
    /// </summary>
    public Parameter Parameter;

    /// <summary>
    /// Reference to what parameter we're binding to
    /// </summary>
    public readonly BindTarget Target = new();

    /// <summary>
    /// The value at each 2D keypoint
    /// </summary>
    public T[][] Values;

    /// <summary>
    /// Whether the value at each 2D keypoint is user-set
    /// </summary>
    public bool[][] IsSet;

    public ParameterBindingImpl(Parameter parameter)
    {
        Parameter = parameter;
    }

    public ParameterBindingImpl(Parameter parameter, Node targetNode, string paramName)
    {
        Parameter = parameter;
        Target = new()
        {
            Node = targetNode,
            ParamName = paramName
        };

        Clear();
    }

    public abstract void SerializeItem(T item, JsonArray data);
    public abstract T DeserializeItem(JsonElement data);
    public abstract T Multiply(T value, float value1);
    public abstract T Add(T value, T value1);
    public abstract T Add(T value, T value1, T value2);
    public abstract T Lerp(T value, T value1, float value2);
    public abstract T Cubic(T value, T value1, T value2, T value3, float value4);
    /// <summary>
    /// Apply parameter to target node
    /// </summary>
    /// <param name="value"></param>
    public abstract void ApplyToTarget(T value);

    /// <summary>
    /// Serializes a binding
    /// </summary>
    /// <param name="data"></param>
    public override void Serialize(JsonObject data)
    {
        data["node"] = Target.Node.Guid.ToString();
        data["param_name"] = Target.ParamName;

        var list = new JsonArray();
        foreach (var item in Values)
        {
            var list1 = new JsonArray();
            foreach (var item1 in item)
            {
                SerializeItem(item1, list1);
            }
            list.Add(list1);
        }

        data["values"] = list;
        data["isSet"] = IsSet.ToToken();
        data["interpolate_mode"] = InterpolateMode.ToString();
    }

    /// <summary>
    /// Deserializes a binding
    /// </summary>
    /// <param name="data"></param>
    public override void Deserialize(JsonElement data)
    {
        nodeRef = data.GetGuid("node", "target");

        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "param_name" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Target.ParamName = item.Value.GetString()!;
            }
            else if (item.Name == "isSet" && item.Value.ValueKind == JsonValueKind.Array)
            {
                IsSet = item.Value.ToArray<bool>();
            }
            else if (item.Name == "values" && item.Value.ValueKind == JsonValueKind.Array)
            {
                var temp = new List<List<T>>();
                foreach (JsonElement item1 in item.Value.EnumerateArray())
                {
                    var list = new List<T>();
                    foreach (JsonElement item2 in item1.EnumerateArray())
                    {
                        list.Add(DeserializeItem(item2));
                    }
                    temp.Add(list);
                }
                Values = [.. temp.Select(item => item.ToArray())];
            }
            else if (item.Name == "interpolate_mode" && item.Value.ValueKind != JsonValueKind.Null)
            {
                InterpolateMode = item.Value.GetString()!.ToInterpolateMode();
            }
        }

        int xCount = Parameter.AxisPointCount(0);
        int yCount = Parameter.AxisPointCount(1);

        if (Values.Length != xCount)
        {
            throw new Exception("Mismatched X value count");
        }
        foreach (var i in Values)
        {
            if (i.Length != yCount)
            {
                throw new Exception("Mismatched Y value count");
            }
        }

        if (IsSet.Length != xCount)
        {
            throw new Exception("Mismatched X isSet_ count");
        }
        foreach (var i in IsSet)
        {
            if (i.Length != yCount)
            {
                throw new Exception("Mismatched Y isSet_ count");
            }
        }
    }

    /// <summary>
    /// Gets target of binding
    /// </summary>
    /// <returns></returns>
    public override BindTarget GetTarget()
    {
        return Target;
    }

    /// <summary>
    /// Gets name of binding
    /// </summary>
    /// <returns></returns>
    public override string GetName()
    {
        return Target.ParamName;
    }

    /// <summary>
    /// Gets the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Node GetNode()
    {
        return Target.Node;
    }

    /// <summary>
    /// Gets the uuid of the node of the binding
    /// </summary>
    /// <returns></returns>
    public override Guid GetNodeUUID()
    {
        return nodeRef;
    }

    /// <summary>
    /// Returns isSet_
    /// </summary>
    /// <returns></returns>
    public override bool[][] GetIsSet()
    {
        return IsSet;
    }

    /// <summary>
    /// Gets how many breakpoints this binding is set to
    /// </summary>
    /// <returns></returns>
    public override uint GetSetCount()
    {
        uint count = 0;
        for (int x = 0; x < IsSet.Length; x++)
        {
            for (int y = 0; y < IsSet[x].Length; y++)
            {
                if (IsSet[x][y]) count++;
            }
        }
        return count;
    }

    /// <summary>
    /// Finalize loading of parameter
    /// </summary>
    /// <param name="puppet"></param>
    public override void Finalize(Puppet puppet)
    {
        Target.Node = puppet.Find<Node>(nodeRef)!;
    }

    public override void Reconstruct(Puppet puppet)
    {

    }

    public abstract void ClearValue(ref T i);

    /// <summary>
    /// Clear all keypoint data
    /// </summary>
    public override void Clear()
    {
        uint xCount = (uint)Parameter.AxisPointCount(0);
        uint yCount = (uint)Parameter.AxisPointCount(1);

        Values = new T[xCount][];
        IsSet = new bool[xCount][];
        for (int x = 0; x < xCount; x++)
        {
            IsSet[x] = new bool[yCount];

            Values[x] = new T[yCount];
            for (int y = 0; y < yCount; y++)
            {
                ClearValue(ref Values[x][y]);
            }
        }
    }

    /// <summary>
    /// Gets the value at the specified point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public T GetValue(Vector2UInt point)
    {
        return Values[point.X][point.Y];
    }

    /// <summary>
    /// Sets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    /// <param name="value"></param>
    public void SetValue(Vector2UInt point, T value)
    {
        Values[point.X][point.Y] = value;
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Sets value at specified keypoint to the current value
    /// </summary>
    /// <param name="point"></param>
    public override void SetCurrent(Vector2UInt point)
    {
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public override void Unset(Vector2UInt point)
    {
        ClearValue(ref Values[point.X][point.Y]);
        IsSet[point.X][point.Y] = false;

        ReInterpolate();
    }

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public override void Reset(Vector2UInt point)
    {
        ClearValue(ref Values[point.X][point.Y]);
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Returns whether the specified keypoint is set
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public override bool GetIsSet(Vector2UInt index)
    {
        return IsSet[index.X][index.Y];
    }

    /// <summary>
    /// Flip the keypoints on an axis
    /// </summary>
    /// <param name="axis"></param>
    public override void ReverseAxis(uint axis)
    {
        if (axis == 0)
        {
            Array.Reverse(Values);
            Array.Reverse(IsSet);
        }
        else
        {
            foreach (var item in Values)
            {
                Array.Reverse(item);
            }
            foreach (var item in IsSet)
            {
                Array.Reverse(item);
            }
        }
    }

    /// <summary>
    /// Re-calculate interpolation
    /// </summary>
    public override void ReInterpolate()
    {
        uint xCount = (uint)Parameter.AxisPointCount(0);
        uint yCount = (uint)Parameter.AxisPointCount(1);

        // Currently valid points
        bool[][] valid = new bool[xCount][];
        uint validCount = 0;
        uint totalCount = xCount * yCount;

        // Initialize validity map to user-set points
        for (int x = 0; x < xCount; x++)
        {
            valid[x] = [.. IsSet[x]];
            for (int y = 0; y < yCount; y++)
            {
                if (IsSet[x][y])
                {
                    validCount++;
                }
            }
        }

        // If there are zero valid points, just clear ourselves
        if (validCount == 0)
        {
            Clear();
            return;
        }

        // Whether any given point was just set
        bool[][] newlySet = new bool[xCount][];

        // List of indices to commit
        var commitPoints = new List<Vector2UInt>();

        // Used by extendAndIntersect for x/y factor
        float[][] interpDistance = new float[xCount][];
        for (int x = 0; x < xCount; x++)
        {
            interpDistance[x] = new float[yCount];
        }

        // Current interpolation axis
        bool yMajor = false;

        // Helpers to handle interpolation across both axes more easily
        uint MajorCnt()
        {
            if (yMajor)
                return yCount;
            else
                return xCount;
        }

        uint MinorCnt()
        {
            if (yMajor)
                return xCount;
            else
                return yCount;
        }

        bool IsValid(uint maj, uint min)
        {
            if (yMajor)
                return valid[min][maj];
            else
                return valid[maj][min];
        }

        bool IsNewlySet(uint maj, uint min)
        {
            if (yMajor)
                return newlySet[min][maj];
            else
                return newlySet[maj][min];
        }

        T Get(uint maj, uint min)
        {
            if (yMajor)
                return Values[min][maj];
            else
                return Values[maj][min];
        }

        float GetDistance(uint maj, uint min)
        {
            if (yMajor)
                return interpDistance[min][maj];
            else
                return interpDistance[maj][min];
        }

        void Reset(uint maj, uint min, T val, float distance = 0)
        {
            if (yMajor)
            {
                //debug writefln("set (%d, %d) -> %s", min, maj, val);
                if (valid[min][maj])
                {
                    throw new Exception("valid error");
                }
                Values[min][maj] = val;
                interpDistance[min][maj] = distance;
                newlySet[min][maj] = true;
            }
            else
            {
                //debug writefln("set (%d, %d) -> %s", maj, min, val);
                if (valid[maj][min])
                {
                    throw new Exception("valid error");
                }
                Values[maj][min] = val;
                interpDistance[maj][min] = distance;
                newlySet[maj][min] = true;
            }
        }

        void Set(uint maj, uint min, T val, float distance = 0)
        {
            Reset(maj, min, val, distance);
            if (yMajor)
                commitPoints.Add(new(min, maj));
            else
                commitPoints.Add(new(maj, min));
        }

        float AxisPoint(uint idx)
        {
            if (yMajor)
                return Parameter.AxisPoints[0][idx];
            else
                return Parameter.AxisPoints[1][idx];
        }

        T Interp(uint maj, uint left, uint mid, uint right)
        {
            float leftOff = AxisPoint(left);
            float midOff = AxisPoint(mid);
            float rightOff = AxisPoint(right);
            float off = (midOff - leftOff) / (rightOff - leftOff);

            //writefln("interp %d %d %d %d -> %f %f %f %f", maj, left, mid, right,
            //leftOff, midOff, rightOff, off);
            return Add(Multiply(Get(maj, left), 1 - off), Multiply(Get(maj, right), off));
        }

        void Interpolate1D2D(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            for (uint i = 0; i < MajorCnt(); i++)
            {
                uint l = 0;
                uint cnt = MinorCnt();

                // Find first element set
                for (; l < cnt && !IsValid(i, l); l++) { }

                // Empty row, we're done
                if (l >= cnt)
                    continue;

                while (true)
                {
                    // Advance until before a missing element
                    for (; l < cnt - 1 && IsValid(i, l + 1); l++) { }

                    // Reached right side, done
                    if (l >= (cnt - 1))
                        break;

                    // Find next set element
                    uint r = l + 1;
                    for (; r < cnt && !IsValid(i, r); r++) { }

                    // If we ran off the edge, we're done
                    if (r >= cnt)
                        break;

                    // Interpolate between the pair of valid elements
                    for (uint m = l + 1; m < r; m++)
                    {
                        T val = Interp(i, l, m, r);

                        // If we're running the second stage of intersecting 1D interpolation
                        if (secondPass && IsNewlySet(i, m))
                        {
                            // Found an intersection, do not commit the previous points
                            if (!detectedIntersections)
                            {
                                //debug writefln("Intersection at %d, %d", i, m);
                                commitPoints.Clear();
                            }
                            // Average out the point at the intersection
                            Set(i, m, Multiply(Add(val, Get(i, m)), 0.5f));
                            // From now on we're only computing intersection points
                            detectedIntersections = true;
                        }
                        // If we've found no intersections so far, continue with normal
                        // 1D interpolation.
                        if (!detectedIntersections)
                            Set(i, m, val);
                    }

                    // Look for the next pair
                    l = r;
                }
            }
        }

        void ExtrapolateCorners()
        {
            if (yCount <= 1 || xCount <= 1)
                return;

            void ExtrapolateCorner(int baseX, int baseY, int offX, int offY)
            {
                T base1 = Values[baseX][baseY];
                T temp = Multiply(base1, -1f);
                T dX = Add(Values[baseX + offX][baseY], temp);
                T dY = Add(Values[baseX][baseY + offY], temp);
                Values[baseX + offX][baseY + offY] = Add(base1, dX, dY);
                commitPoints.Add(new((uint)(baseX + offX), (uint)(baseY + offY)));
            }

            for (int x = 0; x < xCount - 1; x++)
            {
                for (int y = 0; y < yCount - 1; y++)
                {
                    if (valid[x][y] && valid[x + 1][y] && valid[x][y + 1] && !valid[x + 1][y + 1])
                        ExtrapolateCorner(x, y, 1, 1);
                    else if (valid[x][y] && valid[x + 1][y] && !valid[x][y + 1] && valid[x + 1][y + 1])
                        ExtrapolateCorner(x + 1, y, -1, 1);
                    else if (valid[x][y] && !valid[x + 1][y] && valid[x][y + 1] && valid[x + 1][y + 1])
                        ExtrapolateCorner(x, y + 1, 1, -1);
                    else if (!valid[x][y] && valid[x + 1][y] && valid[x][y + 1] && valid[x + 1][y + 1])
                        ExtrapolateCorner(x + 1, y + 1, -1, -1);
                }
            }
        }

        void extendAndIntersect(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            void SetOrAverage(uint maj, uint min, T val, float origin)
            {
                float minDist = float.Abs(AxisPoint(min) - origin);
                // Same logic as in interpolate1D2D
                if (secondPass && IsNewlySet(maj, min))
                {
                    // Found an intersection, do not commit the previous points
                    if (!detectedIntersections)
                    {
                        commitPoints.Clear();
                    }
                    float majDist = GetDistance(maj, min);
                    float frac = minDist / (minDist + majDist * majDist / minDist);
                    // Interpolate the point at the intersection
                    Set(maj, min, Add(Multiply(val, 1 - frac), Multiply(Get(maj, min), frac)));
                    // From now on we're only computing intersection points
                    detectedIntersections = true;
                }
                // If we've found no intersections so far, continue with normal
                // 1D extension.
                if (!detectedIntersections)
                {
                    Set(maj, min, val, minDist);
                }
            }

            for (uint i = 0; i < MajorCnt(); i++)
            {
                uint j;
                uint cnt = MinorCnt();

                // Find first element set
                for (j = 0; j < cnt && !IsValid(i, j); j++)
                {
                }

                // Empty row, we're done
                if (j >= cnt)
                    continue;

                // Replicate leftwards
                T val = Get(i, j);
                float origin = AxisPoint(j);
                for (uint k = 0; k < j; k++)
                {
                    SetOrAverage(i, k, val, origin);
                }

                // Find last element set
                for (j = cnt - 1; j < cnt && !IsValid(i, j); j--)
                {
                }

                // Replicate rightwards
                val = Get(i, j);
                origin = AxisPoint(j);
                for (uint k = j + 1; k < cnt; k++)
                {
                    SetOrAverage(i, k, val, origin);
                }
            }
        }

        while (true)
        {
            foreach (var i in commitPoints)
            {
                if (valid[i.X][i.Y])
                {
                    throw new Exception("trying to double-set a point");
                }
                valid[i.X][i.Y] = true;
                validCount++;
            }
            commitPoints.Clear();

            // Are we done?
            if (validCount == totalCount)
                break;

            // Reset the newlySet array
            for (int x = 0; x < xCount; x++)
            {
                newlySet[x] = new bool[yCount];
            }

            // Try 1D interpolation in the X-Major direction
            Interpolate1D2D(false);
            // Try 1D interpolation in the Y-Major direction, with intersection detection
            // If this finds an intersection with the above, it will fall back to
            // computing *only* the intersecting points as the average of the interpolated values.
            // If that happens, the next loop will re-try normal 1D interpolation.
            Interpolate1D2D(true);
            // Did we get work done? If so, commit and loop
            if (commitPoints.Count > 0) continue;

            // Now try corner extrapolation
            ExtrapolateCorners();
            // Did we get work done? If so, commit and loop
            if (commitPoints.Count > 0) continue;

            // Running out of options. Expand out points in both axes outwards, but if
            // two expansions intersect then compute the average and commit only intersections.
            // This works like interpolate1D2D, in two passes, one per axis, changing behavior
            // once an intersection is detected.
            extendAndIntersect(false);
            extendAndIntersect(true);
            // Did we get work done? If so, commit and loop
            if (commitPoints.Count > 0) continue;

            // Should never happen
            break;
        }

        // The above algorithm should be guaranteed to succeed in all cases.
        if (validCount != totalCount)
        {
            throw new Exception("Interpolation failed to complete");
        }
    }

    public T Interpolate(Vector2UInt leftKeypoint, Vector2 offset)
    {
        return InterpolateMode switch
        {
            InterpolateMode.Nearest => InterpolateNearest(leftKeypoint, offset),
            InterpolateMode.Linear => InterpolateLinear(leftKeypoint, offset),
            InterpolateMode.Cubic => InterpolateCubic(leftKeypoint, offset),
            _ => throw new Exception("out of range"),
        };
    }

    public T InterpolateNearest(Vector2UInt leftKeypoint, Vector2 offset)
    {
        var px = leftKeypoint.X + ((offset.X >= 0.5) ? 1 : 0);
        if (Parameter.IsVec2)
        {
            var py = leftKeypoint.Y + ((offset.Y >= 0.5) ? 1 : 0);
            return Values[px][py];
        }
        else
        {
            return Values[px][0];
        }
    }

    public T InterpolateLinear(Vector2UInt leftKeypoint, Vector2 offset)
    {
        T p0, p1;

        if (Parameter.IsVec2)
        {
            var p00 = Values[leftKeypoint.X][leftKeypoint.Y];
            var p01 = Values[leftKeypoint.X][leftKeypoint.Y + 1];
            var p10 = Values[leftKeypoint.X + 1][leftKeypoint.Y];
            var p11 = Values[leftKeypoint.X + 1][leftKeypoint.Y + 1];
            p0 = Lerp(p00, p01, offset.Y);
            p1 = Lerp(p10, p11, offset.Y);
        }
        else
        {
            p0 = Values[leftKeypoint.X][0];
            p1 = Values[leftKeypoint.X + 1][0];
        }

        return Lerp(p0, p1, offset.X);
    }

    public T InterpolateCubic(Vector2UInt leftKeypoint, Vector2 offset)
    {
        T p0, p1, p2, p3;

        T bicubicInterp(Vector2UInt left, float xt, float yt)
        {
            T p01, p02, p03, p04;
            T[] pOut = new T[4];

            var xlen = (uint)Values.Length - 1;
            var ylen = Values[0].Length - 1;
            var xkp = leftKeypoint.X;
            var ykp = leftKeypoint.Y;

            for (int y = 0; y < 4; y++)
            {
                var yp = float.Clamp(ykp + y - 1, 0, ylen);

                p01 = Values[uint.Max(xkp - 1, 0)][(int)yp];
                p02 = Values[xkp][(int)yp];
                p03 = Values[xkp + 1][(int)yp];
                p04 = Values[uint.Min(xkp + 2, xlen)][(int)yp];
                pOut[y] = Cubic(p01, p02, p03, p04, xt);
            }

            return Cubic(pOut[0], pOut[1], pOut[2], pOut[3], yt);
        }

        if (Parameter.IsVec2)
        {
            return bicubicInterp(leftKeypoint, offset.X, offset.Y);
        }
        else
        {
            var xkp = leftKeypoint.X;
            var xlen = (uint)Values.Length - 1;

            p0 = Values[uint.Max(xkp - 1, 0)][0];
            p1 = Values[xkp][0];
            p2 = Values[xkp + 1][0];
            p3 = Values[uint.Min(xkp + 2, xlen)][0];
            return Cubic(p0, p1, p2, p3, offset.X);
        }
    }

    public override void Apply(Vector2UInt leftKeypoint, Vector2 offset)
    {
        ApplyToTarget(Interpolate(leftKeypoint, offset));
    }

    public override void InsertKeypoints(uint axis, uint index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            int yCount = Parameter.AxisPointCount(1);

            var temp = new List<T[]>(Values);
            temp.Insert((int)index, new T[yCount]);
            Values = [.. temp];

            var temp1 = new List<bool[]>(IsSet);
            temp1.Insert((int)index, new bool[yCount]);
            IsSet = [.. temp1];

            for (int i = 0; i < yCount; i++)
            {
                IsSet[index][i] = false;
                Values[index][i] = default!;
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                var item = new List<T>(Values[i]);
                item.Insert((int)index, default!);
                Values[i] = [.. item];
            }
            for (int i = 0; i < IsSet.Length; i++)
            {
                var item = new List<bool>(IsSet[i]);
                item.Insert((int)index, false);
                IsSet[i] = [.. item];
            }
        }

        ReInterpolate();
    }

    public override void MoveKeypoints(uint axis, uint oldindex, uint newindex)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            (Values[newindex], Values[oldindex]) = (Values[oldindex], Values[newindex]);
            (IsSet[newindex], IsSet[oldindex]) = (IsSet[oldindex], IsSet[newindex]);
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                (Values[i][oldindex], Values[i][newindex]) = (Values[i][newindex], Values[i][oldindex]);
            }
            for (int i = 0; i < IsSet.Length; i++)
            {
                (IsSet[i][oldindex], IsSet[i][newindex]) = (IsSet[i][newindex], IsSet[i][oldindex]);
            }
        }

        ReInterpolate();
    }

    public override void DeleteKeypoints(uint axis, uint index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            var temp = new List<T[]>(Values);
            temp.RemoveAt((int)index);
            Values = [.. temp];

            var temp1 = new List<bool[]>(IsSet);
            temp1.RemoveAt((int)index);
            IsSet = [.. temp1];
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Length; i++)
            {
                var temp = new List<T>(Values[i]);
                temp.RemoveAt((int)index);
                Values[i] = [.. temp];
            }
            for (int i = 0; i < IsSet.Length; i++)
            {
                var temp = new List<bool>(IsSet[i]);
                temp.RemoveAt((int)index);
                IsSet[i] = [.. temp];
            }
        }

        ReInterpolate();
    }

    public override void ScaleValueAt(Vector2UInt index, int axis, float scale)
    {
        /* Default to just scalar scale */
        SetValue(index, Multiply(GetValue(index), scale));
    }

    public override void ExtrapolateValueAt(Vector2UInt index, int axis)
    {
        var offset = Parameter.GetKeypointOffset(index);

        switch (axis)
        {
            case -1: offset = new Vector2(1, 1) - offset; break;
            case 0: offset.X = 1 - offset.X; break;
            case 1: offset.Y = 1 - offset.Y; break;
            default: throw new Exception("bad axis");
        }

        Parameter.FindOffset(offset, out var srcIndex, out var subOffset);

        var srcVal = Interpolate(srcIndex, subOffset);

        SetValue(index, srcVal);
        ScaleValueAt(index, axis, -1);
    }

    public override void CopyKeypointToBinding(Vector2UInt src, ParameterBinding other, Vector2UInt dest)
    {
        if (!GetIsSet(src))
        {
            other.Unset(dest);
        }
        else if (other is ParameterBindingImpl<T> o)
        {
            o.SetValue(dest, GetValue(src));
        }
        else
        {
            throw new Exception("ParameterBinding class mismatch");
        }
    }

    public override void SwapKeypointWithBinding(Vector2UInt src, ParameterBinding other, Vector2UInt dest)
    {
        if (other is ParameterBindingImpl<T> o)
        {
            bool thisSet = GetIsSet(src);
            bool otherSet = other.GetIsSet(dest);
            T thisVal = GetValue(src);
            T otherVal = o.GetValue(dest);

            // Swap directly, to avoid clobbering by update
            o.Values[dest.X][dest.Y] = thisVal;
            o.IsSet[dest.X][dest.Y] = thisSet;
            Values[src.X][src.Y] = otherVal;
            IsSet[src.X][src.Y] = otherSet;

            ReInterpolate();
            o.ReInterpolate();
        }
        else
        {
            throw new Exception("ParameterBinding class mismatch");
        }
    }
}
