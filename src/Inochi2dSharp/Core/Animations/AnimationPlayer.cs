

namespace Inochi2dSharp.Core.Animations;

/// <summary>
/// Construct animation player
/// </summary>
/// <param name="puppet"></param>
public class AnimationPlayer(Puppet puppet)
{
    public List<AnimationPlayback> PlayingAnimations = [];

    public Puppet Puppet => puppet;

    /// <summary>
    /// Whether to snap to framerate
    /// </summary>
    public bool SnapToFramerate = false;

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
            var item = new AnimationPlayback(this, anim, name);
            PlayingAnimations.Add(item);
            return item;
        }

        // Invalid state
        return null;
    }

    /// <summary>
    /// Play a custom animation
    /// </summary>
    /// <param name="name"></param>
    /// <param name="animation"></param>
    /// <returns></returns>
    public void Play(string name, Animation animation)
    {
        foreach (var anim1 in PlayingAnimations)
        {
            if (anim1.Name == name)
            {
                anim1.Play();
            }
        }

         PlayingAnimations.Add(new AnimationPlayback(this, animation, name));
    }

    /// <summary>
    /// Convenience function which plays an animation
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public void Play(string name)
    {
        var anim = CreateOrGet(name);
        anim?.Play();
    }

    public void Pause(string name)
    {
        foreach (var anim1 in PlayingAnimations)
        {
            if (anim1.Name == name)
            {
                anim1.Pause();
            }
        }
    }

    public void Stop(string name, bool immediate = false)
    {
        foreach (var anim1 in PlayingAnimations)
        {
            if (anim1.Name == name)
            {
                anim1.Stop(immediate);
            }
        }
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
    public void Dispose()
    {
        foreach (var anim in PlayingAnimations)
        {
            anim.Valid = false;
        }
        PlayingAnimations.Clear();
    }

    public bool IsPlay(string name)
    {
        foreach (var anim in PlayingAnimations)
        {
            if (anim.Name == name)
            {
                return anim.IsRunning;
            }
        }

        return false;
    }

    public void Remove(string name)
    {
        PlayingAnimations.RemoveAll(item => item.Name == name);
    }
}
