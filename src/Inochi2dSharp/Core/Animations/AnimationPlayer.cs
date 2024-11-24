namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// Construct animation player
/// </summary>
/// <param name="puppet"></param>
public class AnimationPlayer(Puppet puppet)
{
    public List<AnimationPlayback> PlayingAnimations { get; init; } = [];

    public Puppet Puppet => puppet;

    /// <summary>
    /// Whether to snap to framerate
    /// </summary>
    public bool SnapToFramerate { get; private set; } = false;

    /// <summary>
    /// Run an update step for the animation player
    /// </summary>
    /// <param name="delta"></param>
    public void Update(float delta)
    {
        foreach (var anim in PlayingAnimations)
        {
            if (anim.Valid) anim.Update(delta);
        }
    }

    /// <summary>
    /// Gets an animation
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public AnimationPlayback? CreateOrGet(string name)
    {
        // Try fetching from pre-existing
        foreach (var anim1 in PlayingAnimations)
        {
            if (anim1.Name == name) return anim1;
        }

        // Create new playback
        if (Puppet.Animations.TryGetValue(name, out var anim))
        {
            PlayingAnimations.Add(new AnimationPlayback(this, anim, name));
            return PlayingAnimations[^1];
        }

        // Invalid state
        return null;
    }

    /// <summary>
    /// Convenience function which plays an animation
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public AnimationPlayback? Play(string name)
    {
        var anim = CreateOrGet(name);
        anim?.Play();

        return anim;
    }

    /// <summary>
    /// pre-render one frame of all animations
    /// </summary>
    public void PrerenderAll()
    {
        foreach (var anim in PlayingAnimations)
        {
            anim.Render();
        }
    }

    /// <summary>
    /// Stop all animations
    /// </summary>
    /// <param name="immediate"></param>
    public void StopAll(bool immediate = false)
    {
        foreach (var anim in PlayingAnimations)
        {
            anim.Stop(immediate);
        }
    }

    /// <summary>
    /// Destroy all animations
    /// </summary>
    public void DestroyAll()
    {
        foreach (var anim in PlayingAnimations)
        {
            anim.Valid = false;
        }
        PlayingAnimations.Clear();
    }
}
