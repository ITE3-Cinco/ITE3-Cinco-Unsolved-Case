using System.Collections;
using UnityEngine;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }

    [Header("UI Reference")]
    [Tooltip("Drag your NotificationText (TextMeshProUGUI) here")]
    public TextMeshProUGUI notificationText;

    [Header("Settings")]
    [Tooltip("Default duration if no duration is passed to ShowNotification")]
    public float defaultDuration = 3.5f;

    private Coroutine hideCoroutine;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning($"[NotificationManager] Duplicate instance on '{gameObject.name}' destroyed.");
            Destroy(gameObject);
            return;
        }

        // Hide the text at the start so it doesn't show blank on scene load
        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("[NotificationManager] notificationText is not assigned in the Inspector!");
        }
    }

    /// <summary>
    /// Shows a notification message for the given duration. If another notification
    /// is already visible, it is replaced and the timer is reset.
    /// </summary>
    public void ShowNotification(string message, float duration = -1f)
    {
        if (notificationText == null)
        {
            Debug.LogWarning($"[NotificationManager] Tried to show '{message}' but notificationText is null.");
            return;
        }

        if (duration < 0f) duration = defaultDuration;

        notificationText.text = message;
        notificationText.gameObject.SetActive(true);

        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
        }
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    /// <summary>
    /// Manually hide the current notification (useful for cutscenes or menus).
    /// </summary>
    public void HideNotification()
    {
        if (hideCoroutine != null)
        {
            StopCoroutine(hideCoroutine);
            hideCoroutine = null;
        }

        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }
    }

    private IEnumerator HideAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (notificationText != null)
        {
            notificationText.gameObject.SetActive(false);
        }

        hideCoroutine = null;
    }
}