using System.Numerics;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public class DeformationParameterBinding : ParameterBindingImpl
{
    /// <summary>
    /// The value at each 2D keypoint
    /// </summary>
    public List<List<Deformation>> values;

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
    public override void serialize(JObject serializer)
    {
        serializer.Add("node", target.node.UUID);
        serializer.Add("param_name", target.paramName);
        serializer.Add("values", values.ToToken());
        serializer.Add("isSet", isSet_.ToToken());
        serializer.Add("interpolate_mode", interpolateMode.ToString());
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
            values = array.ToListList<Deformation>();
        }

        temp = data["isSet"];
        if (temp is JArray array1)
        {
            isSet_ = array1.ToListList<bool>();
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

        int xCount = parameter.axisPointCount(0);
        int yCount = parameter.axisPointCount(1);

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

        if (isSet_.Count != xCount)
        {
            throw new Exception("Mismatched X isSet_ count");
        }
        foreach (var i in isSet_)
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
    public override void clear()
    {
        int xCount = parameter.axisPointCount(0);
        int yCount = parameter.axisPointCount(1);

        values = [];
        isSet_ = [];
        for (int x = 0; x < xCount; x++)
        {
            isSet_.Add([]);
            values.Add([]);
            for (int y = 0; y < yCount; y++)
            {
                isSet_[x].Add(false);
                var value = new Deformation();
                clearValue(ref value);
                values.Add(value);
            }
        }
    }

    public void clearValue(ref Deformation val)
    {
        // Reset deformation to identity, with the right vertex count
        if (target.node is Drawable d)
        {
            val.Clear(d.vertices.length);
        }
    }

    /// <summary>
    /// Gets the value at the specified point
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    public Deformation getValue(Vector2Int point)
    {
        return values[point.X][point.Y];
    }

    /// <summary>
    /// Sets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    /// <param name="value"></param>
    public void setValue(Vector2Int point, Deformation value)
    {
        values[point.X][point.Y] = value;
        isSet_[point.X][point.Y] = true;

        reInterpolate();
    }

    /// <summary>
    /// Unsets value at specified keypoint
    /// </summary>
    /// <param name="point"></param>
    public override void unset(Vector2Int point)
    {
        clearValue(ref values[point.X][point.Y]);
        isSet_[point.X][point.Y] = false;

        reInterpolate();
    }

    /// <summary>
    /// Resets value at specified keypoint to default
    /// </summary>
    /// <param name="point"></param>
    public override void reset(Vector2Int point)
    {
        clearValue(ref values[point.X][point.Y]);
        isSet_[point.X][point.Y] = true;

        reInterpolate();
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
        int majorCnt()
        {
            if (yMajor) return yCount;
            else return xCount;
        }
        int minorCnt()
        {
            if (yMajor) return xCount;
            else return yCount;
        }
        bool isValid(int maj, int min)
        {
            if (yMajor) return valid[min][maj];
            else return valid[maj][min];
        }
        bool isNewlySet(int maj, int min)
        {
            if (yMajor) return newlySet[min][maj];
            else return newlySet[maj][min];
        }
        Deformation get(int maj, int min)
        {
            if (yMajor) return values[min][maj];
            else return values[maj][min];
        }
        float getDistance(int maj, int min)
        {
            if (yMajor) return interpDistance[min][maj];
            else return interpDistance[maj][min];
        }
        void reset(int maj, int min, Deformation val, float distance = 0)
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
        void set(int maj, int min, Deformation val, float distance = 0)
        {
            reset(maj, min, val, distance);
            if (yMajor) commitPoints.Add(new Vector2Int(min, maj));
            else commitPoints.Add(new Vector2Int(maj, min));
        }
        float axisPoint(int idx)
        {
            if (yMajor) return parameter.axisPoints[0][idx];
            else return parameter.axisPoints[1][idx];
        }
        Deformation interp(int maj, int left, int mid, int right)
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

            for (int i = 0; i < majorCnt(); i++)
            {
                int l = 0;
                int cnt = minorCnt();

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
                    int r = l + 1;
                    for (; r < cnt && !isValid(i, r); r++) { }

                    // If we ran off the edge, we're done
                    if (r >= cnt) break;

                    // Interpolate between the pair of valid elements
                    for (int m = l + 1; m < r; m++)
                    {
                        Deformation val = interp(i, l, m, r);

                        // If we're running the second stage of intersecting 1D interpolation
                        if (secondPass && isNewlySet(i, m))
                        {
                            // Found an intersection, do not commit the previous points
                            if (!detectedIntersections)
                            {
                                //debug writefln("Intersection at %d, %d", i, m);
                                commitPoints.Clear();
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

            void extrapolateCorner(int baseX, int baseY, int offX, int offY)
            {
                Deformation base1 = values[baseX][baseY];
                Deformation dX = values[baseX + offX][baseY] + (base1 * -1f);
                Deformation dY = values[baseX][baseY + offY] + (base1 * -1f);
                values[baseX + offX][baseY + offY] = base1 + dX + dY;
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

        void extendAndIntersect(bool secondPass)
        {
            yMajor = secondPass;
            bool detectedIntersections = false;

            void setOrAverage(int maj, int min, Deformation val, float origin)
            {
                float minDist = float.Abs(axisPoint(min) - origin);
                // Same logic as in interpolate1D2D
                if (secondPass && isNewlySet(maj, min))
                {
                    // Found an intersection, do not commit the previous points
                    if (!detectedIntersections)
                    {
                        commitPoints.Clear();
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

            for (int i = 0; i < majorCnt(); i++)
            {
                int j;
                int cnt = minorCnt();

                // Find first element set
                for (j = 0; j < cnt && !isValid(i, j); j++) { }

                // Empty row, we're done
                if (j >= cnt) continue;

                // Replicate leftwards
                Deformation val = get(i, j);
                float origin = axisPoint(j);
                for (int k = 0; k < j; k++)
                {
                    setOrAverage(i, k, val, origin);
                }

                // Find last element set
                for (j = cnt - 1; j < cnt && !isValid(i, j); j--) { }

                // Replicate rightwards
                val = get(i, j);
                origin = axisPoint(j);
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
            interpolate1D2D(false);
            // Try 1D interpolation in the Y-Major direction, with intersection detection
            // If this finds an intersection with the above, it will fall back to
            // computing *only* the intersecting points as the average of the interpolated values.
            // If that happens, the next loop will re-try normal 1D interpolation.
            interpolate1D2D(true);
            // Did we get work done? If so, commit and loop
            if (commitPoints.Count > 0) continue;

            // Now try corner extrapolation
            extrapolateCorners();
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

    public Deformation interpolate(Vector2Int leftKeypoint, Vector2 offset)
    {
        switch (interpolateMode)
        {
            case InterpolateMode.Nearest:
                return interpolateNearest(leftKeypoint, offset);
            case InterpolateMode.Linear:
                return interpolateLinear(leftKeypoint, offset);
            case InterpolateMode.Cubic:
                return interpolateCubic(leftKeypoint, offset);
            default: throw new Exception("out of range");
        }
    }

    public Deformation interpolateNearest(Vector2Int leftKeypoint, Vector2 offset)
    {
        var px = leftKeypoint.X + ((offset.X >= 0.5) ? 1 : 0);
        if (parameter.isVec2)
        {
            var py = leftKeypoint.Y + ((offset.Y >= 0.5) ? 1 : 0);
            return values[px][py];
        }
        else
        {
            return values[px][0];
        }
    }

    public Deformation interpolateLinear(Vector2Int leftKeypoint, Vector2 offset)
    {
        Deformation p0, p1;

        if (parameter.isVec2)
        {
            Deformation p00 = values[leftKeypoint.X][leftKeypoint.Y];
            Deformation p01 = values[leftKeypoint.X][leftKeypoint.Y + 1];
            Deformation p10 = values[leftKeypoint.X + 1][leftKeypoint.Y];
            Deformation p11 = values[leftKeypoint.X + 1][leftKeypoint.Y + 1];
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

    public Deformation interpolateCubic(Vector2Int leftKeypoint, Vector2 offset)
    {
        Deformation p0, p1, p2, p3;

        Deformation bicubicInterp(Vector2Int left, float xt, float yt)
        {
            Deformation p01, p02, p03, p04;
            Deformation[] pOut = new Deformation[4];

            var xlen = values.Length - 1;
            var ylen = values[0].Length - 1;
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

        if (parameter.isVec2)
        {
            return bicubicInterp(leftKeypoint, offset.X, offset.Y);
        }
        else
        {
            var xkp = leftKeypoint.X;
            var xlen = values.Length - 1;

            p0 = values[int.Max(xkp - 1, 0)][0];
            p1 = values[xkp][0];
            p2 = values[xkp + 1][0];
            p3 = values[int.Min(xkp + 2, xlen)][0];
            return MathHelper.Cubic(p0, p1, p2, p3, offset.X);
        }
    }

    public override void apply(Vector2Int leftKeypoint, Vector2 offset)
    {
        applyToTarget(interpolate(leftKeypoint, offset));
    }

    public override void insertKeypoints(int axis, int index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            int yCount = parameter.axisPointCount(1);

            values.Insert(index, []);
            values[index] = new Deformation[yCount];
            isSet_.Insert(index, []);
            isSet_[index] = new bool[yCount];
        }
        else if (axis == 1)
        {
            for (int i = 0; i < values.Count; i++)
            {
                var item = values[i];
                item.Insert(index, new Deformation());
            }
            for (int i = 0; i < isSet_.Count; i++)
            {
                var item = isSet_[i];
                item.Insert(index, false);
            }
        }

        reInterpolate();
    }

    public override void moveKeypoints(int axis, int oldindex, int newindex)
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
                var swap = isSet_[oldindex];
                isSet_.RemoveAt(oldindex);
                isSet_.Insert(newindex, swap);
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
            for (int i = 0; i < isSet_.Count; i++)
            {
                var item = isSet_[i];
                var swap = item[oldindex];
                item.RemoveAt(oldindex);
                item.Insert(newindex, swap);
            }
        }

        reInterpolate();
    }

    public override void deleteKeypoints(int axis, int index)
    {
        if (!(axis == 0 || axis == 1))
        {
            throw new Exception("axis was error value");
        }

        if (axis == 0)
        {
            values.RemoveAt(index);
            isSet_.RemoveAt(index);
        }
        else if (axis == 1)
        {
            for (int i = 0; i < values.Count; i++)
            {
                values[i].RemoveAt(index);
            }
            for (int i = 0; i < isSet_.Count; i++)
            {
                isSet_[i].RemoveAt(index);
            }
        }

        reInterpolate();
    }

    public override void scaleValueAt(Vector2Int index, int axis, float scale)
    {
        Vector2 vecScale;

        switch (axis)
        {
            case -1: vecScale = new(scale, scale); break;
            case 0: vecScale = new(scale, 1); break;
            case 1: vecScale = new(1, scale); break;
            default: throw new Exception("Bad axis");
        }

        /* Default to just scalar scale */
        setValue(index, getValue(index) * vecScale);
    }

    public override void extrapolateValueAt(Vector2Int index, int axis)
    {
        var offset = parameter.getKeypointOffset(index);

        switch (axis)
        {
            case -1: offset = new Vector2(1, 1) - offset; break;
            case 0: offset.X = 1 - offset.X; break;
            case 1: offset.Y = 1 - offset.Y; break;
            default: throw new Exception("bad axis");
        }

        Vector2Int srcIndex;
        Vector2 subOffset;
        parameter.findOffset(offset, out srcIndex, out subOffset);

        Deformation srcVal = interpolate(srcIndex, subOffset);

        setValue(index, srcVal);
        scaleValueAt(index, axis, -1);
    }

    public override void copyKeypointToBinding(Vector2Int src, ParameterBinding other, Vector2Int dest)
    {
        if (!isSet(src))
        {
            other.unset(dest);
        }
        else if (other is DeformationParameterBinding o)
        {
            o.setValue(dest, getValue(src));
        }
        else
        {
            throw new Exception("ParameterBinding class mismatch");
        }
    }

    public override void swapKeypointWithBinding(Vector2Int src, ParameterBinding other, Vector2Int dest)
    {
        if (other is DeformationParameterBinding o)
        {
            bool thisSet = isSet(src);
            bool otherSet = other.isSet(dest);
            Deformation thisVal = getValue(src);
            Deformation otherVal = o.getValue(dest);

            // Swap directly, to avoid clobbering by update
            o.values[dest.x][dest.y] = thisVal;
            o.isSet_[dest.x][dest.y] = thisSet;
            values[src.x][src.y] = otherVal;
            isSet_[src.x][src.y] = otherSet;

            reInterpolate();
            o.reInterpolate();
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
    public override void applyToTarget(Deformation value)
    {
        if (this.target.paramName != "deform")
        {
            throw new Exception("paramName is not deform");
        }

        if (target.node is Drawable d)
        {
            d.DeformStack.push(value);
        }
    }

    public override bool isCompatibleWithNode(Node other)
    {
        if (target.node is Drawable d)
        {
            if (other is Drawable o)
            {
                return d.Vertices.length == o.vertices.length;
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

    public void update(Vector2Int point, Vector2[] offsets)
    {
        this.isSet_[point.X][point.Y] = true;
        this.values[point.X][point.Y].update(offsets.ToArray());
        this.reInterpolate();
    }
}
