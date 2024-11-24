using Inochi2dSharp.Core.Automations;
using Inochi2dSharp.Core.Nodes;

namespace Inochi2dSharp;

public static class Inochi2d
{
    public const string Version = "0.8.7";

    public static float currentTime_ = 0;
    public static float lastTime_ = 0;
    public static float deltaTime_ = 0;
    internal static Func<float> tfunc_;

    public static void Init()
    {
        AutomationHelper.Init();
        NodeHelper.Init();
    }

    /// <summary>
    /// Initializes Inochi2D
    /// Run this after OpenGL context has been set current
    /// </summary>
    /// <param name="timeFunc"></param>
    public static void inInit(Func<float> timeFunc)
    {
        initRenderer();
        tfunc_ = timeFunc;
    }

    public static void inSetTimingFunc(Func<float> timeFunc)
    {
        tfunc_ = timeFunc;
    }

    /// <summary>
    /// Run this at the start of your render/game loop
    /// </summary>
    public static void InUpdate()
    {
        currentTime_ = tfunc_();
        deltaTime_ = currentTime_ - lastTime_;
        lastTime_ = currentTime_;
    }

    /// <summary>
    /// Gets the time difference between the last frame and the current frame
    /// </summary>
    /// <returns></returns>
    public static float deltaTime()
    {
        return deltaTime_;
    }

    /// <summary>
    /// Gets the last frame's time step
    /// </summary>
    /// <returns></returns>
    public static float lastTime()
    {
        return lastTime_;
    }

    /// <summary>
    /// Gets the current time step
    /// </summary>
    /// <returns></returns>
    public static float currentTime()
    {
        return currentTime_;
    }
}
