using System.Numerics;
using System.Text.Json.Nodes;
using Inochi2dSharp;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.Core.Param;

public class DeformationParameterBinding : ParameterBindingImpl
{
    /// <summary>
    /// The value at each 2D keypoint
    /// </summary>
    public List<List<Deformation>> Values = [];

    public DeformationParameterBinding(Parameter parameter) : base(parameter)
    {

    }

    public DeformationParameterBinding(Parameter parameter, Node targetNode, string paramName) : base(parameter, targetNode, paramName)
    {

    }

    /// <summary>
    /// Serializes a binding
    /// </summary>
    /// <param name="serializer"></param>
    public override void Serialize(JsonObject serializer)
    {
        serializer.Add("node", Target.node.UUID);
        serializer.Add("param_name", Target.paramName);
        var list = new JsonArray();
        foreach (var item in Values)
        {
            var list1 = new JsonArray();
            foreach (var item1 in item)
            {
                var obj = new JsonArray();
                item1.Serialize(obj);
                list1.Add(obj);
            }
            list.Add(list1);
        }
        serializer.Add("values", list);
        serializer.Add("isSet", IsSet.ToToken());
        serializer.Add("interpolate_mode", InterpolateMode.ToString());
    }

    /// <summary>
    /// Deserializes a binding
    /// </summary>
    /// <param name="data"></param>
    public override void Deserialize(JsonObject data)
    {
        if (data.TryGetPropertyValue("node", out var temp) && temp != null)
        {
            NodeRef = temp.GetValue<uint>();
        }
        if (data.TryGetPropertyValue("param_name", out temp) && temp != null)
        {
            Target.paramName = temp.GetValue<string>();
        }

        if (data.TryGetPropertyValue("values", out temp) && temp is JsonArray array)
        {
            foreach (JsonArray item in array.Cast<JsonArray>())
            {
                var list = new List<Deformation>();
                foreach (JsonArray item1 in item.Cast<JsonArray>())
                {
                    var item2 = new Deformation();
                    item2.Deserialize(item1);
                    list.Add(item2);
                }
                Values.Add(list);
            }
        }

        if (data.TryGetPropertyValue("isSet", out temp) && temp is JsonArray array1)
        {
            IsSet = array1.ToListList<bool>();
        }

        if (data.TryGetPropertyValue("interpolate_mode", out temp) && temp != null 
            && !Enum.TryParse<InterpolateMode>(temp.GetValue<string>(), out var _interpolateMode))
        {
            InterpolateMode = _interpolateMode;
        }
        else
        {
            InterpolateMode = InterpolateMode.Linear;
        }

        int xCount = Parameter.AxisPointCount(0);
        int yCount = Parameter.AxisPointCount(1);

        if (Values.Count != xCount)
        {
            throw new Exception("Mismatched X value count");
        }
        foreach (var i in Values)
        {
            if (i.Count != yCount)
            {
                throw new Exception("Mismatched Y value count");
            }
        }

        if (IsSet.Count != xCount)
        {
            throw new Exception("Mismatched X isSet_ count");
        }
        foreach (var i in IsSet)
        {
            if (i.Count != yCount)
            {
                throw new Exception("Mismatched Y isSet_ count");
            }
        }
    }

    /// <summary>
    /// Clear all keypoint data
    /// </summary>
    public override void Clear()
    {
        int xCount = Parameter.AxisPointCount(0);
        int yCount = Parameter.AxisPointCount(1);

        Values = [];
        IsSet = [];
        for (int x = 0; x < xCount; x++)
        {
            IsSet.Add([]);
            Values.Add([]);
            for (int y = 0; y < yCount; y++)
            {
                IsSet[x].Add(false);
                var value = new Deformation();
                ClearValue(value);
                Values[x].Add(value);
            }
        }
    }

    public void ClearValue(Deformation val)
    {
        // Reset deformation to identity, with the right vertex count
        if (Target.node is Drawable d)
        {
            val.Clear(d.Vertices.Count);
        }
    }

    /// <summary>
    /// Gets the value at the specified point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Deformation GetValue(Vector2Int point)
    {
        return Values[point.X][point.Y];
    }

    /// <summary>
    /// Sets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    /// <param name="value"></param>
    public void SetValue(Vector2Int point, Deformation value)
    {
        Values[point.X][point.Y] = value;
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public override void Unset(Vector2Int point)
    {
        ClearValue(Values[point.X][point.Y]);
        IsSet[point.X][point.Y] = false;

        ReInterpolate();
    }

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public override void Reset(Vector2Int point)
    {
        ClearValue(Values[point.X][point.Y]);
        IsSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Flip the keypoints on an axis
    /// </summary>
    /// <param name="axis"></param>
    public override void ReverseAxis(int axis)
    {
        if (axis == 0)
        {
            Values.Reverse();
            IsSet.Reverse();
        }
        else
        {
            for (int i = 0; i < Values.Count; i++)
            {
                Values[i].Reverse();
            }
            for (int i = 0; i < IsSet.Count; i++)
            {
                IsSet[i].Reverse();
            }
        }
    }

    /// <summary>
    /// Re-calculate interpolation
    /// </summary>
    public override void ReInterpolate()
    {
        int xCount = Parameter.AxisPointCount(0);
        int yCount = Parameter.AxisPointCount(1);

        // Currently valid points
        var valid = new List<bool[]>();
        int validCount = 0;
        int totalCount = xCount * yCount;

        // Initialize validity map to user-set points
        for (int x = 0; x < xCount; x++)
        {
            valid.Add([.. IsSet[x]]);
            for (int y = 0; y < yCount; y++)
            {
                if (IsSet[x][y]) validCount++;
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
        var commitPoints = new List<Vector2Int>();

        // Used by extendAndIntersect for x/y factor
        float[][] interpDistance;
        interpDistance = new float[xCount][];
        for (int x = 0; x < xCount; x++)
        {
            interpDistance[x] = new float[yCount];
        }

        // Current interpolation axis
        bool yMajor = false;

        // Helpers to handle interpolation across both axes more easily
        int MajorCnt()
        {
            if (yMajor) return yCount;
            else return xCount;
        }
        int MinorCnt()
        {
            if (yMajor) return xCount;
            else return yCount;
        }
        bool IsValid(int maj, int min)
        {
            if (yMajor) return valid[min][maj];
            else return valid[maj][min];
        }
        bool IsNewlySet(int maj, int min)
        {
            if (yMajor) return newlySet[min][maj];
            else return newlySet[maj][min];
        }
        Deformation Get(int maj, int min)
        {
            if (yMajor) return Values[min][maj];
            else return Values[maj][min];
        }
        float GetDistance(int maj, int min)
        {
            if (yMajor) return interpDistance[min][maj];
            else return interpDistance[maj][min];
        }
        void Reset(int maj, int min, Deformation val, float distance = 0)
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
        void Set(int maj, int min, Deformation val, float distance = 0)
        {
            Reset(maj, min, val, distance);
            if (yMajor) commitPoints.Add(new Vector2Int(min, maj));
            else commitPoints.Add(new Vector2Int(maj, min));
        }
        float AxisPoint(int idx)
        {
            if (yMajor) return Parameter.AxisPoints[0][idx];
            else return Parameter.AxisPoints[1][idx];
        }
        Deformation Interp(int maj, int left, int mid, int right)
        {
            float leftOff = AxisPoint(left);
            float midOff = AxisPoint(mid);
            float rightOff = AxisPoint(right);
            float off = (midOff - leftOff) / (rightOff - leftOff);

            //writefln("interp %d %d %d %d -> %f %f %f %f", maj, left, mid, right,
            //leftOff, midOff, rightOff, off);
            return Get(maj, left) * (1 - off) + Get(maj, right) * off;
        }

        void Interpolate1D2D(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            for (int i = 0; i < MajorCnt(); i++)
            {
                int l = 0;
                int cnt = MinorCnt();

                // Find first element set
                for (; l < cnt && !IsValid(i, l); l++) { }

                // Empty row, we're done
                if (l >= cnt) continue;

                while (true)
                {
                    // Advance until before a missing element
                    for (; l < cnt - 1 && IsValid(i, l + 1); l++) { }

                    // Reached right side, done
                    if (l >= (cnt - 1)) break;

                    // Find next set element
                    int r = l + 1;
                    for (; r < cnt && !IsValid(i, r); r++) { }

                    // If we ran off the edge, we're done
                    if (r >= cnt) break;

                    // Interpolate between the pair of valid elements
                    for (int m = l + 1; m < r; m++)
                    {
                        Deformation val = Interp(i, l, m, r);

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
                            Set(i, m, (val + Get(i, m)) * 0.5f);
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
            if (yCount <= 1 || xCount <= 1) return;

            void extrapolateCorner(int baseX, int baseY, int offX, int offY)
            {
                Deformation base1 = Values[baseX][baseY];
                Deformation dX = Values[baseX + offX][baseY] + (base1 * -1f);
                Deformation dY = Values[baseX][baseY + offY] + (base1 * -1f);
                Values[baseX + offX][baseY + offY] = base1 + dX + dY;
                commitPoints.Add(new Vector2Int(baseX + offX, baseY + offY));
            }

            for (int x = 0; x < xCount - 1; x++)
            {
                for (int y = 0; y < yCount - 1; y++)
                {
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

        void ExtendAndIntersect(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            void setOrAverage(int maj, int min, Deformation val, float origin)
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
                    Set(maj, min, val * (1 - frac) + Get(maj, min) * frac);
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

            for (int i = 0; i < MajorCnt(); i++)
            {
                int j;
                int cnt = MinorCnt();

                // Find first element set
                for (j = 0; j < cnt && !IsValid(i, j); j++) { }

                // Empty row, we're done
                if (j >= cnt) continue;

                // Replicate leftwards
                Deformation val = Get(i, j);
                float origin = AxisPoint(j);
                for (int k = 0; k < j; k++)
                {
                    setOrAverage(i, k, val, origin);
                }

                // Find last element set
                for (j = cnt - 1; j < cnt && !IsValid(i, j); j--) { }

                // Replicate rightwards
                val = Get(i, j);
                origin = AxisPoint(j);
                for (int k = j + 1; k < cnt; k++)
                {
                    setOrAverage(i, k, val, origin);
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
            if (validCount == totalCount) break;

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
            ExtendAndIntersect(false);
            ExtendAndIntersect(true);
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

    public Deformation Interpolate(Vector2Int leftKeypoint, Vector2 offset)
    {
        return InterpolateMode switch
        {
            InterpolateMode.Nearest => InterpolateNearest(leftKeypoint, offset),
            InterpolateMode.Linear => InterpolateLinear(leftKeypoint, offset),
            InterpolateMode.Cubic => InterpolateCubic(leftKeypoint, offset),
            _ => throw new Exception("out of range"),
        };
    }

    public Deformation InterpolateNearest(Vector2Int leftKeypoint, Vector2 offset)
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

    public Deformation InterpolateLinear(Vector2Int leftKeypoint, Vector2 offset)
    {
        Deformation p0, p1;

        if (Parameter.IsVec2)
        {
            var p00 = Values[leftKeypoint.X][leftKeypoint.Y];
            var p01 = Values[leftKeypoint.X][leftKeypoint.Y + 1];
            var p10 = Values[leftKeypoint.X + 1][leftKeypoint.Y];
            var p11 = Values[leftKeypoint.X + 1][leftKeypoint.Y + 1];
            p0 = MathHelper.Lerp(p00, p01, offset.Y);
            p1 = MathHelper.Lerp(p10, p11, offset.Y);
        }
        else
        {
            p0 = Values[leftKeypoint.X][0];
            p1 = Values[leftKeypoint.X + 1][0];
        }

        return MathHelper.Lerp(p0, p1, offset.X);
    }

    public Deformation InterpolateCubic(Vector2Int leftKeypoint, Vector2 offset)
    {
        Deformation p0, p1, p2, p3;

        Deformation BicubicInterp(Vector2Int left, float xt, float yt)
        {
            Deformation p01, p02, p03, p04;
            Deformation[] pOut = new Deformation[4];

            var xlen = Values.Count - 1;
            var ylen = Values[0].Count - 1;
            var xkp = leftKeypoint.X;
            var ykp = leftKeypoint.Y;

            for (int y = 0; y < 4; y++)
            {
                var yp = float.Clamp(ykp + y - 1, 0, ylen);

                p01 = Values[int.Max(xkp - 1, 0)][(int)yp];
                p02 = Values[xkp][(int)yp];
                p03 = Values[xkp + 1][(int)yp];
                p04 = Values[int.Min(xkp + 2, xlen)][(int)yp];
                pOut[y] = MathHelper.Cubic(p01, p02, p03, p04, xt);
            }

            return MathHelper.Cubic(pOut[0], pOut[1], pOut[2], pOut[3], yt);
        }

        if (Parameter.IsVec2)
        {
            return BicubicInterp(leftKeypoint, offset.X, offset.Y);
        }
        else
        {
            var xkp = leftKeypoint.X;
            var xlen = Values.Count - 1;

            p0 = Values[int.Max(xkp - 1, 0)][0];
            p1 = Values[xkp][0];
            p2 = Values[xkp + 1][0];
            p3 = Values[int.Min(xkp + 2, xlen)][0];
            return MathHelper.Cubic(p0, p1, p2, p3, offset.X);
        }
    }

    public override void Apply(Vector2Int leftKeypoint, Vector2 offset)
    {
        ApplyToTarget(Interpolate(leftKeypoint, offset));
    }

    public override void InsertKeypoints(int axis, int index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            int yCount = Parameter.AxisPointCount(1);

            Values.Insert(index, []);
            IsSet.Insert(index, []);

            for (int i = 0; i < yCount; i++)
            {
                IsSet[index].Add(false);
                Values[index].Add(new());
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                var item = Values[i];
                item.Insert(index, new Deformation());
            }
            for (int i = 0; i < IsSet.Count; i++)
            {
                var item = IsSet[i];
                item.Insert(index, false);
            }
        }

        ReInterpolate();
    }

    public override void MoveKeypoints(int axis, int oldindex, int newindex)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            {
                var swap = Values[oldindex];
                Values.RemoveAt(oldindex);
                Values.Insert(newindex, swap);
            }

            {
                var swap = IsSet[oldindex];
                IsSet.RemoveAt(oldindex);
                IsSet.Insert(newindex, swap);
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                var item = Values[i];
                var swap = item[oldindex];
                item.RemoveAt(oldindex);
                item.Insert(newindex, swap);
            }
            for (int i = 0; i < IsSet.Count; i++)
            {
                var item = IsSet[i];
                var swap = item[oldindex];
                item.RemoveAt(oldindex);
                item.Insert(newindex, swap);
            }
        }

        ReInterpolate();
    }

    public override void DeleteKeypoints(int axis, int index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            Values.RemoveAt(index);
            IsSet.RemoveAt(index);
        }
        else if (axis == 1)
        {
            for (int i = 0; i < Values.Count; i++)
            {
                Values[i].RemoveAt(index);
            }
            for (int i = 0; i < IsSet.Count; i++)
            {
                IsSet[i].RemoveAt(index);
            }
        }

        ReInterpolate();
    }

    public override void ScaleValueAt(Vector2Int index, int axis, float scale)
    {
        var vecScale = axis switch
        {
            -1 => new Vector2(scale, scale),
            0 => new Vector2(scale, 1),
            1 => new Vector2(1, scale),
            _ => throw new Exception("Bad axis"),
        };

        /* Default to just scalar scale */
        SetValue(index, GetValue(index) * vecScale);
    }

    public override void ExtrapolateValueAt(Vector2Int index, int axis)
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

    public override void CopyKeypointToBinding(Vector2Int src, ParameterBinding other, Vector2Int dest)
    {
        if (!GetIsSet(src))
        {
            other.Unset(dest);
        }
        else if (other is DeformationParameterBinding o)
        {
            o.SetValue(dest, GetValue(src));
        }
        else
        {
            throw new Exception("ParameterBinding class mismatch");
        }
    }

    public override void SwapKeypointWithBinding(Vector2Int src, ParameterBinding other, Vector2Int dest)
    {
        if (other is DeformationParameterBinding o)
        {
            bool thisSet = GetIsSet(src);
            bool otherSet = other.GetIsSet(dest);
            Deformation thisVal = GetValue(src);
            Deformation otherVal = o.GetValue(dest);

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

    /// <summary>
    /// Apply parameter to target node
    /// </summary>
    /// <param name="value"></param>
    public void ApplyToTarget(Deformation value)
    {
        if (Target.paramName != "deform")
        {
            throw new Exception("paramName is not deform");
        }

        if (Target.node is Drawable d)
        {
            d.DeformStack.Push(value);
        }
    }

    public override bool IsCompatibleWithNode(Node other)
    {
        if (Target.node is Drawable d)
        {
            if (other is Drawable o)
            {
                return d.Vertices.Count == o.Vertices.Count;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }
    }

    public void Update(Vector2Int point, Vector2[] offsets)
    {
        IsSet[point.X][point.Y] = true;
        Values[point.X][point.Y].Update([.. offsets]);
        ReInterpolate();
    }
}
