using UnityEngine;
using System.Collections;

public class InvisibilityConsumable : MonoBehaviour
{
    public static InvisibilityConsumable Instance;

    [Header("Invisibility Settings")]
    public float duration = 5f; // Invisibility length in seconds
    public bool isInvisible = false;

    [Header("Visual Effects (Optional)")]
    public Material originalMaterial;
    public Material invisibleMaterial; // Semi-transparent shader material
    public Renderer playerRenderer;

    [Header("Agimat Warning")]
    [Min(1f)] public float warningDistance = 16f;
    public bool GhostNearby { get; private set; }
    private Inventory inventory;
    private GhostEnemy[] ghosts = System.Array.Empty<GhostEnemy>();
    private Coroutine invisibilityCoroutine;

    private void Start()
    {
        inventory = GetComponent<Inventory>();
        ghosts = FindObjectsByType<GhostEnemy>(FindObjectsSortMode.None);
    }

    private void Update()
    {
        GhostNearby = false;
        if (isInvisible || inventory == null || !inventory.HasAgimat) return;
        foreach (GhostEnemy ghost in ghosts)
        {
            if (ghost != null && ghost.isActiveAndEnabled &&
                Vector3.Distance(transform.position, ghost.transform.position) <= warningDistance)
            {
                GhostNearby = true;
                break;
            }
        }
    }

    public bool TryUseAgimat()
    {
        if (isInvisible || Time.timeScale <= 0f || PauseManager.IsGamePaused ||
            inventory == null || !inventory.ConsumeAgimat()) return false;
        UseInvisibility();
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.ShowNotification("Agimat protects you. The ghost retreats.", 3f);
        return true;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void UseInvisibility()
    {
        if (!isInvisible)
        {
            invisibilityCoroutine = StartCoroutine(InvisibilityRoutine());
        }
    }

    private IEnumerator InvisibilityRoutine()
    {
        isInvisible = true;
        GhostNearby = false;
        foreach (GhostEnemy ghost in ghosts)
            if (ghost != null) ghost.Repel(duration);
        Debug.Log("<color=purple>[Invisibility Active]</color> Player is now hidden from enemies!");

        // Apply visual transparency if assigned
        if (playerRenderer != null && invisibleMaterial != null)
        {
            playerRenderer.material = invisibleMaterial;
        }

        yield return new WaitForSeconds(duration);

        // Revert transparency
        if (playerRenderer != null && originalMaterial != null)
        {
            playerRenderer.material = originalMaterial;
        }

        isInvisible = false;
        invisibilityCoroutine = null;
        Debug.Log("<color=purple>[Invisibility Expired]</color> Enemies can see you again!");
    }
    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (invisibilityCoroutine != null)
        {
            StopCoroutine(invisibilityCoroutine);
            invisibilityCoroutine = null;
        }

        if (playerRenderer != null && originalMaterial != null)
            playerRenderer.material = originalMaterial;

        isInvisible = false;
    }
}
