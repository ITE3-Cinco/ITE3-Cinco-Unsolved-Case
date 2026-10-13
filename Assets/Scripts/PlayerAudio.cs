// Purpose: Plays timed footsteps and a separate caught sound for the player.
// Use: Attach to the player with AudioSource and CharacterController; assign sound clips.
// How it works: Grounded movement input schedules footsteps; Shift selects a shorter running interval.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    // footstepClip: AudioClip played for each movement step. Assigned in the scene/prefab Inspector.
    [Header("Footsteps")]
    public AudioClip footstepClip;
    // walkStepInterval: gameplay seconds between walking footsteps. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float walkStepInterval = 0.45f;
    // runStepInterval: gameplay seconds between Shift-selected footsteps. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    public float runStepInterval = 0.24f;
    // footstepVolume: volume multiplier for each footstep. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Range(0f, 1f)] public float footstepVolume = 0.65f;
    // walkFootstepPitch: Playback speed of footsteps while walking. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Tooltip("Playback speed of footsteps while walking.")]
    public float walkFootstepPitch = 1f;
    // runFootstepPitch: Playback speed of footsteps while running/sprinting. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    [Tooltip("Playback speed of footsteps while running/sprinting.")]
    public float runFootstepPitch = 1.18f;

    // preventFootstepOverlap: Prevents a new footstep from starting while the previous one is still playing.
    // Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Prevents a new footstep from starting while the previous one is still playing.")]
    public bool preventFootstepOverlap = true;

    // caughtClip: AudioClip played when the player is caught. Assigned in the scene/prefab Inspector.
    [Header("Caught / Lose")]
    public AudioClip caughtClip;
    // caughtVolume: volume multiplier for the caught sound. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Range(0f, 1f)] public float caughtVolume = 1f;

    // footstepSource: AudioSource used for steps. Awake retrieves it from a AudioSource component attached to the
    // GameObject that owns this component.
    private AudioSource footstepSource;
    // caughtSource: separate AudioSource used for the caught sound. Awake retrieves it from a AudioSource
    // component attached to the GameObject that owns this component.
    private AudioSource caughtSource;
    // controller: CharacterController cached for collision-aware player motion. Awake retrieves it from the first
    // CharacterController component on this GameObject.
    private CharacterController controller;
    // stepTimer: seconds until the next footstep. Update sets it from Unity gameplay frame duration in seconds.
    private float stepTimer;

    /// <summary>Find the controller and prepare separate audio sources for footsteps and the caught sound.</summary>
    private void Awake()
    {
        // Retrieve CharacterController from this player GameObject; store it in controller for collision-aware
        // movement.
        controller = GetComponent<CharacterController>();

        // Store all AudioSource components on this GameObject in local sources.
        AudioSource[] sources = GetComponents<AudioSource>();

        // If length of sources is above 0, run the following block.
        if (sources.Length > 0)
            // Set footstepSource to sources[0] (entry read from sources).
            footstepSource = sources[0];
        else
            // Store a AudioSource component attached to the GameObject that owns this component in footstepSource.
            footstepSource = gameObject.AddComponent<AudioSource>();

        // If length of sources is above 1, run the following block.
        if (sources.Length > 1)
            // Set caughtSource to sources[1] (entry read from sources).
            caughtSource = sources[1];
        else
            // Store a AudioSource component attached to the GameObject that owns this component in caughtSource.
            caughtSource = gameObject.AddComponent<AudioSource>();

        // Call PlayerAudio.ConfigureFootstepSource; set footsteps to non-looping playback heard directly by the
        // player.
        ConfigureFootstepSource();
        // Call PlayerAudio.ConfigureCaughtSource; set the caught sound to a separate high-priority source.
        ConfigureCaughtSource();
    }

    /// <summary>Set footsteps to non-looping playback heard directly by the player.</summary>
    private void ConfigureFootstepSource()
    {
        // Set footstepSource.playOnAwake to false.
        footstepSource.playOnAwake = false;
        // Set footstepSource.loop to false.
        footstepSource.loop = false;
        // Set footstepSource.spatialBlend to 0.
        footstepSource.spatialBlend = 0f;
        // Set footstepSource.volume to 1.
        footstepSource.volume = 1f;
        // Set footstepSource.mute to false.
        footstepSource.mute = false;
        // Set footstepSource.priority to 128.
        footstepSource.priority = 128;
        // Set footstepSource.reverbZoneMix to 0.
        footstepSource.reverbZoneMix = 0f;
        // Set footstepSource.bypassReverbZones to true.
        footstepSource.bypassReverbZones = true;
        // Set footstepSource.bypassEffects to false.
        footstepSource.bypassEffects = false;
    }

    /// <summary>Set the caught sound to a separate high-priority source.</summary>
    private void ConfigureCaughtSource()
    {
        // Set caughtSource.playOnAwake to false.
        caughtSource.playOnAwake = false;
        // Set caughtSource.loop to false.
        caughtSource.loop = false;
        // Set caughtSource.spatialBlend to 0.
        caughtSource.spatialBlend = 0f;
        // Set caughtSource.volume to 1.
        caughtSource.volume = 1f;
        // Set caughtSource.mute to false.
        caughtSource.mute = false;
        // Set caughtSource.priority to 0.
        caughtSource.priority = 0;
        // Set caughtSource.reverbZoneMix to 1.
        caughtSource.reverbZoneMix = 1f;
    }

    /// <summary>While grounded movement input is held, play footsteps at walking or running intervals.</summary>
    private void Update()
    {
        // Stop this method early when controller is missing or the current Input System keyboard is missing.
        if (controller == null || Keyboard.current == null)
            // Return now; skip the rest of this method.
            return;

        // Store controller floor-contact result from its last Move in local grounded.
        bool grounded = controller.isGrounded;
        // Store the result of PlayerAudio.IsMovementInputHeld in local moving.
        bool moving = IsMovementInputHeld();

        // If not (grounded) or not (moving) or footstepClip is missing, run the following block.
        if (!grounded || !moving || footstepClip == null)
        {
            // Call PlayerAudio.StopFootsteps; reset the step timer and stop any currently playing footstep.
            StopFootsteps();
            // Return now; skip the rest of this method.
            return;
        }

        // Store leftShift key on the current Input System keyboard is held down in local running.
        bool running = Keyboard.current.leftShiftKey.isPressed;
        // Store runStepInterval when running; otherwise walkStepInterval in local interval.
        float interval = running ? runStepInterval : walkStepInterval;

        // Subtract Unity gameplay frame duration in seconds from stepTimer and store the updated value there.
        stepTimer -= Time.deltaTime;

        // If stepTimer is at most 0, run the following block.
        if (stepTimer <= 0f)
        {
            // If not (preventFootstepOverlap) or not (footstepSource.isPlaying), run the following block.
            if (!preventFootstepOverlap || !footstepSource.isPlaying)
                // Play AudioClip footstepClip once through footstepSource at volume multiplier footstepVolume.
                footstepSource.PlayOneShot(footstepClip, footstepVolume);

            // Copy interval into stepTimer.
            stepTimer = interval;
        }
    }

    /// <summary>Return whether any supported WASD or arrow movement key is held.</summary>
    private bool IsMovementInputHeld()
    {
        // Return Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed ||
        // Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed ||
        // Keyboard.current.upArrowKey.isPressed or Keyboard.current.downArrowKey.isPressed or
        // Keyboard.current.leftArrowKey is held down or rightArrow key on the current Input System keyboard is
        // held down to the method caller.
        return
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed;
    }

    /// <summary>Reset the step timer and stop any currently playing footstep.</summary>
    private void StopFootsteps()
    {
        // Set stepTimer to 0.
        stepTimer = 0f;

        // If footstepSource exists and footstepSource.isPlaying, run the following block.
        if (footstepSource != null && footstepSource.isPlaying)
            // Stop playback or activity on footstepSource.
            footstepSource.Stop();
    }

    /// <summary>Play the assigned caught clip, or report that it needs an Inspector assignment.</summary>
    public void PlayCaught()
    {
        // If caughtClip exists, run the following block.
        if (caughtClip != null)
            // Play AudioClip caughtClip once through caughtSource at volume multiplier caughtVolume.
            caughtSource.PlayOneShot(caughtClip, caughtVolume);
        else
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[PlayerAudio] No caught/scream AudioClip is assigned.");
    }
}
