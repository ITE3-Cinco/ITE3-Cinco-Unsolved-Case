using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GhostAudio : MonoBehaviour
{
    [Header("Humming")]
    public AudioClip hummingClip;
    public float maxVolume = 0.7f;
    public float maxDistance = 12f;
    public float minDistance = 2f;
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
        audioSource.spatialBlend = 1f;
        audioSource.minDistance = minDistance;
        audioSource.maxDistance = maxDistance;
        audioSource.clip = hummingClip;
        audioSource.volume = 0f;
    }

    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
            player = playerObject.transform;
    }

    private void Update()
    {
        if (hummingClip == null || player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool inRange = distance <= maxDistance;
        bool allowed = !playOnlyWhileChasing || (ghost != null && ghost.isChasing);

        if (inRange && allowed)
        {
            if (!audioSource.isPlaying)
                audioSource.Play();

            float closeness = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);
            audioSource.volume = closeness * maxVolume;
        }
        else
        {
            audioSource.volume = 0f;
            if (audioSource.isPlaying)
                audioSource.Stop();
        }
    }
}