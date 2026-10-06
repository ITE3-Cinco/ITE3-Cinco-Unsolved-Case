using UnityEngine;
using UnityEngine.SceneManagement;

public class EscapeDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public string lockedMessage = "Find all the required evidence first.";
    public string unlockedMessage = "The way out is open.";
    public string nextSceneName = "Level2";
    public bool requirePlayerTrigger = true;

    [Header("Optional Door Visual")]
    public GameObject lockedIndicator;
    public GameObject unlockedIndicator;

    private bool playerInside;

    private void Start()
    {
        UpdateDoorVisual();
    }

    public void RefreshDoorVisual()
    {
        UpdateDoorVisual();
    }

    private void UpdateDoorVisual()
    {
        bool unlocked = EscapeManager.Instance != null && EscapeManager.Instance.CanEscape();

        if (lockedIndicator != null)
            lockedIndicator.SetActive(!unlocked);

        if (unlockedIndicator != null)
            unlockedIndicator.SetActive(unlocked);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!requirePlayerTrigger || !other.CompareTag("Player"))
            return;

        playerInside = true;
        TryEscape();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            playerInside = false;
    }

    public void TryEscape()
    {
        if (!playerInside && requirePlayerTrigger)
            return;

        bool canEscape = EscapeManager.Instance != null && EscapeManager.Instance.CanEscape();

        // Refresh the indicator before deciding what message/state to show.
        UpdateDoorVisual();

        if (!canEscape)
        {
            ShowMessage(lockedMessage);
            return;
        }

        ShowMessage(unlockedMessage);

        if (string.IsNullOrWhiteSpace(nextSceneName))
        {
            Debug.LogWarning("[EscapeDoor] Escape is unlocked, but no next scene name is assigned.");
            return;
        }

        SceneManager.LoadScene(nextSceneName);
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[EscapeDoor] {message}");

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowNotification(message, 3f);
    }
}
