﻿using System.Numerics;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public class ValueParameterBinding : ParameterBindingImpl
{
    /// <summary>
    /// The value at each 2D keypoint
    /// </summary>
    public List<List<float>> values;

    public ValueParameterBinding(Parameter parameter) : base(parameter)
    {

    }

    public ValueParameterBinding(Parameter parameter, Node targetNode, string paramName) : base(parameter, targetNode, paramName)
    {

    }

    /// <summary>
    /// Serializes a binding
    /// </summary>
    /// <param name="serializer"></param>
    public override void Serialize(JObject serializer)
    {
        serializer.Add("node", Target.node.UUID);
        serializer.Add("param_name", Target.paramName);
        serializer.Add("values", values.ToToken());
        serializer.Add("isSet", isSet.ToToken());
        serializer.Add("interpolate_mode", interpolateMode.ToString());
    }

    /// <summary>
    /// Deserializes a binding
    /// </summary>
    /// <param name="data"></param>
    public override void Deserialize(JObject data)
    {
        var temp = data["node"];
        if (temp != null)
        {
            NodeRef = (uint)temp;
        }
        temp = data["param_name"];
        if (temp != null)
        {
            Target.paramName = temp.ToString();
        }

        temp = data["values"];
        if (temp is JArray array)
        {
            values = array.ToListList<float>();
        }

        temp = data["isSet"];
        if (temp is JArray array1)
        {
            isSet = array1.ToListList<bool>();
        }

        temp = data["interpolate_mode"];
        if (temp == null || !Enum.TryParse<InterpolateMode>(temp.ToString(), out var _interpolateMode))
        {
            interpolateMode = InterpolateMode.Linear;
        }
        else
        {
            interpolateMode = _interpolateMode;
        }

        int xCount = Parameter.AxisPointCount(0);
        int yCount = Parameter.AxisPointCount(1);

        if (values.Count != xCount)
        {
            throw new Exception("Mismatched X value count");
        }
        foreach (var i in values)
        {
            if (i.Count != yCount)
            {
                throw new Exception("Mismatched Y value count");
            }
        }

        if (isSet.Count != xCount)
        {
            throw new Exception("Mismatched X isSet_ count");
        }
        foreach (var i in isSet)
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

        values = [];
        isSet = [];
        for (int x = 0; x < xCount; x++)
        {
            isSet.Add([]);
            values.Add([]);
            for (int y = 0; y < yCount; y++)
            {
                isSet[x].Add(false);
                values[x].Add(0);
                var value = values[x][y];
                ClearValue(ref value);
                values[x][y] = value;
            }
        }
    }

    public void ClearValue(ref float val)
    {
        val = Target.node.GetDefaultValue(Target.paramName);
    }

    /// <summary>
    /// Gets the value at the specified point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public float GetValue(Vector2Int point)
    {
        return values[point.X][point.Y];
    }

    /// <summary>
    /// Sets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    /// <param name="value"></param>
    public void SetValue(Vector2Int point, float value)
    {
        values[point.X][point.Y] = value;
        isSet[point.X][point.Y] = true;

        ReInterpolate();
    }

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public override void Unset(Vector2Int point)
    {
        var value = values[point.X][point.Y];
        ClearValue(ref value);
        values[point.X][point.Y] = value;
        isSet[point.X][point.Y] = false;

        ReInterpolate();
    }

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public override void Reset(Vector2Int point)
    {
        var value = values[point.X][point.Y];
        ClearValue(ref value);
        values[point.X][point.Y] = value;
        isSet[point.X][point.Y] = true;

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
            values.Reverse();
            isSet.Reverse();
        }
        else
        {
            for (int i = 0; i < values.Count; i++)
            {
                values[i].Reverse();
            }
            for (int i = 0; i < isSet.Count; i++)
            {
                isSet[i].Reverse();
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
            valid.Add([.. isSet[x]]);
            for (int y = 0; y < yCount; y++)
            {
                if (isSet[x][y]) validCount++;
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
        float Get(int maj, int min)
        {
            if (yMajor) return values[min][maj];
            else return values[maj][min];
        }
        float GetDistance(int maj, int min)
        {
            if (yMajor) return interpDistance[min][maj];
            else return interpDistance[maj][min];
        }
        void Reset(int maj, int min, float val, float distance = 0)
        {
            if (yMajor)
            {
                //debug writefln("set (%d, %d) -> %s", min, maj, val);
                if (valid[min][maj])
                {
                    throw new Exception("valid error");
                }
                values[min][maj] = val;
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
                values[maj][min] = val;
                interpDistance[maj][min] = distance;
                newlySet[maj][min] = true;
            }
        }
        void Set(int maj, int min, float val, float distance = 0)
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
        float Interp(int maj, int left, int mid, int right)
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
                        float val = Interp(i, l, m, r);

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

            void ExtrapolateCorner(int baseX, int baseY, int offX, int offY)
            {
                float base1 = values[baseX][baseY];
                float dX = values[baseX + offX][baseY] + (base1 * -1f);
                float dY = values[baseX][baseY + offY] + (base1 * -1f);
                values[baseX + offX][baseY + offY] = base1 + dX + dY;
                commitPoints.Add(new Vector2Int(baseX + offX, baseY + offY));
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

        void ExtendAndIntersect(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            void SetOrAverage(int maj, int min, float val, float origin)
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
                float val = Get(i, j);
                float origin = AxisPoint(j);
                for (int k = 0; k < j; k++)
                {
                    SetOrAverage(i, k, val, origin);
                }

                // Find last element set
                for (j = cnt - 1; j < cnt && !IsValid(i, j); j--) { }

                // Replicate rightwards
                val = Get(i, j);
                origin = AxisPoint(j);
                for (int k = j + 1; k < cnt; k++)
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

    public float Interpolate(Vector2Int leftKeypoint, Vector2 offset)
    {
        return interpolateMode switch
        {
            InterpolateMode.Nearest => InterpolateNearest(leftKeypoint, offset),
            InterpolateMode.Linear => InterpolateLinear(leftKeypoint, offset),
            InterpolateMode.Cubic => InterpolateCubic(leftKeypoint, offset),
            _ => throw new Exception("out of range"),
        };
    }

    public float InterpolateNearest(Vector2Int leftKeypoint, Vector2 offset)
    {
        var px = leftKeypoint.X + ((offset.X >= 0.5) ? 1 : 0);
        if (Parameter.IsVec2)
        {
            var py = leftKeypoint.Y + ((offset.Y >= 0.5) ? 1 : 0);
            return values[px][py];
        }
        else
        {
            return values[px][0];
        }
    }

    public float InterpolateLinear(Vector2Int leftKeypoint, Vector2 offset)
    {
        float p0, p1;

        if (Parameter.IsVec2)
        {
            float p00 = values[leftKeypoint.X][leftKeypoint.Y];
            float p01 = values[leftKeypoint.X][leftKeypoint.Y + 1];
            float p10 = values[leftKeypoint.X + 1][leftKeypoint.Y];
            float p11 = values[leftKeypoint.X + 1][leftKeypoint.Y + 1];
            p0 = float.Lerp(p00, p01, offset.Y);
            p1 = float.Lerp(p10, p11, offset.Y);
        }
        else
        {
            p0 = values[leftKeypoint.X][0];
            p1 = values[leftKeypoint.X + 1][0];
        }

        return float.Lerp(p0, p1, offset.X);
    }

    public float InterpolateCubic(Vector2Int leftKeypoint, Vector2 offset)
    {
        float p0, p1, p2, p3;

        float BicubicInterp(Vector2Int left, float xt, float yt)
        {
            float p01, p02, p03, p04;
            float[] pOut = new float[4];

            var xlen = values.Count - 1;
            var ylen = values[0].Count - 1;
            var xkp = leftKeypoint.X;
            var ykp = leftKeypoint.Y;

            for (int y = 0; y < 4; y++)
            {
                var yp = float.Clamp(ykp + y - 1, 0, ylen);

                p01 = values[int.Max(xkp - 1, 0)][(int)yp];
                p02 = values[xkp][(int)yp];
                p03 = values[xkp + 1][(int)yp];
                p04 = values[int.Min(xkp + 2, xlen)][(int)yp];
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
            var xlen = values.Count - 1;

            p0 = values[int.Max(xkp - 1, 0)][0];
            p1 = values[xkp][0];
            p2 = values[xkp + 1][0];
            p3 = values[int.Min(xkp + 2, xlen)][0];
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

            values.Insert(index, []);
            isSet.Insert(index, []);
            for (int i = 0; i < yCount; i++)
            {
                values[index].Add(0);
                isSet[index].Add(false);
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var item = values[i];
                item.Insert(index, 0);
            }
            for (int i = 0; i < isSet.Count; i++)
            {
                var item = isSet[i];
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
                var swap = values[oldindex];
                values.RemoveAt(oldindex);
                values.Insert(newindex, swap);
            }

            {
                var swap = isSet[oldindex];
                isSet.RemoveAt(oldindex);
                isSet.Insert(newindex, swap);
            }
        }
        else if (axis == 1)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var item = values[i];
                var swap = item[oldindex];
                item.RemoveAt(oldindex);
                item.Insert(newindex, swap);
            }
            for (int i = 0; i < isSet.Count; i++)
            {
                var item = isSet[i];
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
            values.RemoveAt(index);
            isSet.RemoveAt(index);
        }
        else if (axis == 1)
        {
            for (int i = 0; i < values.Count; i++)
            {
                values[i].RemoveAt(index);
            }
            for (int i = 0; i < isSet.Count; i++)
            {
                isSet[i].RemoveAt(index);
            }
        }

        ReInterpolate();
    }

    public override void ScaleValueAt(Vector2Int index, int axis, float scale)
    {
        /* Nodes know how to do axis-aware scaling */
        SetValue(index, Node.ScaleValue(Target.paramName, GetValue(index), axis, scale));
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

        float srcVal = Interpolate(srcIndex, subOffset);

        SetValue(index, srcVal);
        ScaleValueAt(index, axis, -1);
    }

    public override void CopyKeypointToBinding(Vector2Int src, ParameterBinding other, Vector2Int dest)
    {
        if (!IsSet(src))
        {
            other.Unset(dest);
        }
        else if (other is ValueParameterBinding o)
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
        if (other is ValueParameterBinding o)
        {
            bool thisSet = IsSet(src);
            bool otherSet = other.IsSet(dest);
            float thisVal = GetValue(src);
            float otherVal = o.GetValue(dest);

            // Swap directly, to avoid clobbering by update
            o.values[dest.X][dest.Y] = thisVal;
            o.isSet[dest.X][dest.Y] = thisSet;
            values[src.X][src.Y] = otherVal;
            isSet[src.X][src.Y] = otherSet;

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
    public void ApplyToTarget(float value)
    {
        Target.node.SetValue(Target.paramName, value);
    }

    public override bool IsCompatibleWithNode(Node other)
    {
        return other.HasParam(Target.paramName);
    }
}