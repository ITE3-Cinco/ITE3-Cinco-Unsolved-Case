using UnityEngine;
using UnityEngine.UI;

/**
 * TitleGreenGlowEffect.cs
 * This script creates a dynamic glowing effect for a UI Image component, specifically designed for a title text.
 * It smoothly oscillates between two shades of green and can optionally add a flickering effect to simulate power fluctuations.
 *
 * Usage:
 * 1. Attach this script to a GameObject that has an Image component (e.g., a UI Text or Image).
 * 2. Configure the baseColor, glowColor, glowSpeed, and flicker settings in the Inspector.
 * 3. The effect will automatically run during the Update loop.
 *
 * Note: Ensure the Image component is set to use a material that supports color changes (e.g., default UI material).
 **/

[RequireComponent(typeof(Image))]
public class TitleGreenGlowEffect : MonoBehaviour
{
    [Header("Color Settings")]
    [Tooltip("The 'off' or subtle color state (pale greenish white).")]
    [SerializeField] private Color baseColor = new Color(0.8f, 1.0f, 0.85f, 1.0f); // Pale Mint/Off-White
    
    [Tooltip("The intense 'glowing' color state (vibrant neon green).")]
    [SerializeField] private Color glowColor = new Color(0.1f, 1.0f, 0.1f, 1.0f);  // Intense Neon Green (#1AFF1A)

    [Header("Pulsing Speed")]
    [Tooltip("How fast the glow smooth oscillates (sin wave).")]
    [SerializeField] private float glowSpeed = 2.0f;

    [Header("Investigative Flicker Effect")]
    [Tooltip("Adds random power fluctuations to the intensity, like a failing bulb.")]
    [SerializeField] private bool enableFlicker = true;
    
    [Tooltip("Adjusts how violently the color flickers (higher is more volatile).")]
    [Range(0f, 0.5f)] [SerializeField] private float flickerVolatility = 0.15f;

    private Image titleImage;
    private Color currentColor;

    private void Awake()
    {
        titleImage = GetComponent<Image>();
    }

    private void Update()
    {
        if (titleImage == null) return;

        // 1. Generate smooth wave (0 to 1) for the main pulse
        float wave = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;

        // 2. Linear interpolate between the two green tints
        currentColor = Color.Lerp(baseColor, glowColor, wave);

        // 3. Add random flicker (Power fluctuation simulation)
        if (enableFlicker)
        {
            // Use Perlin noise or random range to introduce small shifts
            float noiseShift = (Random.value - 0.5f) * flickerVolatility;
            
            // Apply flicker shift primarily to intensity (can shift V in HSV or slightly shift RGB)
            // Here we shift RGB uniformly to affect brightness mostly.
            currentColor.r = Mathf.Clamp01(currentColor.r + noiseShift);
            currentColor.g = Mathf.Clamp01(currentColor.g + noiseShift * 0.5f); // Keep green dominant
            currentColor.b = Mathf.Clamp01(currentColor.b + noiseShift);
        }

        // 4. Set the new color to the image
        titleImage.color = currentColor;
    }
}