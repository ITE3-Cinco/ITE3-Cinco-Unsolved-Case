// Purpose: Makes corridor lights and their visible panels dip in brightness together.
// Use: Attach to an atmosphere object; assign corridor lights and the environment root.
// How it works: Save original brightness, alternate short dim pulses, then wait for the next burst.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

/// <summary>Occasional power dips shared by corridor lights and their visible panels.</summary>
public class CorridorLightFlicker : MonoBehaviour
{
    // environmentRoot: Transform whose environment children provide geometry. Assigned in the scene/prefab
    // Inspector.
    public Transform environmentRoot;
    // lights: corridor Light references whose saved intensities are multiplied during flicker. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public Light[] lights = System.Array.Empty<Light>();
    // burstInterval: minimum and maximum seconds between flicker bursts. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    public Vector2 burstInterval = new Vector2(2.5f, 6f);

    // BaseColor: cached shader ID for the base-color property. Shared across instances; scene reload alone does
    // not reset static state.
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    // EmissionColor: cached shader ID for the emission-color property. Shared across instances; scene reload alone
    // does not reset static state.
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    // panels: renderer material slots that represent visible fluorescent panels. Initialized from a new list with
    // no entries.
    private readonly List<Panel> panels = new List<Panel>();
    // intensities: startup Light intensity values used as the flicker baseline. Updated by OnEnable.
    private float[] intensities;
    // remainingTime: seconds until the next flicker step. Advance sets it from deltaTime.
    private float remainingTime;
    // remainingPulses: alternating dim/bright steps left in the current burst. Updated by OnEnable, Advance.
    private int remainingPulses;
    // dimmed: whether the current flicker step is dim. Updated by OnEnable, Advance.
    private bool dimmed;

    // Each panel remembers one renderer material slot and its original and working color overrides.
    private class Panel
    {
        // renderer: Renderer whose fluorescent material slot is dimmed. Runtime data filled by the methods below;
        // starts at the type default.
        public Renderer renderer;
        // slot: zero-based material slot belonging to this fluorescent panel. Runtime data filled by the methods
        // below; starts at the type default.
        public int slot;
        // color: original panel base tint read from its material. Runtime data filled by the methods below; starts
        // at the type default.
        public Color color;
        // emission: original panel emission tint read from its material. Runtime data filled by the methods below;
        // starts at the type default.
        public Color emission;
        // original: saved renderer property block restored when flicker stops. Runtime data filled by the methods
        // below; starts at the type default.
        public MaterialPropertyBlock original;
        // working: separate renderer property block changed during flicker. Runtime data filled by the methods
        // below; starts at the type default.
        public MaterialPropertyBlock working;
    }

    /// <summary>Save original light values and panel properties before starting a random flicker timer.</summary>
    private void OnEnable()
    {
        // Set intensities to a new array/object initialized with the entries specified here.
        intensities = new float[lights.Length];
        // Repeat this block while i is below length of lights; start from int i = 0.
        for (int i = 0; i < lights.Length; i++)
            // If lights[i] (entry read from lights) exists, set intensities[i] to lights[i].intensity.
            if (lights[i] != null) intensities[i] = lights[i].intensity;

        // Remove all entries from panels.
        panels.Clear();
        // If environmentRoot exists, run the following block.
        if (environmentRoot != null)
        {
            // Process each renderer in all Renderer components on or beneath environmentRoot (including inactive
            // children).
            foreach (Renderer renderer in environmentRoot.GetComponentsInChildren<Renderer>(true))
            {
                // Store shared material asset(s) on renderer in local materials.
                Material[] materials = renderer.sharedMaterials;
                // Repeat this block while slot is below length of materials; start from int slot = 0.
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    // Store materials[slot] (entry read from materials) in local material.
                    Material material = materials[slot];
                    // The imported map combines all fluorescent panels into one mesh.
                    // Match its material, rather than flickering the wall/trim renderers.
                    // If material is missing or text from material.name with " (Instance)" replaced by "" differs
                    // from "light", skip this entry and continue with the next loop iteration.
                    if (material == null || material.name.Replace(" (Instance)", "") != "light") continue;
                    // Store a new Panel instance in local panel.
                    var panel = new Panel
                    {
                        renderer = renderer,
                        slot = slot,
                        color = material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white,
                        emission = material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) : Color.black,
                        original = new MaterialPropertyBlock(),
                        working = new MaterialPropertyBlock()
                    };
                    // Copy renderer property overrides from renderer into panel.original for material slot slot.
                    renderer.GetPropertyBlock(panel.original, slot);
                    // Copy renderer property overrides from renderer into panel.working for material slot slot.
                    renderer.GetPropertyBlock(panel.working, slot);
                    // Add panel to panels.
                    panels.Add(panel);
                }
            }
        }

        // Set remainingPulses to 0.
        remainingPulses = 0;
        // Set dimmed to false.
        dimmed = false;
        // Store Unity random value between the largest of 1f, burstInterval.x and the largest of 1f,
        // burstInterval.y in remainingTime.
        remainingTime = Random.Range(Mathf.Max(1f, burstInterval.x), Mathf.Max(1f, burstInterval.y));
        // Call CorridorLightFlicker.ApplyBrightness with 1; multiply saved light brightness and panel colors by
        // the same flicker amount.
        ApplyBrightness(1f);
    }

    /// <summary>Each frame, advance flicker only while gameplay is active.</summary>
    private void Update()
    {
        // If PauseManager.IsGamePaused or SceneTransition.IsLoading, return now; skip the rest of this method.
        if (PauseManager.IsGamePaused || SceneTransition.IsLoading) return;
        // Call CorridorLightFlicker.Advance with Unity gameplay frame duration in seconds; use the elapsed seconds
        // to alternate dim and normal brightness within a burst.
        Advance(Time.deltaTime);
    }

    /// <summary>Use the elapsed seconds to alternate dim and normal brightness within a burst.</summary>
    // deltaTime is a float argument supplied by CorridorLightFlicker.Update.
    private void Advance(float deltaTime)
    {
        // If deltaTime is at most 0, return now; skip the rest of this method.
        if (deltaTime <= 0f) return;
        // Subtract deltaTime from remainingTime and store the updated value there.
        remainingTime -= deltaTime;
        // If remainingTime is above 0, return now; skip the rest of this method.
        if (remainingTime > 0f) return;

        // An even number of alternating pulses returns brightness to normal before the longer wait.
        // If remainingPulses equals 0, recalculate remainingPulses from Unity random value between 2 and 6
        // (integer upper bound excluded) multiplied by 2.
        if (remainingPulses == 0) remainingPulses = Random.Range(2, 6) * 2;
        // Set dimmed to not (dimmed).
        dimmed = !dimmed;
        // Call CorridorLightFlicker.ApplyBrightness with Unity random value between 0.05f and 0.35f when dimmed;
        // otherwise 1; multiply saved light brightness and panel colors by the same flicker amount.
        ApplyBrightness(dimmed ? Random.Range(0.05f, 0.35f) : 1f);
        // Decrease remainingPulses by one, storing the next counter/index value.
        remainingPulses--;
        // Set remainingTime to Unity random value between 0.06f and 0.16f when remainingPulses is above 0;
        // otherwise Unity random value between Mathf.Max(1f, burstInterval.x) and Mathf.Max(1f, burstInterval.y).
        remainingTime = remainingPulses > 0
            ? Random.Range(0.06f, 0.16f)
            : Random.Range(Mathf.Max(1f, burstInterval.x), Mathf.Max(1f, burstInterval.y));
    }

    /// <summary>Multiply saved light brightness and panel colors by the same flicker amount.</summary>
    // multiplier is a float argument supplied by CorridorLightFlicker.Advance, CorridorLightFlicker.OnEnable.
    private void ApplyBrightness(float multiplier)
    {
        // Repeat this block while i is below length of lights; start from int i = 0.
        for (int i = 0; i < lights.Length; i++)
            // If lights[i] (entry read from lights) exists, set lights[i].intensity to intensities[i] (entry read
            // from intensities) multiplied by multiplier.
            if (lights[i] != null) lights[i].intensity = intensities[i] * multiplier;

        // Process each panel in panels.
        foreach (Panel panel in panels)
        {
            // If panel.renderer is missing, skip this entry and continue with the next loop iteration.
            if (panel.renderer == null) continue;
            // Keep even the healthy fluorescent panel glow subdued.
            // Set panel.working color property BaseColor from panel.color multiplied by (0.35f * multiplier).
            panel.working.SetColor(BaseColor, panel.color * (0.35f * multiplier));
            // Set panel.working color property EmissionColor from panel.emission multiplied by (0.35f *
            // multiplier).
            panel.working.SetColor(EmissionColor, panel.emission * (0.35f * multiplier));
            // Apply panel.working overrides to panel.renderer material slot panel.slot.
            panel.renderer.SetPropertyBlock(panel.working, panel.slot);
        }
    }

    /// <summary>Restore the saved lights and panel properties when the effect stops.</summary>
    private void OnDisable()
    {
        // If intensities exists, run the following block.
        if (intensities != null)
            // Repeat this block while i is below length of lights; start from int i = 0.
            for (int i = 0; i < lights.Length; i++)
                // If lights[i] (entry read from lights) exists, set lights[i].intensity to intensities[i] (entry
                // read from intensities).
                if (lights[i] != null) lights[i].intensity = intensities[i];
        // Process each panel in panels.
        foreach (Panel panel in panels)
            // If panel.renderer exists, apply panel.original overrides to panel.renderer material slot panel.slot.
            if (panel.renderer != null) panel.renderer.SetPropertyBlock(panel.original, panel.slot);
        // Remove all entries from panels.
        panels.Clear();
    }
}
