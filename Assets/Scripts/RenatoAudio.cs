// Purpose: Crossfades Renato whistle, chase music and the background ambience.
// Use: Attach to Renato and assign the enemy, flow, separate sources and supplied clips.
// How it works: Enemy state selects music; chase lowers ambience and completed outcomes fade sounds to silence.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

/// <summary>Uses Renato's supplied loops without feeding sounds into his detection.</summary>
[DefaultExecutionOrder(200)]
public class RenatoAudio : MonoBehaviour
{
    // enemy: RenatoEnemy supplying patrol/chase state to audio. Assigned in the scene/prefab Inspector.
    public RenatoEnemy enemy;
    // flow: Level3Flow coordinating key, catch and escape outcomes. Assigned in the scene/prefab Inspector.
    public Level3Flow flow;
    // whistleSource: looping positional AudioSource for Renato patrol whistle. Assigned in the scene/prefab
    // Inspector.
    public AudioSource whistleSource;
    // chaseSource: non-positional AudioSource for pursuit music. Assigned in the scene/prefab Inspector.
    public AudioSource chaseSource;
    // ambienceSource: background AudioSource lowered during pursuit. Assigned in the scene/prefab Inspector.
    public AudioSource ambienceSource;
    // whistleClip: patrol whistle AudioClip. Assigned in the scene/prefab Inspector.
    public AudioClip whistleClip;
    // chaseClip: pursuit music AudioClip. Assigned in the scene/prefab Inspector.
    public AudioClip chaseClip;
    // whistleVolume: target whistle loudness. Assigned in the scene/prefab Inspector; the initializer below is the
    // default.
    [Range(0f, 1f)] public float whistleVolume = 0.7f;
    // chaseVolume: target pursuit loudness. Assigned in the scene/prefab Inspector; the initializer below is the
    // default.
    [Range(0f, 1f)] public float chaseVolume = 0.65f;
    // fadeDuration: seconds used to crossfade patrol and chase audio. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Min(0.1f)] public float fadeDuration = 2f;

    // ambienceVolume: starting ambience volume cached for restoration. Awake sets it from ambienceSource.volume.
    private float ambienceVolume;
    // paused: whether this audio controller has paused playback. LateUpdate sets it from shouldPause.
    private bool paused;

    /// <summary>Remember ambience volume, validate references and configure the two enemy loops.</summary>
    private void Awake()
    {
        // If ambienceSource exists, set ambienceVolume to ambienceSource.volume.
        if (ambienceSource != null) ambienceVolume = ambienceSource.volume;
        // If enemy == null || flow == null || whistleSource == null || chaseSource == null or whistleSource ==
        // chaseSource or whistleClip is missing or chaseClip is missing, run the following block.
        if (enemy == null || flow == null || whistleSource == null || chaseSource == null ||
            whistleSource == chaseSource || whistleClip == null || chaseClip == null)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[RenatoAudio] Assign Renato, flow, two distinct sources and the existing audio clips.", this);
            // Set enabled to false.
            enabled = false;
            // Return now; skip the rest of this method.
            return;
        }
        // Call RenatoAudio.Configure with whistleSource, whistleClip, true; prepare one looping source, using
        // distance-based sound for the whistle.
        Configure(whistleSource, whistleClip, true);
        // Call RenatoAudio.Configure with chaseSource, chaseClip, false; prepare one looping source, using
        // distance-based sound for the whistle.
        Configure(chaseSource, chaseClip, false);
    }

    /// <summary>Prepare one looping source, using distance-based sound for the whistle.</summary>
    // source is a AudioSource argument supplied by RenatoAudio.Awake.
    // clip is a AudioClip argument supplied by RenatoAudio.Awake.
    // spatial is a bool argument supplied by RenatoAudio.Awake.
    private static void Configure(AudioSource source, AudioClip clip, bool spatial)
    {
        // Stop playback or activity on source.
        source.Stop();
        // Copy clip (argument from the caller) into source.clip.
        source.clip = clip;
        // Set source.playOnAwake to false.
        source.playOnAwake = false;
        // Set source.loop to true.
        source.loop = true;
        // Set source.volume to 0.
        source.volume = 0f;
        // Set source.spatialBlend to 1 when spatial; otherwise 0.
        source.spatialBlend = spatial ? 1f : 0f;
        // Set source.dopplerLevel to 0.
        source.dopplerLevel = 0f;
        // Set source.rolloffMode to AudioRolloffMode.Linear.
        source.rolloffMode = AudioRolloffMode.Linear;
        // Set source.minDistance to 2.
        source.minDistance = 2f;
        // Set source.maxDistance to 18.
        source.maxDistance = 18f;
    }

    /// <summary>Pause audio with gameplay, then update fades using real time for frozen outcomes.</summary>
    private void LateUpdate()
    {
        // Outcome fades use unscaled time: win/lose deliberately sets timeScale to zero.
        // Store flow outcome guard stored by Level3Flow.Finish in local finished.
        bool finished = flow.Finished;
        // Store not (finished) and (PauseManager.IsGamePaused or Time.timeScale <= 0f) in local shouldPause.
        bool shouldPause = !finished && (PauseManager.IsGamePaused || Time.timeScale <= 0f);
        // If shouldPause differs from paused, run the following block.
        if (shouldPause != paused)
        {
            // Call RenatoAudio.SetPaused with whistleSource, shouldPause; pause or resume an assigned source
            // without restarting its clip.
            SetPaused(whistleSource, shouldPause);
            // Call RenatoAudio.SetPaused with chaseSource, shouldPause; pause or resume an assigned source without
            // restarting its clip.
            SetPaused(chaseSource, shouldPause);
            // Call RenatoAudio.SetPaused with ambienceSource, shouldPause; pause or resume an assigned source
            // without restarting its clip.
            SetPaused(ambienceSource, shouldPause);
            // Copy shouldPause into paused.
            paused = shouldPause;
        }
        // If paused, return now; skip the rest of this method.
        if (paused) return;
        // Call RenatoAudio.UpdateMix with Unity real frame duration in seconds (ignores pause); choose whistle or
        // chase levels from enemy state and reduce ambience during pursuit.
        UpdateMix(Time.unscaledDeltaTime);
    }

    /// <summary>Choose whistle or chase levels from enemy state and reduce ambience during pursuit.</summary>
    // deltaTime is a float argument supplied by RenatoAudio.LateUpdate.
    private void UpdateMix(float deltaTime)
    {
        // Store not (flow outcome guard stored by Level3Flow.Finish) and enemy.isActiveAndEnabled in local active.
        bool active = !flow.Finished && enemy.isActiveAndEnabled;
        // AI retains the last sighting briefly. Music falls away once that pursuit ends,
        // without using sound or tracking the player's hidden position.
        // Store active and enemy.State equals RenatoEnemy.BehaviourState.Chase in local chasing.
        bool chasing = active && enemy.State == RenatoEnemy.BehaviourState.Chase;
        // Convert elapsed time to a fade fraction so volume changes take the configured duration.
        // Store deltaTime divided by the largest of 0.1f, fadeDuration in local step.
        float step = deltaTime / Mathf.Max(0.1f, fadeDuration);
        // Store active && !chasing and enemy.animations != null and enemy.animations.isActiveAndEnabled and
        // enemy.animations.IsWalking in local walking.
        bool walking = active && !chasing && enemy.animations != null &&
            enemy.animations.isActiveAndEnabled && enemy.animations.IsWalking;
        // Call RenatoAudio.FadeLoop with whistleSource, whistleVolume when walking; otherwise 0, whistleVolume *
        // step; gradually change a loop volume and start or stop playback as needed.
        FadeLoop(whistleSource, walking ? whistleVolume : 0f, whistleVolume * step);
        // Call RenatoAudio.FadeLoop with chaseSource, chaseVolume when chasing; otherwise 0, chaseVolume * step;
        // gradually change a loop volume and start or stop playback as needed.
        FadeLoop(chaseSource, chasing ? chaseVolume : 0f, chaseVolume * step);
        // If ambienceSource exists, run the following block.
        if (ambienceSource != null)
        {
            // Store 0 when flow outcome guard stored by Level3Flow.Finish; otherwise ambienceVolume multiplied by
            // (chasing ? 0.2f : 1f) in local target.
            float target = flow.Finished ? 0f : ambienceVolume * (chasing ? 0.2f : 1f);
            // Store ambienceSource.volume moved toward target by at most ambienceVolume * step this step in
            // ambienceSource.volume.
            ambienceSource.volume = Mathf.MoveTowards(ambienceSource.volume, target, ambienceVolume * step);
            // If flow outcome guard stored by Level3Flow.Finish and ambienceSource.volume is at most 0, stop
            // playback or activity on ambienceSource.
            if (flow.Finished && ambienceSource.volume <= 0f) ambienceSource.Stop();
        }
    }

    /// <summary>Gradually change a loop volume and start or stop playback as needed.</summary>
    // source is a AudioSource argument supplied by RenatoAudio.UpdateMix.
    // target is a float argument supplied by RenatoAudio.UpdateMix.
    // step is a float argument supplied by RenatoAudio.UpdateMix.
    private static void FadeLoop(AudioSource source, float target, float step)
    {
        // Store source.volume moved toward target by at most step this step in source.volume.
        source.volume = Mathf.MoveTowards(source.volume, target, step);
        // If not (Application.isPlaying), return now; skip the rest of this method.
        if (!Application.isPlaying) return;
        // If source.volume is above 0 and not (source.isPlaying), start/resume playback on source.
        if (source.volume > 0f && !source.isPlaying) source.Play();
        else if (source.volume <= 0f && source.isPlaying) source.Stop();
    }

    /// <summary>Pause or resume an assigned source without restarting its clip.</summary>
    // source is a AudioSource argument supplied by RenatoAudio.LateUpdate.
    // pause is a bool argument supplied by RenatoAudio.LateUpdate.
    private static void SetPaused(AudioSource source, bool pause)
    {
        // If source is missing, return now; skip the rest of this method.
        if (source == null) return;
        // If pause, pause source without resetting its playback position.
        if (pause) source.Pause();
        else source.UnPause();
    }

    /// <summary>Stop enemy loops and restore the background ambience settings.</summary>
    private void OnDisable()
    {
        // If whistleSource exists, stop playback or activity on whistleSource.
        if (whistleSource != null) whistleSource.Stop();
        // If chaseSource exists, stop playback or activity on chaseSource.
        if (chaseSource != null) chaseSource.Stop();
        // If ambienceSource exists, run the following block.
        if (ambienceSource != null)
        {
            // If paused, resume ambienceSource from its paused playback position.
            if (paused) ambienceSource.UnPause();
            // Copy ambienceVolume cached by this component into ambienceSource.volume.
            ambienceSource.volume = ambienceVolume;
        }
    }
}
