using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>Apply saved brightness relative to the scene's authored exposure.</summary>
[RequireComponent(typeof(Volume))]
public class SceneBrightness : MonoBehaviour
{
    private Volume volume;
    private ColorAdjustments adjustments;
    private float baseExposure;

    private void OnEnable()
    {
        Apply(PlayerPrefs.GetFloat("Brightness", 5f));
    }

    public void Apply(float sliderValue)
    {
        if (volume == null)
        {
            volume = GetComponent<Volume>();
            if (volume.sharedProfile != null &&
                volume.sharedProfile.TryGet(out ColorAdjustments authored))
                baseExposure = authored.postExposure.value;
            if (!volume.profile.TryGet(out adjustments))
                adjustments = volume.profile.Add<ColorAdjustments>(true);
        }
        adjustments.active = true;
        adjustments.postExposure.overrideState = true;
        adjustments.postExposure.value = baseExposure + Mathf.Lerp(-2f, 2f, Mathf.Clamp01(sliderValue / 10f));
    }
}
