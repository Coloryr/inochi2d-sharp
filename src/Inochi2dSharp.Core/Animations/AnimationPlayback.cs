namespace Inochi2dSharp.Core.Animations;

public class AnimationPlayback(AnimationPlayer player, Animation anim, string name)
{
    public Animation Anim => anim;
    /// <summary>
    /// Whether this instance is valid
    /// </summary>
    public bool Valid = true;

    /// <summary>
    /// Gets the name of the animation
    /// </summary>
    public string Name => name;

    // Runtime
    private bool _playLeadOut = false;
    private bool _paused = false;
    private bool _playing = false;
    /// <summary>
    /// whether this instance is looping
    /// </summary>
    public bool Looping;

    private bool _stopping;
    private float _time;
    private float _strength = 1;
    private float _speed = 1;
    private int _looped;

    /// <summary>
    /// Gets whether the animation has run to end
    /// </summary>
    public bool Eof => Frame >= Anim.Length;

    /// <summary>
    /// Gets whether this instance is currently playing
    /// </summary>
    public bool Playing => _playing;

    /// <summary>
    /// Gets whether this instance is currently stopping
    /// </summary>
    public bool Stopping => _stopping;

    /// <summary>
    /// Gets whether this instance is currently paused
    /// </summary>
    public bool Paused => _paused;

    /// <summary>
    /// Gets how many times the animation has looped
    /// </summary>
    public int Looped => _looped;

    /// <summary>
    /// Gets or sets the speed multiplier for the animation
    /// </summary>
    public float Speed
    {
        get => _speed;
        set => _speed = float.Clamp(value, 1, 10);
    }

    /// <summary>
    /// Gets or sets the strength multiplier (0..1) for the animation
    /// </summary>
    public float Strength
    {
        get => _strength;
        set => _strength = float.Clamp(value, 0, 1);
    }

    /// <summary>
    /// Gets the current frame of animation
    /// </summary>
    public int Frame => (int)float.Round(_time / Anim.Timestep);

    /// <summary>
    /// Gets the current floating point (half-)frame of animation
    /// </summary>
    public float Hframe => _time / Anim.Timestep;

    /// <summary>
    /// Gets the frame looping ends at
    /// </summary>
    public int LoopPointEnd => HasLeadOut ? Anim.LeadOut : Anim.Length;

    /// <summary>
    /// Gets the frame looping begins at
    /// </summary>
    public int LoopPointBegin => HasLeadIn ? Anim.LeadIn : 0;

    /// <summary>
    /// Gets whether the animation has lead-in
    /// </summary>
    public bool HasLeadIn => Anim.LeadIn > 0 && Anim.LeadIn + 1 < Anim.Length;

    /// <summary>
    /// Gets whether the animation has lead-out
    /// </summary>
    public bool HasLeadOut => Anim.LeadOut > 0 && Anim.LeadOut + 1 < Anim.Length;

    /// <summary>
    /// Gets whether the animation is playing the leadout
    /// </summary>
    public bool IsPlayingLeadOut => ((_playing && !Looping) || _stopping) && _playLeadOut && Frame < Anim.Length;

    /// <summary>
    /// Gets whether the animation is playing the main part or lead out
    /// </summary>
    public bool IsRunning => _playing || IsPlayingLeadOut;

    /// <summary>
    /// Gets the framerate of the animation
    /// </summary>
    public int Fps => (int)(1000.0 / (Anim.Timestep * 1000.0));

    /// <summary>
    /// Gets playback seconds
    /// </summary>
    public int Seconds => (int)_time;

    /// <summary>
    /// Gets playback miliseconds
    /// </summary>
    public int Miliseconds => (int)((_time - Seconds) * 1000);

    /// <summary>
    /// Gets length in frames
    /// </summary>
    public int Frames => Anim.Length;

    public Puppet GetPuppet() { return player.Puppet; }

    /// Gets the playback ID
    public int PlaybackId()
    {
        int idx = -1;
        for (int i = 0; i < player.PlayingAnimations.Count; i++)
        {
            var sanim = player.PlayingAnimations[i];
            if (sanim.Name == Name) idx = i;
        }

        return idx;
    }

    /// <summary>
    /// Destroys this animation instance
    /// </summary>
    public void Destroy()
    {
        Valid = false;
        if (PlaybackId() > -1) player.PlayingAnimations.RemoveAt(PlaybackId());
    }

    /// <summary>
    /// Plays the animation
    /// </summary>
    /// <param name="loop"></param>
    /// <param name="playLeadOut"></param>
    public void Play(bool loop = false, bool playLeadOut = true)
    {
        if (_paused) _paused = false;
        else
        {
            _looped = 0;
            _time = 0;
            _stopping = false;
            _playing = true;
            Looping = loop;
            _playLeadOut = playLeadOut;
        }
    }

    /// <summary>
    /// Pauses the animation
    /// </summary>
    public void Pause()
    {
        _paused = true;
    }

    /// <summary>
    /// Stops the animation
    /// </summary>
    /// <param name="immediate"></param>
    public void Stop(bool immediate = false)
    {
        if (_stopping) return;

        bool shouldStopImmediate = immediate || Frame == 0 || _paused || !HasLeadOut;
        _stopping = !shouldStopImmediate;
        Looping = false;
        _paused = false;
        _playing = false;
        _playLeadOut = !shouldStopImmediate;
        if (shouldStopImmediate)
        {
            _time = 0;
            _looped = 0;
        }
    }

    /// <summary>
    /// Seeks the animation
    /// </summary>
    /// <param name="frame"></param>
    public void Seek(int frame)
    {
        float frameTime = float.Clamp(frame, 0, Frames);
        _time = frameTime * Anim.Timestep;
        _looped = 0;
    }

    /// <summary>
    /// Renders the current frame of animation
    /// <br/>
    /// Called internally automatically by the animation player
    /// </summary>
    public void Render()
    {
        // Apply lanes
        float realStrength = float.Clamp(_strength, 0, 1);
        foreach (var lane in Anim.Lanes)
        {
            lane.ParamRef.TargetParam.PushIOffsetAxis(
                lane.ParamRef.TargetAxis,
                lane.Get(Hframe, player.SnapToFramerate) * realStrength,
                lane.MergeMode
            );
        }
    }

    // Internal functions

    public void Update(float delta)
    {
        if (!Valid || !IsRunning) return;
        if (_paused)
        {
            Render();
            return;
        }

        // Time step
        _time += delta;

        // Handle looping
        if (!IsPlayingLeadOut && Looping && Frame >= LoopPointEnd)
        {
            _time = LoopPointBegin * Anim.Timestep;
            _looped++;
        }

        // Always render the last frame
        if (Frame + 1 >= Frames)
        {
            _time = (Frames - 1) * Anim.Timestep;
        }

        Render();

        // Handle stopping animation completely on lead-out end
        if (!Looping && IsPlayingLeadOut)
        {
            if (Frame + 1 >= Anim.Length)
            {
                _playing = false;
                _playLeadOut = false;
                _stopping = false;
                _time = 0;
                _looped = 0;

                player.AnimStop?.Invoke(name);
            }
        }
    }
}
