using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    public AudioClip footstepClip;
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.24f;
    [Range(0f, 1f)] public float footstepVolume = 0.65f;
    [Tooltip("Playback speed of footsteps while walking.")]
    public float walkFootstepPitch = 1f;
    [Tooltip("Playback speed of footsteps while running/sprinting.")]
    public float runFootstepPitch = 1.18f;

    [Tooltip("Prevents a new footstep from starting while the previous one is still playing.")]
    public bool preventFootstepOverlap = true;

    [Header("Caught / Lose")]
    public AudioClip caughtClip;
    [Range(0f, 1f)] public float caughtVolume = 1f;

    private AudioSource footstepSource;
    private AudioSource caughtSource;
    private CharacterController controller;
    private PlayerMovement movement;
    private float stepTimer;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        AudioSource[] sources = GetComponents<AudioSource>();

        if (sources.Length > 0)
            footstepSource = sources[0];
        else
            footstepSource = gameObject.AddComponent<AudioSource>();

        if (sources.Length > 1)
            caughtSource = sources[1];
        else
            caughtSource = gameObject.AddComponent<AudioSource>();

        ConfigureFootstepSource();
        ConfigureCaughtSource();
    }

    private void ConfigureFootstepSource()
    {
        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.spatialBlend = 0f;
        footstepSource.volume = 1f;
        footstepSource.mute = false;
        footstepSource.priority = 128;
        footstepSource.reverbZoneMix = 0f;
        footstepSource.bypassReverbZones = true;
        footstepSource.bypassEffects = false;
    }

    private void ConfigureCaughtSource()
    {
        caughtSource.outputAudioMixerGroup = footstepSource.outputAudioMixerGroup;
        caughtSource.playOnAwake = false;
        caughtSource.loop = false;
        caughtSource.spatialBlend = 0f;
        caughtSource.volume = 1f;
        caughtSource.mute = false;
        caughtSource.priority = 0;
        caughtSource.reverbZoneMix = 1f;
    }

    private void LateUpdate()
    {
        if (controller == null || !controller.enabled || movement == null || !movement.isActiveAndEnabled ||
            Keyboard.current == null || PauseManager.IsGamePaused || SceneTransition.IsLoading || Time.timeScale <= 0f)
        {
            StopFootsteps();
            return;
        }

        bool grounded = controller.isGrounded;
        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;
        bool moving = IsMovementInputHeld() && horizontalVelocity.sqrMagnitude > 0.01f;

        if (!grounded || !moving || footstepClip == null)
        {
            StopFootsteps();
            return;
        }

        bool running = movement.IsSprinting;
        float interval = running ? runStepInterval : walkStepInterval;
        footstepSource.pitch = running ? runFootstepPitch : walkFootstepPitch;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            if (!preventFootstepOverlap || !footstepSource.isPlaying)
                footstepSource.PlayOneShot(footstepClip, footstepVolume);

            stepTimer = interval;
        }
    }

    private bool IsMovementInputHeld()
    {
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

    private void StopFootsteps()
    {
        stepTimer = 0f;

        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
    }

    public void PlayCaught()
    {
        StopFootsteps();
        if (caughtClip != null)
            caughtSource.PlayOneShot(caughtClip, caughtVolume);
        else
            Debug.LogWarning("[PlayerAudio] No caught/scream AudioClip is assigned.");
    }
}
