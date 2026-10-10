using System.Collections;
using UnityEngine;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance;

    public TMP_Text notificationText;
    private Coroutine hideCoroutine;

    void Awake()
    {
        // Simple singleton setup for easy access from other scripts
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ShowNotification(string message, float duration = 3.5f)
    {
        if (notificationText == null) return;

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        // Reset timer if a new message pops up while one is already active
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        notificationText.gameObject.SetActive(false);
    }
}