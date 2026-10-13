// Purpose: Displays short messages for pickups and checkpoint updates.
// Use: Attach to a UI manager and assign the notification TMP text.
// How it works: ShowNotification replaces the text and restarts a coroutine that hides it after a delay.

// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;

public class NotificationManager : MonoBehaviour
{
    // NotificationManager.Instance is the shared reference set to this component during initialization; other
    // scripts read it to find this manager.
    public static NotificationManager Instance;

    // notificationText: TMP text that receives temporary notification messages. Assigned in the scene/prefab
    // Inspector.
    public TMP_Text notificationText;
    // hideCoroutine: running notification timer replaced when another message arrives. Updated by
    // ShowNotification.
    private Coroutine hideCoroutine;

    /// <summary>Register one shared notification manager for other scene scripts to call.</summary>
    void Awake()
    {
        // Simple singleton setup for easy access from other scripts
        // If Instance is missing, register this component in the class shared Instance field so other scripts can
        // locate it without repeated scene searches.
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>Show a new message and restart its hide timer so it receives its full display time.</summary>
    // message is display text supplied by a pickup, checkpoint or story/outcome caller.
    // duration is the caller requested display time in gameplay seconds; omitting it uses the method default.
    public void ShowNotification(string message, float duration = 3.5f)
    {
        // If notificationText is missing, return now; skip the rest of this method.
        if (notificationText == null) return;

        // Copy the caller message argument into the assigned TMP notification text before showing it.
        notificationText.text = message;
        // Activate notificationText.gameObject.
        notificationText.gameObject.SetActive(true);

        // Reset timer if a new message pops up while one is already active
        // If hideCoroutine exists, stop the coroutine stored in hideCoroutine.
        if (hideCoroutine != null) StopCoroutine(hideCoroutine);
        // Store the coroutine handle for HideAfterDelay(duration); Unity resumes it after each yield. in
        // hideCoroutine.
        hideCoroutine = StartCoroutine(HideAfterDelay(duration));
    }

    /// <summary>Wait in gameplay seconds, then hide the notification text.</summary>
    // delay is a float argument supplied by NotificationManager.ShowNotification.
    private IEnumerator HideAfterDelay(float delay)
    {
        // Yield a wait for delay gameplay seconds (pause stops the timer); Unity resumes the coroutine after it
        // completes.
        yield return new WaitForSeconds(delay);
        // Deactivate notificationText.gameObject.
        notificationText.gameObject.SetActive(false);
    }
}