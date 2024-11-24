namespace Inochi2dSharp;

/// <summary>
/// Initializes Inochi2D
/// Run this after OpenGL context has been set current
/// </summary>
/// <param name="timeFunc"></param>
internal class I2dTime(Func<float> timeFunc)
{
    private float _currentTime = 0;
    private float _lastTime = 0;
    private float _deltaTime = 0;

    private Func<float> _tfunc = timeFunc;

    public void InSetTimingFunc(Func<float> timeFunc)
    {
        _tfunc = timeFunc;
    }

    /// <summary>
    /// Run this at the start of your render/game loop
    /// </summary>
    public void InUpdate()
    {
        _currentTime = _tfunc();
        _deltaTime = _currentTime - _lastTime;
        _lastTime = _currentTime;
    }

    /// <summary>
    /// Gets the time difference between the last frame and the current frame
    /// </summary>
    /// <returns></returns>
    public float DeltaTime()
    {
        return _deltaTime;
    }

    /// <summary>
    /// Gets the last frame's time step
    /// </summary>
    /// <returns></returns>
    public float LastTime()
    {
        return _lastTime;
    }

    /// <summary>
    /// Gets the current time step
    /// </summary>
    /// <returns></returns>
    public float CurrentTime()
    {
        return _currentTime;
    }
}
