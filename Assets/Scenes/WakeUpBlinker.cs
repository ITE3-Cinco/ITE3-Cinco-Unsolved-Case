using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Resolution-independent eyelids: black, two slow blinks, then fully open.</summary>
[DefaultExecutionOrder(-100)]
public class WakeUpBlinker : MonoBehaviour
{
    [Header("Eyelid UI Rects")]
    [SerializeField] private RectTransform topLid;
    [SerializeField] private RectTransform bottomLid;

    [Header("Animation Settings")]
    [Min(0f)] [SerializeField] private float delayBeforeBlink = 0.8f;
    [Min(0.1f)] [SerializeField] private float blinkSpeedMultiplier = 1f;
    private float openness;
    private Canvas eyelidCanvas;

    private void Awake()
    {
        if (topLid == null || bottomLid == null)
        {
            Debug.LogError("[WakeUpBlinker] Assign both eyelid images.", this);
            enabled = false;
            return;
        }
        eyelidCanvas = GetComponent<Canvas>();
        if (eyelidCanvas != null)
        {
            eyelidCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            eyelidCanvas.sortingOrder = 100;
        }
        foreach (RectTransform lid in new[] { topLid, bottomLid })
        {
            lid.gameObject.SetActive(true);
            lid.localRotation = Quaternion.identity;
            lid.localScale = Vector3.one;
            Image image = lid.GetComponent<Image>();
            if (image == null) image = lid.gameObject.AddComponent<Image>();
            image.enabled = true;
            image.sprite = null;
            image.color = Color.black;
            image.raycastTarget = false;
        }
        SetOpenness(0f); // Cover the very first rendered frame.
    }

    private void LateUpdate()
    {
        // Let the pause menu remain readable while the scaled blink is suspended.
        if (eyelidCanvas != null) eyelidCanvas.enabled = !PauseManager.IsGamePaused;
    }

    private IEnumerator Start()
    {
        yield return new WaitForSeconds(delayBeforeBlink);
        yield return AnimateLids(0.22f, 1.1f);
        yield return AnimateLids(0f, 0.55f);
        yield return new WaitForSeconds(0.45f);
        yield return AnimateLids(0.55f, 1.4f);
        yield return AnimateLids(0f, 0.65f);
        yield return new WaitForSeconds(0.35f);
        yield return AnimateLids(1f, 2.4f);
        gameObject.SetActive(false);
    }

    private IEnumerator AnimateLids(float target, float duration)
    {
        float start = openness;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime * Mathf.Max(0.1f, blinkSpeedMultiplier);
            SetOpenness(Mathf.Lerp(start, target, Mathf.SmoothStep(0f, 1f, elapsed / duration)));
            yield return null;
        }
        SetOpenness(target);
    }

    private void SetOpenness(float value)
    {
        openness = Mathf.Clamp01(value);
        // Anchors are fractions of the canvas, so CanvasScaler and aspect-ratio
        // changes cannot leave the old 100-pixel strips at the screen edges.
        float shift = openness * 0.51f;
        topLid.anchorMin = new Vector2(0f, 0.5f + shift);
        topLid.anchorMax = new Vector2(1f, 1f + shift);
        bottomLid.anchorMin = new Vector2(0f, -shift);
        bottomLid.anchorMax = new Vector2(1f, 0.5f - shift);
        topLid.offsetMin = new Vector2(0f, -1f);
        topLid.offsetMax = Vector2.zero;
        bottomLid.offsetMin = Vector2.zero;
        bottomLid.offsetMax = new Vector2(0f, 1f);
    }
}
