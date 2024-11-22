using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Animations;

public class AnimationLane
{
    private uint refuuid;

    /// <summary>
    /// Reference to parameter if any
    /// </summary>
    public AnimationParameterRef? paramRef;

    /// <summary>
    /// List of frames in the lane
    /// </summary>
    public List<Keyframe> frames = [];

    /// <summary>
    /// The interpolation between each frame in the lane
    /// </summary>
    public InterpolateMode interpolation;

    /// <summary>
    /// Merging mode of the lane
    /// <see cref = "ParamMergeMode" />
    /// </summary>
    public string mergeMode = ParamMergeMode.Forced;

    /// <summary>
    /// Serialization function
    /// </summary>
    /// <param name="serializer"></param>
    public void serialize(JObject serializer)
    {
        serializer.Add("interpolation", interpolation.ToString());
        if (paramRef != null)
        {
            serializer.Add("uuid", paramRef.targetParam.uuid);
            serializer.Add("target", paramRef.targetAxis);
        }
        serializer.Add("keyframes", new JArray(frames));
        serializer.Add("merge_mode", mergeMode);
    }

    /// <summary>
    /// Deserialization function
    /// </summary>
    /// <param name="data"></param>
    public void deserialize(JObject data)
    {
        var temp = data["interpolation"];
        if (temp != null)
        {
            interpolation = Enum.Parse<InterpolateMode>(temp.ToString());
        }

        temp = data["uuid"];
        if (temp != null)
        {
            refuuid = (uint)temp;
        }

        this.paramRef = new AnimationParameterRef(null, 0);

        temp = data["target"];
        if (temp != null)
        {
            this.paramRef.targetAxis = (int)temp;
        }

        temp = data["keyframes"];
        if (temp is JArray array)
        {
            foreach (var item in array)
            {
                frames.Add(item.ToObject<Keyframe>()!);
            }
        }

        temp = data["merge_mode"];
        if (temp != null)
        {
            mergeMode = temp.ToString();
        }
    }

    /// <summary>
    /// Gets the interpolated state of a frame of animation 
    /// for this lane
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="snapSubframes"></param>
    /// <returns></returns>
    public float get(float frame, bool snapSubframes = false)
    {
        if (frames.Count > 0)
        {
            // If subframe snapping is turned on then we'll only run at the framerate
            // of the animation, without any smooth interpolation on faster app rates.
            if (snapSubframes) frame = MathF.Floor(frame);

            // Fallback if there's only 1 frame
            if (frames.Count == 1) return frames[0].Value;

            for (int i = 0; i < frames.Count; i++)
            {
                if (frames[i].Frame < frame) continue;

                // Fallback to not try to index frame -1
                if (i == 0) return frames[0].Value;

                // Interpolation "time" 0->1
                // Note we use floats here in case you're running the
                // update step faster than the timestep of the animation
                // This way it won't look choppy
                float tonext = frames[i].Frame - frame;
                float ilen = frames[i].Frame - (float)frames[i - 1].Frame;
                float t = 1 - (tonext / ilen);

                // Interpolation tension 0->1
                float tension = frames[i].Tension;

                switch (interpolation)
                {
                    // Nearest - Snap to the closest frame
                    case InterpolateMode.Nearest:
                        return t > 0.5 ? frames[i].Value : frames[i - 1].Value;

                    // Stepped - Snap to the current active keyframe
                    case InterpolateMode.Stepped:
                        return frames[i - 1].Value;

                    // Linear - Linearly interpolate between frame A and B
                    case InterpolateMode.Linear:
                        return float.Lerp(frames[i - 1].Value, frames[i].Value, t);

                    // Cubic - Smoothly in a curve between frame A and B
                    case InterpolateMode.Cubic:
                        float prev = frames[int.Max(i - 2, 0)].Value;
                        float curr = frames[int.Max(i - 1, 0)].Value;
                        float next1 = frames[int.Min(i, frames.Count - 1)].Value;
                        float next2 = frames[int.Min(i + 1, frames.Count - 1)].Value;

                        // TODO: Switch formulae, catmullrom interpolation
                        return MathHelper.Cubic(prev, curr, next1, next2, t);

                    // Bezier - Allows the user to specify beziér curves.
                    case InterpolateMode.Bezier:
                        // TODO: Switch formulae, Beziér curve
                        return float.Lerp(frames[i - 1].Value, frames[i].Value, float.Clamp(MathHelper.Hermite(0, 2 * tension, 1, 2 * tension, t), 0, 1));

                    default: throw new Exception("interpolation out");
                }
            }
            return frames[^1].Value;
        }

        // Fallback, no values.
        // Ideally we won't even call this function
        // if there's nothing to do.
        return 0;
    }

    public void reconstruct(Puppet puppet) { }

    public void finalize(Puppet puppet)
    {
        if (paramRef != null) paramRef.targetParam = puppet.findParameter(refuuid);
    }

    /// <summary>
    /// Updates the order of the keyframes
    /// </summary>
    public void updateFrames()
    {
        frames.Sort((a, b) => a.Frame.CompareTo(b.Frame));
    }
}
