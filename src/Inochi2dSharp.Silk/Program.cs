using System.Numerics;
using Inochi2dSharp.Silk;
using Inochi2dSharp.View;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.KHR;
using Silk.NET.Windowing;

namespace Inochi2dSharp.Slik;

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
                Version = new APIVersion(4, 2)
            }
        });

        // Declare some variables
        GL gl = null;
        KhrBlendEquationAdvanced khr;
        I2dView view = null;

        // Our loading function
        window.Load += () =>
        {
            khr = new KhrBlendEquationAdvanced(window.GLContext);
            gl = window.CreateOpenGL();
            view = new I2dView(new SilkApi(gl, khr));
            view.SetView(window.Size.X, window.Size.Y);
            var model = view.LoadModel("E:\\temp_code\\example-models\\Midori.inx");
            model.Dispose();
        };

        // Handle resizes
        window.FramebufferResize += s =>
        {
            // Adjust the viewport to the new window size
            gl?.Viewport(s);

            view?.SetView(s.X, s.Y);
        };

        // The render function
        window.Render += delta =>
        {
            view?.Tick((float)delta);
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
