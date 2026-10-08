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

    private AudioSource audioSource;
    private Transform player;
    private GhostEnemy ghost;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        ghost = GetComponent<GhostEnemy>();

        audioSource.playOnAwake = false;
        audioSource.loop = true;
        audioSource.spatialBlend = 0.85f;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.volume = maxVolume;
        audioSource.dopplerLevel = 0f;
        audioSource.priority = 0;
        audioSource.clip = hummingClip;
    }

    private void Start()
    {
        FindPlayer();
        UpdateHumming();
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
            StopHumming();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= maxDistance;
        bool allowed = !playOnlyWhileChasing || (ghost != null && ghost.isChasing);

        if (!inRange || !allowed)
        {
            StopHumming();
            return;
        }

        if (!audioSource.isPlaying)
        {
            audioSource.clip = hummingClip;
            audioSource.Play();
        }

        float closeness = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);
        audioSource.volume = Mathf.Lerp(0.2f, maxVolume, closeness);
    }

    private void StopHumming()
    {
        if (audioSource.isPlaying)
            audioSource.Stop();

        audioSource.volume = 0f;
    }
}
