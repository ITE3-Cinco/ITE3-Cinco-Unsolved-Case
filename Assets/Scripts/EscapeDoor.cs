using UnityEngine;

public class EscapeDoor : MonoBehaviour
{
    [Header("Door Settings")]
    public string lockedMessage = "Find all the required evidence first.";
    public string unlockedMessage = "The way out is open.";
    [Tooltip("Overrides the scene name on EscapeManager when set. Leave empty to use EscapeManager's configured scene.")]
    public string nextSceneName = "";
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

        // Prefer the EscapeManager's transition (fade + timeScale freeze) over a
        // raw SceneManager.LoadScene call so the experience is consistent.
        if (EscapeManager.Instance != null)
        {
            // Allow an optional per-door scene override.
            if (!string.IsNullOrWhiteSpace(nextSceneName))
                EscapeManager.Instance.nextSceneName = nextSceneName;

            // Trigger the manager's own transition logic.
            // The manager's LateUpdate proximity check handles the actual load;
            // force it immediately by calling the internal method via the public API.
            // Since EscapeManager.TryTransition is private, we replicate the intent:
            // freeze time and start the fade here, delegating to the manager's fade path.
            ScreenFadeController fade = ScreenFadeController.Instance;
            if (fade == null) fade = gameObject.AddComponent<ScreenFadeController>();

            string scene = string.IsNullOrWhiteSpace(nextSceneName)
                ? EscapeManager.Instance.nextSceneName
                : nextSceneName;

            if (!string.IsNullOrWhiteSpace(scene))
            {
                Time.timeScale = 0f;
                fade.FadeToScene(scene, EscapeManager.Instance.fadeDuration);
            }
            else
            {
                Debug.LogWarning("[EscapeDoor] Escape is unlocked, but no next scene name is configured.");
            }
        }
        else
        {
            Debug.LogWarning("[EscapeDoor] No EscapeManager found. Cannot transition to next scene.");
        }
    }

    private void ShowMessage(string message)
    {
        Debug.Log($"[EscapeDoor] {message}");

        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowNotification(message, 3f);
    }
}
