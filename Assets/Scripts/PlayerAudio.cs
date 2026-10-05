using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    public AudioClip footstepClip;
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.28f;
    [Range(0f, 1f)] public float footstepVolume = 0.8f;

    [Header("Caught / Lose")]
    public AudioClip caughtClip;
    [Range(0f, 1f)] public float caughtVolume = 1f;

    private AudioSource audioSource;
    private CharacterController controller;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
        audioSource.mute = false;
        audioSource.priority = 0;
    }

    private void Update()
    {
        if (controller == null || footstepClip == null || Keyboard.current == null)
            return;

        if (!controller.isGrounded)
        {
            stepTimer = 0f;
            return;
        }

        bool moving =
            Keyboard.current.wKey.isPressed ||
            Keyboard.current.aKey.isPressed ||
            Keyboard.current.sKey.isPressed ||
            Keyboard.current.dKey.isPressed ||
            Keyboard.current.upArrowKey.isPressed ||
            Keyboard.current.downArrowKey.isPressed ||
            Keyboard.current.leftArrowKey.isPressed ||
            Keyboard.current.rightArrowKey.isPressed;

        if (!moving)
        {
            stepTimer = 0f;
            return;
        }

        bool running = Keyboard.current.leftShiftKey.isPressed;
        float interval = running ? runStepInterval : walkStepInterval;

        stepTimer -= Time.deltaTime;

        if (stepTimer <= 0f)
        {
            audioSource.PlayOneShot(footstepClip, footstepVolume);
            stepTimer = interval;
        }
    }

    public void PlayCaught()
    {
        if (caughtClip != null)
        {
            audioSource.PlayOneShot(caughtClip, caughtVolume);
        }
        else
        {
            Debug.LogWarning("[PlayerAudio] No caught/scream AudioClip is assigned.");
        }
    }
}
