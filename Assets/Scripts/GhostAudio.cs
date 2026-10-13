// Purpose: Plays ghost humming based on distance and supplies one-shot scare audio.
// Use: Attach beside GhostEnemy and AudioSource; assign humming and scream clips.
// How it works: Distance chooses the target volume; gradual fading avoids sudden humming changes.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GhostAudio : MonoBehaviour
{
    // screamClip: scare sound. Assigned in the scene/prefab Inspector.
    [Header("Jump Scares")]
    public AudioClip screamClip;

    /// <summary>Play a supplied scream at a position, or use the scene ghost scream as a fallback.</summary>
    // clip is a AudioClip argument supplied by ArtifactCornerScare.PlayScare.
    // position is a Vector3 argument supplied by ArtifactCornerScare.PlayScare.
    // volume is a float argument supplied by ArtifactCornerScare.PlayScare.
    public static void PlayScare(AudioClip clip, Vector3 position, float volume)
    {
        // If clip is missing, run the following block.
        if (clip == null)
        {
            // Store a scene GhostAudio component in local ghostAudio.
            GhostAudio ghostAudio = FindAnyObjectByType<GhostAudio>();
            // If ghostAudio exists, set clip to ghostAudio.screamClip.
            if (ghostAudio != null) clip = ghostAudio.screamClip;
        }
        // If clip exists, play AudioClip clip at position with volume volume.
        if (clip != null) AudioSource.PlayClipAtPoint(clip, position, volume);
    }

    // hummingClip: looping AudioClip used for the ghost distance warning. Assigned in the scene/prefab Inspector.
    [Header("Humming")]
    public AudioClip hummingClip;
    // maxVolume: maximum humming loudness used near the ghost. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Range(0f, 1f)] public float maxVolume = 1f;
    // maxDistance: maximum humming distance and AudioSource attenuation range. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public float maxDistance = 40f;
    // minDistance: distance within which humming reaches its strongest configured volume. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public float minDistance = 6f;
    // playOnlyWhileChasing: whether humming is restricted to an active pursuit. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public bool playOnlyWhileChasing = false;
    // volumeFadeSpeed: How quickly the humming volume fades in and out (units per second). Higher = snappier
    // transitions. Assigned in the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("How quickly the humming volume fades in and out (units per second). Higher = snappier transitions.")]
    [Min(0.01f)] public float volumeFadeSpeed = 1.5f;

    // audioSource: AudioSource configured and controlled by this component. Awake retrieves it from the first
    // AudioSource component on this GameObject.
    private AudioSource audioSource;
    // player: player reference used by this script. FindPlayer sets it from Transform of playerObject.
    private Transform player;
    // ghost: GhostEnemy reference supplying state/configuration. Awake retrieves it from the first GhostEnemy
    // component on this GameObject.
    private GhostEnemy ghost;
    // currentVolume: current humming loudness. Updated by FadeVolumeTo.
    private float currentVolume = 0f;

    /// <summary>Configure the looping humming source silently before any playback starts.</summary>
    private void Awake()
    {
        // Store the first AudioSource component on this GameObject in audioSource.
        audioSource = GetComponent<AudioSource>();
        // Store the first GhostEnemy component on this GameObject in ghost.
        ghost = GetComponent<GhostEnemy>();

        // Explicitly disable play-on-awake so the humming clip does not fire
        // before the audio system has been configured (the scene-serialized
        // value may still be true when the component first wakes up).
        // Set audioSource.playOnAwake to false.
        audioSource.playOnAwake = false;
        // Stop playback or activity on audioSource.
        audioSource.Stop();

        // Set audioSource.loop to true.
        audioSource.loop = true;
        // Set audioSource.spatialBlend to 0.85.
        audioSource.spatialBlend = 0.85f;
        // Set audioSource.rolloffMode to AudioRolloffMode.Logarithmic.
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        // Copy minDistance from this component into audioSource.minDistance.
        audioSource.minDistance = minDistance;
        // Copy maxDistance from this component into audioSource.maxDistance.
        audioSource.maxDistance = maxDistance;
        // Set audioSource.volume to 0.
        audioSource.volume = 0f;
        // Set audioSource.dopplerLevel to 0.
        audioSource.dopplerLevel = 0f;
        // Set audioSource.priority to 0.
        audioSource.priority = 0;
        // Copy hummingClip from this component into audioSource.clip.
        audioSource.clip = hummingClip;
    }

    /// <summary>Find the Player-tagged object when the scene begins.</summary>
    private void Start()
    {
        // Call GhostAudio.FindPlayer; locate the tagged player so distance can control the sound.
        FindPlayer();
    }

    /// <summary>Recover a missing player reference and update humming each frame.</summary>
    private void Update()
    {
        // If player is missing, run the following block.
        if (player == null)
            // Call GhostAudio.FindPlayer; locate the tagged player so distance can control the sound.
            FindPlayer();

        // Call GhostAudio.UpdateHumming; choose volume from range, hostility and chase settings, then start or
        // stop the loop.
        UpdateHumming();
    }

    /// <summary>Locate the tagged player so distance can control the sound.</summary>
    private void FindPlayer()
    {
        // Store the scene GameObject tagged "Player" in local playerObject.
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        // If playerObject exists, run the following block.
        if (playerObject != null)
            // Set player to Transform of playerObject.
            player = playerObject.transform;
    }

    /// <summary>Choose volume from range, hostility and chase settings, then start or stop the loop.</summary>
    private void UpdateHumming()
    {
        // If hummingClip is missing or player is missing, run the following block.
        if (hummingClip == null || player == null)
        {
            // Call GhostAudio.FadeVolumeTo with 0; smoothly moves the AudioSource volume toward <paramref
            // name="target"/>.
            FadeVolumeTo(0f);
            // Return now; skip the rest of this method.
            return;
        }

        // Store the distance between world position of transform and world position of player in local distance.
        float distance = Vector3.Distance(transform.position, player.position);
        // Store distance is at most maxDistance in local inRange.
        bool inRange = distance <= maxDistance;
        // Store (ghost == null or ghost.IsHostile) and (!playOnlyWhileChasing or (ghost != null &&
        // ghost.isChasing)) in local allowed.
        bool allowed = (ghost == null || ghost.IsHostile) &&
            (!playOnlyWhileChasing || (ghost != null && ghost.isChasing));

        // Declare local targetVolume to hold float data supplied later in this method.
        float targetVolume;
        // If not (inRange) or not (allowed), run the following block.
        if (!inRange || !allowed)
        {
            // Set targetVolume to 0.
            targetVolume = 0f;
        }
        else
        {
            // Store 1f - Mathf.InverseLerp(minDistance, maxDistance, distance) (calculated from the values shown)
            // in local closeness.
            float closeness = 1f - Mathf.InverseLerp(minDistance, maxDistance, distance);
            // Store a blend from 0.2 to maxVolume using fraction closeness in targetVolume.
            targetVolume = Mathf.Lerp(0.2f, maxVolume, closeness);
        }

        // Call GhostAudio.FadeVolumeTo with targetVolume; smoothly moves the AudioSource volume toward <paramref
        // name="target"/>.
        FadeVolumeTo(targetVolume);

        // Start or stop the AudioSource based on whether we want any sound.
        // If currentVolume is above 0.001 and not (audioSource.isPlaying), run the following block.
        if (currentVolume > 0.001f && !audioSource.isPlaying)
        {
            // Copy hummingClip from this component into audioSource.clip.
            audioSource.clip = hummingClip;
            // Start/resume playback on audioSource.
            audioSource.Play();
        }
        else if (currentVolume <= 0.001f && audioSource.isPlaying)
        {
            // Stop playback or activity on audioSource.
            audioSource.Stop();
        }
    }

    /// <summary>Smoothly moves the AudioSource volume toward <paramref name="target"/>.</summary>
    // Move the humming volume toward its target at a steady rate per second.
    // target is a float argument supplied by GhostAudio.UpdateHumming.
    private void FadeVolumeTo(float target)
    {
        // Store currentVolume moved toward target by at most volumeFadeSpeed multiplied by Unity gameplay frame
        // duration in seconds this step in currentVolume.
        currentVolume = Mathf.MoveTowards(currentVolume, target, volumeFadeSpeed * Time.deltaTime);
        // Copy currentVolume cached by this component into audioSource.volume.
        audioSource.volume = currentVolume;
    }
}
