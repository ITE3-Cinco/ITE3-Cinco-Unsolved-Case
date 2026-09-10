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

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void UseInvisibility()
    {
        if (!isInvisible)
        {
            StartCoroutine(InvisibilityRoutine());
        }
    }

    private IEnumerator InvisibilityRoutine()
    {
        isInvisible = true;
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
        Debug.Log("<color=purple>[Invisibility Expired]</color> Enemies can see you again!");
    }
}