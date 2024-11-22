using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Inochi2dSharp;

public static class Inochi2d
{
    public const string Version = "0.8.7";

    public static double currentTime_ = 0;
    public static double lastTime_ = 0;
    public static double deltaTime_ = 0;
    internal static Func<double> tfunc_;

    /// <summary>
    /// Initializes Inochi2D
    /// Run this after OpenGL context has been set current
    /// </summary>
    /// <param name="timeFunc"></param>
    public static void inInit(Func<double> timeFunc)
    {
        initRenderer();
        tfunc_ = timeFunc;
    }

    public static void inSetTimingFunc(Func<double> timeFunc)
    {
        tfunc_ = timeFunc;
    }

    /// <summary>
    /// Run this at the start of your render/game loop
    /// </summary>
    public static void inUpdate()
    {
        currentTime_ = tfunc_();
        deltaTime_ = currentTime_ - lastTime_;
        lastTime_ = currentTime_;
    }

    /// <summary>
    /// Gets the time difference between the last frame and the current frame
    /// </summary>
    /// <returns></returns>
    public static double deltaTime()
    {
        return deltaTime_;
    }

    /// <summary>
    /// Gets the last frame's time step
    /// </summary>
    /// <returns></returns>
    public static double lastTime()
    {
        return lastTime_;
    }

    /// <summary>
    /// Gets the current time step
    /// </summary>
    /// <returns></returns>
    public static double currentTime()
    {
        return currentTime_;
    }
}
