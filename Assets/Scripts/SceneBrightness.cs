// Purpose: Applies the saved brightness setting to this scene lighting exposure.
// Use: Attach to a URP Volume, or let MainMenuController add it to the active global volume.
// How it works: Convert the 0 to 10 slider into an exposure offset relative to the scene original value.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity rendering supplies Volume/profile types used for camera effects.
using UnityEngine.Rendering;
// URP supplies ColorAdjustments and other pipeline-specific camera settings.
using UnityEngine.Rendering.Universal;

/// <summary>Apply saved brightness relative to the scene's authored exposure.</summary>
[RequireComponent(typeof(Volume))]
public class SceneBrightness : MonoBehaviour
{
    // volume: URP Volume whose runtime profile receives brightness changes. Apply retrieves it from the first
    // Volume component on this GameObject.
    private Volume volume;
    // adjustments: ColorAdjustments setting in the Volume runtime profile. Updated by Apply.
    private ColorAdjustments adjustments;
    // baseExposure: original exposure read once from the authored shared Volume profile. Apply sets it from
    // authored.postExposure.value.
    private float baseExposure;

    /// <summary>Apply the saved preference as soon as this scene volume becomes active.</summary>
    private void OnEnable()
    {
        // Call SceneBrightness.Apply with saved PlayerPrefs value for "Brightness" (default 5f); cache the
        // authored exposure once, then adjust the runtime profile from the slider value.
        Apply(PlayerPrefs.GetFloat("Brightness", 5f));
    }

    /// <summary>Cache the authored exposure once, then adjust the runtime profile from the slider value.</summary>
    // sliderValue comes from saved PlayerPrefs on enable or MainMenuController.SetBrightness; expected menu range
    // is 0 to 10.
    public void Apply(float sliderValue)
    {
        // If volume is missing, run the following block.
        if (volume == null)
        {
            // Store the first Volume component on this GameObject in volume.
            volume = GetComponent<Volume>();
            // Read ColorAdjustments from the assigned Volume shared profile into authored when available;
            // its exposure becomes the baseline for the saved brightness offset.
            if (volume.sharedProfile != null &&
                volume.sharedProfile.TryGet(out ColorAdjustments authored))
                // Read the authored ColorAdjustments exposure from the Volume shared profile once; slider offsets
                // stay relative to this baseline.
                baseExposure = authored.postExposure.value;
            // Retrieve the runtime profile ColorAdjustments into adjustments; create one below if absent.
            if (!volume.profile.TryGet(out adjustments))
                // Add ColorAdjustments to the runtime Volume profile and keep the returned setting in
                // adjustments; false starts its property overrides disabled.
                adjustments = volume.profile.Add<ColorAdjustments>(false);
        }
        // Enable only exposure, adding the slider offset to the cached authored value without accumulating it.
        // Set adjustments.active to true.
        adjustments.active = true;
        // Set adjustments.postExposure.overrideState to true.
        adjustments.postExposure.overrideState = true;
        // Convert caller sliderValue from 0-10 to 0-1, blend an exposure offset from -2 to +2, and add it to
        // cached baseExposure.
        adjustments.postExposure.value = baseExposure + Mathf.Lerp(-2f, 2f, Mathf.Clamp01(sliderValue / 10f));
    }
}
