// Purpose: Pulses the menu title image between two green colors with optional flicker.
// Use: Attach to the title UI Image and tune colors, speed and flicker in the Inspector.
// How it works: A sine wave provides a smooth repeating blend; small random color shifts add the horror effect.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
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
    // baseColor: The 'off' or subtle color state (pale greenish white). Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    [Header("Color Settings")]
    [Tooltip("The 'off' or subtle color state (pale greenish white).")]
    [SerializeField] private Color baseColor = new Color(0.8f, 1.0f, 0.85f, 1.0f); // Pale Mint/Off-White
    
    // glowColor: The intense 'glowing' color state (vibrant neon green). Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    [Tooltip("The intense 'glowing' color state (vibrant neon green).")]
    [SerializeField] private Color glowColor = new Color(0.1f, 1.0f, 0.1f, 1.0f);  // Intense Neon Green (#1AFF1A)

    // glowSpeed: How fast the glow smooth oscillates (sin wave). Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Pulsing Speed")]
    [Tooltip("How fast the glow smooth oscillates (sin wave).")]
    [SerializeField] private float glowSpeed = 2.0f;

    // enableFlicker: Adds random power fluctuations to the intensity, like a failing bulb. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Header("Investigative Flicker Effect")]
    [Tooltip("Adds random power fluctuations to the intensity, like a failing bulb.")]
    [SerializeField] private bool enableFlicker = true;
    
    // flickerVolatility: Adjusts how violently the color flickers (higher is more volatile). Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Adjusts how violently the color flickers (higher is more volatile).")]
    [Range(0f, 0.5f)] [SerializeField] private float flickerVolatility = 0.15f;

    // titleImage: UI Image receiving the animated title tint. Awake retrieves it from the first Image component on
    // this GameObject.
    private Image titleImage;
    // currentColor: calculated title tint after blending and flicker. Updated by Update.
    private Color currentColor;

    /// <summary>Find the title Image whose tint will be animated.</summary>
    private void Awake()
    {
        // Store the first Image component on this GameObject in titleImage.
        titleImage = GetComponent<Image>();
    }

    /// <summary>Blend green shades with a repeating wave, add optional flicker and apply the resulting color.</summary>
    private void Update()
    {
        // If titleImage is missing, return now; skip the rest of this method.
        if (titleImage == null) return;

        // 1. Generate smooth wave (0 to 1) for the main pulse
        // Store (Mathf.Sin(Time.time * glowSpeed) plus 1f) divided by 2 in local wave.
        float wave = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;

        // 2. Linear interpolate between the two green tints
        // Store a blend from baseColor to glowColor using fraction wave in currentColor.
        currentColor = Color.Lerp(baseColor, glowColor, wave);

        // 3. Add random flicker (Power fluctuation simulation)
        // If enableFlicker, run the following block.
        if (enableFlicker)
        {
            // Use Perlin noise or random range to introduce small shifts
            // Store (Unity random value from 0 to 1 minus 0.5f) multiplied by flickerVolatility in local
            // noiseShift.
            float noiseShift = (Random.value - 0.5f) * flickerVolatility;
            
            // Apply flicker shift primarily to intensity (can shift V in HSV or slightly shift RGB)
            // Here we shift RGB uniformly to affect brightness mostly.
            // Store currentColor.r plus noiseShift limited to the range 0 to 1 in currentColor.r.
            currentColor.r = Mathf.Clamp01(currentColor.r + noiseShift);
            // Store currentColor.g plus noiseShift * 0.5f limited to the range 0 to 1 in currentColor.g.
            currentColor.g = Mathf.Clamp01(currentColor.g + noiseShift * 0.5f); // Keep green dominant
            // Store currentColor.b plus noiseShift limited to the range 0 to 1 in currentColor.b.
            currentColor.b = Mathf.Clamp01(currentColor.b + noiseShift);
        }

        // 4. Set the new color to the image
        // Copy currentColor cached by this component into titleImage.color.
        titleImage.color = currentColor;
    }
}