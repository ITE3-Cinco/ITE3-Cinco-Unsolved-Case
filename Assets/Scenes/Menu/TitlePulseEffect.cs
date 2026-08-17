using UnityEngine;
using UnityEngine.UI;

public class TitlePulseEffect : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minAlpha = 0.8f;
    [SerializeField] private float maxAlpha = 1.0f;

    private Image titleImage;

    private void Start()
    {
        titleImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (titleImage == null) return;

        // Smoothly oscillate alpha for an eerie flickering title look
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f);
        Color c = titleImage.color;
        c.a = alpha;
        titleImage.color = c;
    }
}