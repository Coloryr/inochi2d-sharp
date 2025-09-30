using Inochi2dSharp.Core;
using Inochi2dSharp.Core.Math;

namespace Inochi2dSharp.View;

public interface IRender
{
    void AddPuppet(Puppet model);
    void PostRender(uint fb);
    void PreRender();
    void Render(Puppet puppet, Camera2D cam);
    void SetSize(int width, int height);
}
