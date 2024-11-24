﻿using System.Numerics;
using Inochi2dSharp.Core.Param;
using Inochi2dSharp.Math;
using Inochi2dSharp.Phys;
using Newtonsoft.Json.Linq;

namespace Inochi2dSharp.Core.Nodes.Drivers;

[TypeId("SimplePhysics")]
public class SimplePhysics : Driver
{
    private uint _paramRef = NodeHelper.InInvalidUUID;

    private Parameter? _param;

    private float _offsetGravity = 1.0f;
    private float _offsetLength = 0;
    private float _offsetFrequency = 1;
    private float _offsetAngleDamping = 0.5f;
    private float _offsetLengthDamping = 0.5f;
    private Vector2 _offsetOutputScale = new(1, 1);

    public string _modelType = PhysicsModel.Pendulum;
    public string MapMode = ParamMapMode.AngleLength;

    /// <summary>
    /// Whether physics system listens to local transform only.
    /// </summary>
    public bool LocalOnly = false;

    /// <summary>
    /// Gravity scale (1.0 = puppet gravity)
    /// </summary>
    private float _gravity = 1.0f;

    /// <summary>
    /// Pendulum/spring rest length (pixels)
    /// </summary>
    public float _length = 100;

    /// <summary>
    /// Resonant frequency (Hz)
    /// </summary>
    public float _frequency = 1;

    /// <summary>
    /// Angular damping ratio
    /// </summary>
    public float _angleDamping = 0.5f;

    /// <summary>
    /// Length damping ratio
    /// </summary>
    public float _lengthDamping = 0.5f;

    public Vector2 _outputScale = new(1, 1);

    public Vector2 _prevAnchor = new(0, 0);

    public Matrix4x4 _prevTransMat;

    public bool _prevAnchorSet = false;

    public Vector2 _anchor = new(0, 0);

    public Vector2 _output;

    public PhysicsSystem _system;

    public string ModelType
    {
        get => _modelType;
        set
        {
            _modelType = value;
            Reset();
        }
    }

    /// <summary>
    /// Gets the final gravity
    /// </summary>
    public float Gravity => _gravity * _offsetGravity * Puppet.Physics.gravity * GetScale();

    /// <summary>
    /// Gets the final length
    /// </summary>
    public float Length => _length + _offsetLength;

    /// <summary>
    /// Gets the final frequency
    /// </summary>
    public float Frequency => _frequency * _offsetFrequency;

    /// <summary>
    /// Gets the final angle damping
    /// </summary>
    public float AngleDamping => _angleDamping * _offsetAngleDamping;

    /// <summary>
    /// Gets the final length damping
    /// </summary>
    public float LengthDamping => _lengthDamping * _offsetLengthDamping;

    /// <summary>
    /// Gets the final length damping
    /// </summary>
    public Vector2 OutputScale => _outputScale * _offsetOutputScale;

    public Parameter? Param
    {
        get => _param;
        set
        {
            _param = value;
            if (value is null) _paramRef = NodeHelper.InInvalidUUID;
            else _paramRef = value.UUID;
        }
    }

    public SimplePhysics() : this(null)
    {

    }

    /// <summary>
    /// Constructs a new SimplePhysics node
    /// </summary>
    /// <param name="parent"></param>
    public SimplePhysics(Node? parent = null) : this(NodeHelper.InCreateUUID(), parent)
    {

    }

    /// <summary>
    /// Constructs a new SimplePhysics node
    /// </summary>
    /// <param name="uuid"></param>
    /// <param name="parent"></param>
    public SimplePhysics(uint uuid, Node? parent = null) : base(uuid, parent)
    {
        Reset();
    }

    public override void BeginUpdate()
    {
        base.BeginUpdate();
        _offsetGravity = 1;
        _offsetLength = 0;
        _offsetFrequency = 1;
        _offsetAngleDamping = 1;
        _offsetLengthDamping = 1;
        _offsetOutputScale = new(1, 1);
    }

    public override void Update()
    {
        base.Update();
    }

    public override Parameter[] GetAffectedParameters()
    {
        if (_param is null) return [];
        return [_param];
    }

    public override void UpdateDriver()
    {
        // Timestep is limited to 10 seconds, as if you
        // Are getting 0.1 FPS, you have bigger issues to deal with.
        float h = float.Min(Inochi2d.deltaTime(), 10);

        UpdateInputs();

        // Minimum physics timestep: 0.01s
        while (h > 0.01)
        {
            _system.Tick(0.01f);
            h -= 0.01f;
        }

        _system.Tick(h);
        UpdateOutputs();
        _prevAnchorSet = false;
    }

    public void UpdateAnchors()
    {
        _system.UpdateAnchor();
    }

    public void UpdateInputs()
    {
        if (_prevAnchorSet)
        {

        }
        else
        {
            var anchorPos = LocalOnly ?
                (new Vector4(TransformLocal().Translation, 1)) :
                (Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1)));
            _anchor = new Vector2(anchorPos.X, anchorPos.Y);
        }
    }

    public override void PreProcess()
    {
        var temp = LocalOnly ?
            new Vector4(TransformLocal().Translation, 1) :
            Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1));
        var prevPos = new Vector2(temp.X, temp.Y);
        base.PreProcess();
        temp = LocalOnly ?
            new Vector4(TransformLocal().Translation, 1) :
            Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1));
        var anchorPos = new Vector2(temp.X, temp.Y);
        if (anchorPos != prevPos)
        {
            _anchor = anchorPos;
            _prevTransMat = Transform().Matrix.Copy();
            _prevAnchorSet = true;
        }
    }

    public override void PostProcess()
    {
        var temp = LocalOnly ?
            new Vector4(TransformLocal().Translation, 1) :
            Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1));
        var prevPos = new Vector2(temp.X, temp.Y);
        base.PostProcess();
        temp = LocalOnly ?
            new Vector4(TransformLocal().Translation, 1) :
            Transform().Matrix.Multiply(new Vector4(0, 0, 0, 1));
        var anchorPos = new Vector2(temp.X, temp.Y);
        if (anchorPos != prevPos)
        {
            _anchor = anchorPos;
            _prevTransMat = Transform().Matrix.Copy();
            _prevAnchorSet = true;
        }
    }

    public void UpdateOutputs()
    {
        if (Param is null) return;

        var oscale = OutputScale;

        // Okay, so this is confusing. We want to translate the angle back to local space,
        // but not the coordinates.

        // Transform the physics output back into local space.
        // The origin here is the anchor. This gives us the local angle.
        Vector4 localPos4;
        localPos4 = LocalOnly ?
        new Vector4(_output.X, _output.Y, 0, 1) :
        (_prevAnchorSet ? _prevTransMat : Transform().Matrix.Copy()).Multiply(new Vector4(_output.X, _output.Y, 0, 1));
        var localAngle = Vector2.Normalize(new Vector2(localPos4.X, localPos4.Y));

        // Figure out the relative length. We can work this out directly in global space.
        var relLength = Vector2.Distance(_output, _anchor) / Length;

        Vector2 paramVal;
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
            default: throw new Exception("mapMode out of range");
        }

        Param.PushIOffset(new Vector2(paramVal.X * oscale.X, paramVal.Y * oscale.Y), ParamMergeMode.Forced);
        Param.Update();
    }

    public override void Reset()
    {
        UpdateInputs();

        _system = ModelType switch
        {
            PhysicsModel.Pendulum => new Pendulum(this),
            PhysicsModel.SpringPendulum => new SpringPendulum(this),
            _ => throw new Exception("modelType error"),
        };
    }

    public override void Dispose()
    {
        _param = Puppet.FindParameter(_paramRef);
        base.Dispose();
        Reset();
    }

    public override void DrawDebug()
    {
        _system.DrawDebug(Matrix4x4.Identity);
    }

    public float GetScale()
    {
        return Puppet.Physics.pixelsPerMeter;
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

    public override string TypeId()
    {
        return "SimplePhysics";
    }

    /// <summary>
    /// Allows serializing self data (with pretty serializer)
    /// </summary>
    /// <param name="serializer"></param>
    /// <param name="recursive"></param>
    protected override void SerializeSelfImpl(JObject serializer, bool recursive = true)
    {
        base.SerializeSelfImpl(serializer, recursive);
        serializer.Add("param", _paramRef);
        serializer.Add("model_type", _modelType);
        serializer.Add("map_mode", MapMode);
        serializer.Add("gravity", _gravity);
        serializer.Add("length", _length);
        serializer.Add("frequency", _frequency);
        serializer.Add("angle_damping", _angleDamping);
        serializer.Add("length_damping", _lengthDamping);
        serializer.Add("output_scale", _outputScale.ToToken());
        serializer.Add("local_only", LocalOnly);
    }

    public override void Deserialize(JObject data)
    {
        base.Deserialize(data);

        var temp = data["param"];
        if (temp != null)
        {
            _paramRef = (uint)temp;
        }

        temp = data["model_type"];
        if (temp != null)
        {
            _modelType = temp.ToString();
        }

        temp = data["map_mode"];
        if (temp != null)
        {
            MapMode = temp.ToString();
        }

        temp = data["gravity"];
        if (temp != null)
        {
            _gravity = (float)temp;
        }

        temp = data["length"];
        if (temp != null)
        {
            _length = (float)temp;
        }

        temp = data["frequency"];
        if (temp != null)
        {
            _frequency = (float)temp;
        }

        temp = data["angle_damping"];
        if (temp != null)
        {
            _angleDamping = (float)temp;
        }

        temp = data["length_damping"];
        if (temp != null)
        {
            _lengthDamping = (float)temp;
        }

        temp = data["output_scale"];
        if (temp != null)
        {
            _outputScale = temp.ToVector2();
        }

        temp = data["local_only"];
        if (temp != null)
        {
            LocalOnly = (bool)temp;
        }
    }
}