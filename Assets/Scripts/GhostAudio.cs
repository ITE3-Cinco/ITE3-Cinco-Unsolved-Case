using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GhostAudio : MonoBehaviour
{
    [Header("Jump Scares")]
    public AudioClip screamClip;

    public static void PlayScare(AudioClip clip, Vector3 position, float volume)
    {
        if (clip == null)
        {
            GhostAudio ghostAudio = FindAnyObjectByType<GhostAudio>();
            if (ghostAudio != null) clip = ghostAudio.screamClip;
        }
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    [Header("Humming")]
    public AudioClip hummingClip;
    [Range(0f, 1f)] public float maxVolume = 1f;
    public float maxDistance = 40f;
    public float minDistance = 6f;
    public bool playOnlyWhileChasing = false;
    [Tooltip("How quickly the humming volume fades in and out (units per second). Higher = snappier transitions.")]
    [Min(0.01f)] public float volumeFadeSpeed = 1.5f;

    private AudioSource audioSource;
    private Transform player;
    private GhostEnemy ghost;
    private float currentVolume = 0f;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ghost = GetComponent<GhostEnemy>();

        // Explicitly disable play-on-awake so the humming clip does not fire
        // before the audio system has been configured (the scene-serialized
        // value may still be true when the component first wakes up).
        audioSource.playOnAwake = false;
        audioSource.Stop();

        audioSource.loop = true;
        audioSource.spatialBlend = 0.85f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.volume = 0f;
        audioSource.dopplerLevel = 0f;
        audioSource.priority = 0;
        audioSource.clip = hummingClip;
    }

    private void Start()
    {
        FindPlayer();
    }

    private void Update()
    {
        if (player == null)
            FindPlayer();

        UpdateHumming();
    }

    private void FindPlayer()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
            player = playerObject.transform;
    }

    private void UpdateHumming()
    {
        if (hummingClip == null || player == null)
        {
            FadeVolumeTo(0f);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= maxDistance;
        bool allowed = (ghost == null || ghost.IsHostile) &&
            (!playOnlyWhileChasing || (ghost != null && ghost.isChasing));

        float targetVolume;
        if (!inRange || !allowed)
        {
            targetVolume = 0f;
        }
        else
        {
            float closeness = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);
            targetVolume = Mathf.Lerp(0.2f, maxVolume, closeness);
        }

        FadeVolumeTo(targetVolume);

        // Start or stop the AudioSource based on whether we want any sound.
        if (currentVolume > 0.001f && !audioSource.isPlaying)
        {
            audioSource.clip = hummingClip;
            audioSource.Play();
        }
        else if (currentVolume <= 0.001f && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    /// <summary>Smoothly moves the AudioSource volume toward <paramref name="target"/>.</summary>
    private void FadeVolumeTo(float target)
    {
        currentVolume = Mathf.MoveTowards(currentVolume, target, volumeFadeSpeed * Time.deltaTime);
        audioSource.volume = currentVolume;
    }
}
