using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Animations;

public class AnimationLane
{
    private uint _refuuid;

    /// <summary>
    /// Reference to parameter if any
    /// </summary>
    public AnimationParameterRef ParamRef { get; private set; }

    /// <summary>
    /// List of frames in the lane
    /// </summary>
    public List<Keyframe> Frames { get; init; } = [];

    /// <summary>
    /// The interpolation between each frame in the lane
    /// </summary>
    private InterpolateMode _interpolation;

    /// <summary>
    /// Merging mode of the lane
    /// <see cref = "ParamMergeMode" />
    /// </summary>
    public string MergeMode { get; private set; } = ParamMergeMode.Forced;

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void Serialize(JObject serializer)
    {
        serializer.Add("interpolation", _interpolation.ToString());
        if (ParamRef != null)
        {
            serializer.Add("uuid", ParamRef.TargetParam.UUID);
            serializer.Add("target", ParamRef.TargetAxis);
        }
        serializer.Add("keyframes", new JArray(Frames));
        serializer.Add("merge_mode", MergeMode);
    }

    /// <summary>
    /// Deserialization function
    /// </summary>
    /// <param name="data"></param>
    public void Deserialize(JObject data)
    {
        var temp = data["interpolation"];
        if (temp != null)
        {
            _interpolation = Enum.Parse<InterpolateMode>(temp.ToString());
        }

        temp = data["uuid"];
        if (temp != null)
        {
            _refuuid = (uint)temp;
        }

        ParamRef = new AnimationParameterRef();

        temp = data["target"];
        if (temp != null)
        {
            ParamRef.TargetAxis = (int)temp;
        }

        temp = data["keyframes"];
        if (temp is JArray array)
        {
            foreach (var item in array)
            {
                Frames.Add(item.ToObject<Keyframe>()!);
            }
        }

        temp = data["merge_mode"];
        if (temp != null)
        {
            MergeMode = temp.ToString();
        }
    }

    /// <summary>
    /// Gets the interpolated state of a frame of animation 
    /// for this lane
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="snapSubframes"></param>
    /// <returns></returns>
    public float Get(float frame, bool snapSubframes = false)
    {
        if (Frames.Count > 0)
        {
            // If subframe snapping is turned on then we'll only run at the framerate
            // of the animation, without any smooth interpolation on faster app rates.
            if (snapSubframes) frame = MathF.Floor(frame);

            // Fallback if there's only 1 frame
            if (Frames.Count == 1) return Frames[0].Value;

            for (int i = 0; i < Frames.Count; i++)
            {
                if (Frames[i].Frame < frame) continue;

                // Fallback to not try to index frame -1
                if (i == 0) return Frames[0].Value;

                // Interpolation "time" 0->1
                // Note we use floats here in case you're running the
                // update step faster than the timestep of the animation
                // This way it won't look choppy
                float tonext = Frames[i].Frame - frame;
                float ilen = Frames[i].Frame - (float)Frames[i - 1].Frame;
                float t = 1 - (tonext / ilen);

                // Interpolation tension 0->1
                float tension = Frames[i].Tension;

                switch (_interpolation)
                {
                    // Nearest - Snap to the closest frame
                    case InterpolateMode.Nearest:
                        return t > 0.5 ? Frames[i].Value : Frames[i - 1].Value;

                    // Stepped - Snap to the current active keyframe
                    case InterpolateMode.Stepped:
                        return Frames[i - 1].Value;

                    // Linear - Linearly interpolate between frame A and B
                    case InterpolateMode.Linear:
                        return float.Lerp(Frames[i - 1].Value, Frames[i].Value, t);

                    // Cubic - Smoothly in a curve between frame A and B
                    case InterpolateMode.Cubic:
                        float prev = Frames[int.Max(i - 2, 0)].Value;
                        float curr = Frames[int.Max(i - 1, 0)].Value;
                        float next1 = Frames[int.Min(i, Frames.Count - 1)].Value;
                        float next2 = Frames[int.Min(i + 1, Frames.Count - 1)].Value;

                        // TODO: Switch formulae, catmullrom interpolation
                        return MathHelper.Cubic(prev, curr, next1, next2, t);

                    // Bezier - Allows the user to specify beziér curves.
                    case InterpolateMode.Bezier:
                        // TODO: Switch formulae, Beziér curve
                        return float.Lerp(Frames[i - 1].Value, Frames[i].Value, float.Clamp(MathHelper.Hermite(0, 2 * tension, 1, 2 * tension, t), 0, 1));

                    default: throw new Exception("interpolation out");
                }
            }
            return Frames[^1].Value;
        }

        // Fallback, no values.
        // Ideally we won't even call this function
        // if there's nothing to do.
        return 0;
    }

    public void Reconstruct(Puppet puppet) { }

    public void Finalize(Puppet puppet)
    {
        if (ParamRef != null) ParamRef.TargetParam = puppet.FindParameter(_refuuid)!;
    }

    /// <summary>
    /// Updates the order of the keyframes
    /// </summary>
    public void UpdateFrames()
    {
        Frames.Sort((a, b) => a.Frame.CompareTo(b.Frame));
    }
}
