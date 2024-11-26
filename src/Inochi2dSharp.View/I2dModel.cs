using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Animations;

namespace Inochi2dSharp.View;

public class I2dModel : IDisposable
{
    private readonly Puppet _model;

    private readonly AnimationPlayer _animation;

    public I2dModel(string file, Puppet model)
    {
        _model = model;
        _animation = new(model);

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

    public void Update(float delta)
    {
        _model.Update();
        _animation.Update(delta);
    }

    public void Draw()
    {
        _animation.PrerenderAll();
        _model.Draw();
    }

    public void Dispose()
    {
        _animation.StopAll(true);
        _animation.Dispose();
        _model.Dispose();
    }
}
