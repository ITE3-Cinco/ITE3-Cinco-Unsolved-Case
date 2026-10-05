using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class PlayerAudio : MonoBehaviour
{
    [Header("Footsteps")]
    public AudioClip footstepClip;
    public float walkStepInterval = 0.45f;
    public float runStepInterval = 0.28f;
    public float footstepVolume = 0.65f;

    [Header("Caught / Lose")]
    public AudioClip caughtClip;
    public float caughtVolume = 1f;

    private AudioSource audioSource;
    private CharacterController controller;
    private PlayerMovement movement;
    private float stepTimer;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        controller = GetComponent<CharacterController>();
        movement = GetComponent<PlayerMovement>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }

    private void Update()
    {
        if (movement == null || controller == null || footstepClip == null)
            return;

        Vector3 horizontalVelocity = controller.velocity;
        horizontalVelocity.y = 0f;

        bool moving = horizontalVelocity.sqrMagnitude > 0.05f && controller.isGrounded;

        if (!moving)
        {
            stepTimer = 0f;
            return;
        }

        bool running = UnityEngine.InputSystem.Keyboard.current != null &&
                       UnityEngine.InputSystem.Keyboard.current.leftShiftKey.isPressed;

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
            audioSource.PlayOneShot(caughtClip, caughtVolume);
    }
}