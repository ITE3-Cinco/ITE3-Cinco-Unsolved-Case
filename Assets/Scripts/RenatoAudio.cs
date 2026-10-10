using UnityEngine;

/// <summary>Uses Renato's supplied loops without feeding sounds into his detection.</summary>
[DefaultExecutionOrder(200)]
public class RenatoAudio : MonoBehaviour
{
    public RenatoEnemy enemy;
    public Level3Flow flow;
    public AudioSource whistleSource;
    public AudioSource chaseSource;
    public AudioSource ambienceSource;
    public AudioClip whistleClip;
    public AudioClip chaseClip;
    [Range(0f, 1f)] public float whistleVolume = 0.7f;
    [Range(0f, 1f)] public float chaseVolume = 0.65f;
    [Min(0.1f)] public float fadeDuration = 2f;

    private float ambienceVolume;
    private bool paused;

    private void Awake()
    {
        if (ambienceSource != null) ambienceVolume = ambienceSource.volume;
        if (enemy == null || flow == null || whistleSource == null || chaseSource == null ||
            whistleSource == chaseSource || whistleClip == null || chaseClip == null)
        {
            Debug.LogError("[RenatoAudio] Assign Renato, flow, two distinct sources and the existing audio clips.", this);
            enabled = false;
            return;
        }
        Configure(whistleSource, whistleClip, true);
        Configure(chaseSource, chaseClip, false);
    }

    private static void Configure(AudioSource source, AudioClip clip, bool spatial)
    {
        source.Stop();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.spatialBlend = spatial ? 1f : 0f;
        source.dopplerLevel = 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 2f;
        source.maxDistance = 18f;
    }

    private void LateUpdate()
    {
        // Outcome fades use unscaled time: win/lose deliberately sets timeScale to zero.
        bool finished = flow.Finished;
        bool shouldPause = !finished && (PauseManager.IsGamePaused || Time.timeScale <= 0f);
        if (shouldPause != paused)
        {
            SetPaused(whistleSource, shouldPause);
            SetPaused(chaseSource, shouldPause);
            SetPaused(ambienceSource, shouldPause);
            paused = shouldPause;
        }
        if (paused) return;
        UpdateMix(Time.unscaledDeltaTime);
    }

    private void UpdateMix(float deltaTime)
    {
        bool active = !flow.Finished && enemy.isActiveAndEnabled;
        // AI retains the last sighting briefly. Music falls away once that pursuit ends,
        // without using sound or tracking the player's hidden position.
        bool chasing = active && enemy.State == RenatoEnemy.BehaviourState.Chase;
        float step = deltaTime / Mathf.Max(0.1f, fadeDuration);
        bool walking = active && !chasing && enemy.animations != null &&
            enemy.animations.isActiveAndEnabled && enemy.animations.IsWalking;
        FadeLoop(whistleSource, walking ? whistleVolume : 0f, whistleVolume * step);
        FadeLoop(chaseSource, chasing ? chaseVolume : 0f, chaseVolume * step);
        if (ambienceSource != null)
        {
            float target = flow.Finished ? 0f : ambienceVolume * (chasing ? 0.2f : 1f);
            ambienceSource.volume = Mathf.MoveTowards(ambienceSource.volume, target, ambienceVolume * step);
            if (flow.Finished && ambienceSource.volume <= 0f) ambienceSource.Stop();
        }
    }

    private static void FadeLoop(AudioSource source, float target, float step)
    {
        source.volume = Mathf.MoveTowards(source.volume, target, step);
        if (!Application.isPlaying) return;
        if (source.volume > 0f && !source.isPlaying) source.Play();
        else if (source.volume <= 0f && source.isPlaying) source.Stop();
    }

    private static void SetPaused(AudioSource source, bool pause)
    {
        if (source == null) return;
        if (pause) source.Pause();
        else source.UnPause();
    }

    private void OnDisable()
    {
        if (whistleSource != null) whistleSource.Stop();
        if (chaseSource != null) chaseSource.Stop();
        if (ambienceSource != null)
        {
            if (paused) ambienceSource.UnPause();
            ambienceSource.volume = ambienceVolume;
        }
    }
}
