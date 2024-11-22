using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Math;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Param;

public class Parameter
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
    public ParameterBinding[] bindings;

    protected void Serialize(JObject serializer)
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
}
