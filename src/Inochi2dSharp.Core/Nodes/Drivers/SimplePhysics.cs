using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Inochi2dSharp.Core.Math;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Core.Phys;
using Inochi2dSharp.Core.Render;

namespace Inochi2dSharp.Core.Nodes.Drivers;

/// <summary>
/// Simple Physics Node
/// </summary>
[TypeId("SimplePhysics", 0x00000103)]
public class SimplePhysics : Driver
{
    private Guid _paramRef;

    private Parameter? _param;

    private float _offsetGravity = 1.0f;
    private float _offsetLength = 0;
    private float _offsetFrequency = 1;
    private float _offsetAngleDamping = 0.5f;
    private float _offsetLengthDamping = 0.5f;
    private Vector2 _offsetOutputScale = new(1, 1);

    private PhysicsModel _modelType = PhysicsModel.Pendulum;

    public Vector2 Output;

    /// <summary>
    /// The mapping between physics space and parameter space.
    /// </summary>
    public ParamMapMode MapMode = ParamMapMode.AngleLength;

    /// <summary>
    /// Whether physics system listens to local transform only.
    /// </summary>
    public bool LocalOnly = false;

    /// <summary>
    /// Gravity scale (1.0 = puppet gravity)
    /// </summary>
    public float Gravity = 1.0f;

    /// <summary>
    /// Pendulum/spring rest length (pixels)
    /// </summary>
    public float Length = 100;

    /// <summary>
    /// Resonant frequency (Hz)
    /// </summary>
    public float Frequency = 1;

    /// <summary>
    /// Angular damping ratio
    /// </summary>
    public float AngleDamping = 0.5f;

    /// <summary>
    /// Length damping ratio
    /// </summary>
    public float LengthDamping = 0.5f;

    /// <summary>
    /// Output scale
    /// </summary>
    public Vector2 OutputScale = new(1, 1);
    /// <summary>
    /// Previous anchor
    /// </summary>
    public Vector2 PrevAnchor = new(0, 0);
    /// <summary>
    /// Current anchor
    /// </summary>
    public Vector2 Anchor = new(0, 0);

    /// <summary>
    /// The parameter that the physics system affects.
    /// </summary>
    public Parameter? Param
    {
        get => _param;
        set
        {
            _param = value;
            if (value is null) _paramRef = Guid.Empty;
            else _paramRef = value.Guid;
        }
    }

    /// <summary>
    /// The physics model to apply.
    /// </summary>
    public PhysicsModel ModelType
    {
        get => _modelType;
        set
        {
            _modelType = value;
            Reset();
        }
    }

    /// <summary>
    /// The affected parameters of the driver.
    /// </summary>
    public override Parameter[] AffectedParameters => _param != null ? [_param] : null!;

    /// <summary>
    /// Physics scale.
    /// </summary>
    public float Scale => Puppet.Physics.PixelsPerMeter;

    /// <summary>
    /// The final gravity
    /// </summary>
    /// <returns></returns>
    public float FinalGravity => Gravity * _offsetGravity * Puppet.Physics.Gravity * Scale;

    /// <summary>
    /// The final length
    /// </summary>
    public float FinalLength => Length + _offsetLength;

    /// <summary>
    /// The final frequency
    /// </summary>
    public float FinalFrequency => Frequency * _offsetFrequency;

    /// <summary>
    /// The final angle damping
    /// </summary>
    public float FinalAngleDamping => AngleDamping * _offsetAngleDamping;

    /// <summary>
    /// The final length damping
    /// </summary>
    public float FinalLengthDamping => LengthDamping * _offsetLengthDamping;

    /// <summary>
    /// The final output scale
    /// </summary>
    public Vector2 FinalOutputScale => OutputScale * _offsetOutputScale;

    protected PhysicsSystem _system;

    /// <summary>
    /// Constructs a new SimplePhysics node
    /// </summary>
    /// <param name="parent"></param>
    public SimplePhysics(Node? parent = null) : this(Guid.NewGuid(), parent)
    {

    }

    /// <summary>
    /// Constructs a new SimplePhysics node
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    private SimplePhysics(Guid guid, Node? parent = null) : base(guid, parent)
    {
        Reset();
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="recursive"></param>
    public override void Serialize(JsonObject obj, bool recursive = true)
    {
        base.Serialize(obj, recursive);
        var target = _paramRef.ToString();
        obj["target"] = target;
        obj["model_type"] = _modelType.GetString();
        obj["map_mode"] = MapMode.GetString();
        obj["gravity"] = Gravity;
        obj["length"] = Length;
        obj["frequency"] = Frequency;
        obj["angle_damping"] = AngleDamping;
        obj["length_damping"] = LengthDamping;
        obj["output_scale"] = OutputScale.ToToken();
        obj["local_only"] = LocalOnly;
    }

    public override void Deserialize(JsonElement data)
    {
        base.Deserialize(data);

        _paramRef = data.GetGuid("param", "target");

        foreach (var item in data.EnumerateObject())
        {
            if (item.Name == "model_type" && item.Value.ValueKind != JsonValueKind.Null)
            {
                _modelType = item.Value.GetString()!.ToPhysicsModel();
            }
            else if (item.Name == "map_mode" && item.Value.ValueKind != JsonValueKind.Null)
            {
                MapMode = item.Value.GetString()!.ToParamMapMode();
            }
            else if (item.Name == "gravity" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Gravity = item.Value.GetSingle();
            }
            else if (item.Name == "length" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Length = item.Value.GetSingle();
            }
            else if (item.Name == "frequency" && item.Value.ValueKind != JsonValueKind.Null)
            {
                Frequency = item.Value.GetSingle();
            }
            else if (item.Name == "angle_damping" && item.Value.ValueKind != JsonValueKind.Null)
            {
                AngleDamping = item.Value.GetSingle();
            }
            else if (item.Name == "length_damping" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LengthDamping = item.Value.GetSingle();
            }
            else if (item.Name == "output_scale" && item.Value.ValueKind == JsonValueKind.Array)
            {
                OutputScale = item.Value.ToVector2();
            }
            else if (item.Name == "local_only" && item.Value.ValueKind != JsonValueKind.Null)
            {
                LocalOnly = item.Value.GetBoolean();
            }
        }
    }

    public override void Finalized()
    {
        Param = Puppet.FindParameter(_paramRef);
        base.Finalized();
        Reset();
    }

    public override void PreUpdate(DrawList drawList)
    {
        base.PreUpdate(drawList);
        _offsetGravity = 1;
        _offsetLength = 0;
        _offsetFrequency = 1;
        _offsetAngleDamping = 1;
        _offsetLengthDamping = 1;
        _offsetOutputScale = new(1, 1);
    }

    public override void UpdateDriver(float delta)
    {
        // Timestep is limited to 10 seconds, as if you
        // Are getting 0.1 FPS, you have bigger issues to deal with.
        float h = float.Min(delta, 10);

        UpdateInputs();

        // Minimum physics timestep: 0.01s
        while (h > 0.01)
        {
            _system.Tick(0.01f);
            h -= 0.01f;
        }

        _system.Tick(h);
        UpdateOutputs();
    }

    public void UpdateAnchors()
    {
        _system.UpdateAnchor();
    }

    public void UpdateInputs()
    {
        var anchorPos = LocalOnly ?
                new Vector4(TransformLocal().Translation, 1) :
                Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1));
        Anchor = new Vector2(anchorPos.X, anchorPos.Y);
    }

    public void UpdateOutputs()
    {
        if (Param is null) return;

        var oscale = FinalOutputScale;

        // Okay, so this is confusing. We want to translate the angle back to local space,
        // but not the coordinates.

        // Transform the physics output back into local space.
        // The origin here is the anchor. This gives us the local angle.
        Vector4 localPos4;
        if (LocalOnly)
        {
            localPos4 = new Vector4(Output.X, Output.Y, 0, 1);
        }
        else
        {
            Matrix4x4.Invert(Transform().Matrix, out var temp);
            localPos4 = temp.Multiply(new Vector4(Output.X, Output.Y, 0, 1));
        }
        var localAngle = Vector2.Normalize(new Vector2(localPos4.X, localPos4.Y));

        // Figure out the relative length. We can work this out directly in global space.
        var relLength = Vector2.Distance(Output, Anchor) / FinalLength;

        var paramVal = new Vector2();
        switch (MapMode)
        {
            case ParamMapMode.XY:
                var localPosNorm = localAngle * relLength;
                paramVal = localPosNorm - new Vector2(0, 1);
                paramVal.Y = -paramVal.Y; // Y goes up for params
                break;
            case ParamMapMode.AngleLength:
                float a = float.Atan2(-localAngle.X, localAngle.Y) / MathF.PI;
                paramVal = new Vector2(a, relLength);
                break;
            case ParamMapMode.YX:
                localPosNorm = localAngle * relLength;
                paramVal = localPosNorm - new Vector2(0, 1);
                paramVal.Y = -paramVal.Y; // Y goes up for params
                paramVal = new Vector2(paramVal.Y, paramVal.X);
                break;
            case ParamMapMode.LengthAngle:
                a = float.Atan2(-localAngle.X, localAngle.Y) / MathF.PI;
                paramVal = new Vector2(relLength, a);
                break;
        }

        Param.PushIOffset(new Vector2(paramVal.X * oscale.X, paramVal.Y * oscale.Y), ParamMergeMode.Forced);
        Param.Update();
    }

    public override void Reset()
    {
        UpdateInputs();
        _system?.Dispose();
        _system = ModelType switch
        {
            PhysicsModel.Pendulum => new Pendulum(this),
            PhysicsModel.SpringPendulum => new SpringPendulum(this),
            _ => throw new Exception("modelType error"),
        };
    }

    public override bool HasParam(string key)
    {
        if (base.HasParam(key)) return true;

        return key switch
        {
            "gravity" or "length" or "frequency" or "angleDamping" or "lengthDamping" or "outputScale.x" or "outputScale.y" => true,
            _ => false,
        };
    }

    public override float GetDefaultValue(string key)
    {
        // Skip our list of our parent already handled it
        float def = base.GetDefaultValue(key);
        if (!float.IsNaN(def)) return def;

        return key switch
        {
            "gravity" or "frequency" or "angleDamping" or "lengthDamping" or "outputScale.x" or "outputScale.y" => 1,
            "length" => 0,
            _ => (float)0,
        };
    }

    public override bool SetValue(string key, float value)
    {
        // Skip our list of our parent already handled it
        if (base.SetValue(key, value)) return true;

        switch (key)
        {
            case "gravity":
                _offsetGravity *= value;
                return true;
            case "length":
                _offsetLength += value;
                return true;
            case "frequency":
                _offsetFrequency *= value;
                return true;
            case "angleDamping":
                _offsetAngleDamping *= value;
                return true;
            case "lengthDamping":
                _offsetLengthDamping *= value;
                return true;
            case "outputScale.x":
                _offsetOutputScale.X *= value;
                return true;
            case "outputScale.y":
                _offsetOutputScale.Y *= value;
                return true;
            default: return false;
        }
    }

    public override float GetValue(string key)
    {
        return key switch
        {
            "gravity" => _offsetGravity,
            "length" => _offsetLength,
            "frequency" => _offsetFrequency,
            "angleDamping" => _offsetAngleDamping,
            "lengthDamping" => _offsetLengthDamping,
            "outputScale.x" => _offsetOutputScale.X,
            "outputScale.y" => _offsetOutputScale.Y,
            _ => base.GetValue(key),
        };
    }

    public override void Dispose()
    {
        base.Dispose();
        _system.Dispose();
    }
}
