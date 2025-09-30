using Inochi2dSharp.View;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.KHR;
using Silk.NET.Windowing;

namespace Inochi2dSharp.OpenGL.Silk;

internal class Program
{
    static void Main(string[] args)
    {
        // Create a Silk.NET window as usual
        using var window = Window.Create(WindowOptions.Default
            with
        {
            Size = new(600, 600),
            API = GraphicsAPI.Default with
            {
                Flags = ContextFlags.ForwardCompatible,
                Profile = ContextProfile.Compatability,
                Version = new APIVersion(4, 6)
            }
        });

        // Declare some variables
        GL gl = null;
        KhrBlendEquationAdvanced khr;
        I2dView view = null;

        I2dModel model;

        // Our loading function
        window.Load += () =>
        {
            khr = new KhrBlendEquationAdvanced(window.GLContext);
            gl = window.CreateOpenGL();
            var render = new Inochi2dGL(new SilkApi(gl, khr), window.Size.X, window.Size.Y);
            view = new I2dView(render, window.Size.X, window.Size.Y, 0.1f);
            model = view.LoadModel("E:\\temp_code\\example-models\\Aka.inx");
            //var parts = model.GetParts();
            //var pars = model.GetParameters();
            //var anima = model.GetAnimations();
            //var par = pars.First();
            //model.SetParameter(par.Index, new(1, 0));

            //model.PlayAnimation(anima.First().Name);
        };

        // Handle resizes
        window.FramebufferResize += s =>
        {
            // Adjust the viewport to the new window size
            gl?.Viewport(s);

            view?.SetSize(s.X, s.Y);
        };

        // The render function
        window.Render += delta =>
        {
            view?.Tick((float)delta, 0);
        };

        // The closing function
        window.Closing += () =>
        {
            view?.Dispose();
            // Unload OpenGL
            gl?.Dispose();
        };

        // Now that everything's defined, let's run this bad boy!
        window.Run();

        window.Dispose();
    }
}
