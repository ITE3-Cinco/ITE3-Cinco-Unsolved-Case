using System.Collections;
using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    [Header("Jumpscare")]
    public GameObject scareObject;
    public AudioClip scareAudio;
    public float visibleDuration = 1.5f;
    public float audioVolume = 1f;
    public bool onlyOnce = true;

    [Header("Optional")]
    public bool facePlayer = true;

    private bool triggered;

    private void Start()
    {
        if (scareObject != null)
            scareObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (onlyOnce && triggered)
            return;

        triggered = true;
        StartCoroutine(PlayJumpscare(other.transform));
    }

    private IEnumerator PlayJumpscare(Transform player)
    {
        if (scareObject != null)
        {
            scareObject.SetActive(true);

            if (facePlayer)
            {
                Vector3 direction = player.position - scareObject.transform.position;
                direction.y = 0f;

                if (direction.sqrMagnitude > 0.01f)
                    scareObject.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        if (scareAudio != null)
            AudioSource.PlayClipAtPoint(scareAudio, player.position, audioVolume);

        yield return new WaitForSeconds(visibleDuration);

        if (scareObject != null)
            scareObject.SetActive(false);
    }
}