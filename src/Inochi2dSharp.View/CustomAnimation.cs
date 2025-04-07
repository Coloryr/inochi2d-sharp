using Inochi2dSharp.Core.Animations;

namespace Inochi2dSharp.View;

public class CustomAnimation(string name) : Animation
{
    public string Name => name;
}
