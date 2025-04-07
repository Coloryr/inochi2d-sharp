using System.Numerics;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Animations;
using Inochi2dSharp.Core.Nodes;
using Inochi2dSharp.Math;

namespace Inochi2dSharp.View;

public class I2dModel : IDisposable
{
    private readonly Puppet _model;

    private readonly AnimationPlayer _animation;

    private readonly Dictionary<string, CustomAnimation> _customAnimations = [];

    private readonly List<Action<I2dModel, string>> _animationStartEvents = [];
    private readonly List<Action<I2dModel, string>> _animationStopEvents = [];

    /// <summary>
    /// 模型动画开始
    /// </summary>
    public event Action<I2dModel, string> AnimationStart
    {
        add
        {
            _animationStartEvents.Add(value);
        }
        remove
        {
            _animationStartEvents.Remove(value);
        }
    }

    /// <summary>
    /// 模型动画结束
    /// </summary>
    public event Action<I2dModel, string> AnimationStop
    {
        add
        {
            _animationStopEvents.Add(value);
        }
        remove
        {
            _animationStopEvents.Remove(value);
        }
    }

    public I2dModel(string file, Puppet model)
    {
        _model = model;
        _animation = new(model)
        {
            AnimStop = OnStopAnimation
        };

#if DEBUG
        Console.WriteLine($"Model load {file} done");
        Console.WriteLine("Meta:");
        var meta = model.Meta;
        Console.WriteLine($"    Name:           {meta.Name}");
        Console.WriteLine($"    Version:        {meta.Version}");
        Console.WriteLine($"    Rigger:         {meta.Rigger}");
        Console.WriteLine($"    Artist:         {meta.Artist}");
        Console.WriteLine($"    Copyright:      {meta.Copyright}");
        Console.WriteLine($"    LicenseURL:     {meta.LicenseURL}");
        Console.WriteLine($"    Contact:        {meta.Contact}");
        Console.WriteLine($"    Reference:      {meta.Reference}");
        Console.WriteLine($"    ThumbnailId:    {meta.ThumbnailId}");
        Console.WriteLine($"    PreservePixels: {meta.PreservePixels}");
        Console.WriteLine();
        Console.WriteLine("Parts:");
        Console.WriteLine(model.ToString());
        Console.WriteLine("Parameters:");
        Console.WriteLine(model.GetParametersString());
        Console.WriteLine();
        Console.WriteLine("Textures:");
        foreach (var item in model.TextureSlots)
        {
            Console.WriteLine($"    Id: {item.Id} Width: {item.Width} Height: {item.Height}");
        }
#endif
    }

    private ModelPart GenInfo(Node node)
    {
        return new()
        {
            Name = node.Name,
            UUID = node.UUID,
            ZSort = node.ZSort,
            Type = node.TypeId(),
            Children = GetParts(node.Children)
        };
    }

    public List<ModelAnimation> GetAnimations()
    {
        var list = new List<ModelAnimation>();
        foreach (var item in _model.Animations)
        {
            list.Add(new()
            {
                Name = item.Key,
                Length = item.Value.Length,
                LeadIn = item.Value.LeadIn,
                LeadOut = item.Value.LeadOut,
                IsRun = _animation.IsPlay(item.Key)
            });
        }
        foreach (var item in _customAnimations)
        {
            list.Add(new()
            {
                Name = item.Key,
                Length = item.Value.Length,
                LeadIn = item.Value.LeadIn,
                LeadOut = item.Value.LeadOut,
                IsRun = _animation.IsPlay(item.Key)
            });
        }
        return list;
    }

    public List<ModelPart> GetParts(IEnumerable<Node> nodes)
    {
        var list = new List<ModelPart>();
        foreach (var item in nodes)
        {
            list.Add(GenInfo(item));
        }

        return list;
    }

    public ModelPart GetParts()
    {
        return GenInfo(_model.Root);
    }

    public List<ModelParameter> GetParameters()
    {
        var list = new List<ModelParameter>();
        for (int a = 0; a < _model.Parameters.Count; a++)
        {
            var item = _model.Parameters[a];
            list.Add(new()
            {
                Index = a,
                UUID = item.UUID,
                Name = item.Name,
                Min = item.Min,
                Max = item.Max,
                Value = item.Value,
                Default = item.Defaults,
                IsVec2 = item.IsVec2
            });
        }

        return list;
    }

    public void SetParameter(ModelParameter parameter)
    {
        var par = _model.Parameters[parameter.Index];
        if (MathHelper.Contains(par.Min, par.Max, parameter.Value))
        {
            par.Value = parameter.Value;
        }
    }

    public void SetParameter(int index, Vector2 value)
    {
        var par = _model.Parameters[index];
        if (MathHelper.Contains(par.Min, par.Max, value))
        {
            par.Value = value;
        }
    }

    public void SetParameter(uint uuid, Vector2 value)
    {
        var par = _model.FindParameter(uuid);
        if (par == null)
        {
            return;
        }
        if (MathHelper.Contains(par.Min, par.Max, value))
        {
            par.Value = value;
        }
    }

    public void ResetParameter(ModelParameter parameter)
    {
        var par = _model.Parameters[parameter.Index];
        if (par == null)
        {
            return;
        }
        par.Value = par.Defaults;
    }

    public void ResetParameter(uint uuid)
    {
        var par = _model.FindParameter(uuid);
        if (par == null)
        {
            return;
        }
        par.Value = par.Defaults;
    }

    public void ResetParameter(int index)
    {
        var par = _model.Parameters[index];
        if (par == null)
        {
            return;
        }
        par.Value = par.Defaults;
    }

    public void PlayAnimation(string name)
    {
        if (_model.Animations.TryGetValue(name, out var animation))
        {
            _animation.Play(name, animation);
            OnPlayAnimation(name);
        }
        else if (_customAnimations.TryGetValue(name, out var animation1))
        {
            _animation.Play(name, animation1);
            OnPlayAnimation(name);
        }
    }

    public void PauseAnimation(string name)
    {
        _animation.Pause(name);
    }

    public void StopAnimation(string name, bool immediate = false)
    {
        _animation.Stop(name, immediate);
    }

    public void AddCustomAnimation(CustomAnimation animation)
    {
        if (_customAnimations.ContainsKey(animation.Name)
            || _model.Animations.ContainsKey(animation.Name))
        {
            throw new Exception($"Animation name: {animation.Name} is exist");
        }

        _customAnimations.Add(animation.Name, animation);
    }

    public void RemoveCustomAnimation(string name)
    {
        _animation.Stop(name);
        _animation.Remove(name);
        _customAnimations.Remove(name);
    }

    public void Update(float delta)
    {
        _model.Update();
        _animation.Update(delta);
    }

    public void Draw()
    {
        _model.Draw();
    }

    private void OnPlayAnimation(string name)
    {
        Task.Run(() =>
        {
            foreach (var item in _animationStartEvents)
            {
                try
                {
                    item.Invoke(this, name);
                }
                catch (Exception e)
                {

                }
            }
        });
    }

    private void OnStopAnimation(string name)
    {
        Task.Run(() =>
        {
            foreach (var item in _animationStopEvents)
            {
                try
                {
                    item.Invoke(this, name);
                }
                catch (Exception e)
                {

                }
            }
        });
    }

    public void Dispose()
    {
        _animation.StopAll(true);
        _animation.Dispose();
        _model.Dispose();
    }
}
