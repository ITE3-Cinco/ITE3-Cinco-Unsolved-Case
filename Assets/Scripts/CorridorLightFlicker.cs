using System.Collections.Generic;
using UnityEngine;

/// <summary>Occasional power dips shared by corridor lights and their visible panels.</summary>
public class CorridorLightFlicker : MonoBehaviour
{
    public Transform environmentRoot;
    public Light[] lights = System.Array.Empty<Light>();
    public Vector2 burstInterval = new Vector2(2.5f, 6f);

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    private readonly List<Panel> panels = new List<Panel>();
    private float[] intensities;
    private float remainingTime;
    private int remainingPulses;
    private bool dimmed;

    private class Panel
    {
        public Renderer renderer;
        public int slot;
        public Color color;
        public Color emission;
        public MaterialPropertyBlock original;
        public MaterialPropertyBlock working;
    }

    private void OnEnable()
    {
        intensities = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) intensities[i] = lights[i].intensity;

        panels.Clear();
        if (environmentRoot != null)
        {
            foreach (Renderer renderer in environmentRoot.GetComponentsInChildren<Renderer>(true))
            {
                Material[] materials = renderer.sharedMaterials;
                for (int slot = 0; slot < materials.Length; slot++)
                {
                    Material material = materials[slot];
                    // The imported map combines all fluorescent panels into one mesh.
                    // Match its material, rather than flickering the wall/trim renderers.
                    if (material == null || material.name.Replace(" (Instance)", "") != "light") continue;
                    var panel = new Panel
                    {
                        renderer = renderer,
                        slot = slot,
                        color = material.HasProperty(BaseColor) ? material.GetColor(BaseColor) : Color.white,
                        emission = material.HasProperty(EmissionColor) ? material.GetColor(EmissionColor) : Color.black,
                        original = new MaterialPropertyBlock(),
                        working = new MaterialPropertyBlock()
                    };
                    renderer.GetPropertyBlock(panel.original, slot);
                    renderer.GetPropertyBlock(panel.working, slot);
                    panels.Add(panel);
                }
            }
        }

        remainingPulses = 0;
        dimmed = false;
        remainingTime = Random.Range(Mathf.Max(1f, burstInterval.x), Mathf.Max(1f, burstInterval.y));
        ApplyBrightness(1f);
    }

    private void Update()
    {
        if (PauseManager.IsGamePaused || SceneTransition.IsLoading) return;
        Advance(Time.deltaTime);
    }

    private void Advance(float deltaTime)
    {
        if (deltaTime <= 0f) return;
        remainingTime -= deltaTime;
        if (remainingTime > 0f) return;

        if (remainingPulses == 0) remainingPulses = Random.Range(2, 6) * 2;
        dimmed = !dimmed;
        ApplyBrightness(dimmed ? Random.Range(0.05f, 0.35f) : 1f);
        remainingPulses--;
        remainingTime = remainingPulses > 0
            ? Random.Range(0.06f, 0.16f)
            : Random.Range(Mathf.Max(1f, burstInterval.x), Mathf.Max(1f, burstInterval.y));
    }

    private void ApplyBrightness(float multiplier)
    {
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = intensities[i] * multiplier;

        foreach (Panel panel in panels)
        {
            if (panel.renderer == null) continue;
            // Keep even the healthy fluorescent panel glow subdued.
            panel.working.SetColor(BaseColor, panel.color * (0.35f * multiplier));
            panel.working.SetColor(EmissionColor, panel.emission * (0.35f * multiplier));
            panel.renderer.SetPropertyBlock(panel.working, panel.slot);
        }
    }

    private void OnDisable()
    {
        if (intensities != null)
            for (int i = 0; i < lights.Length; i++)
                if (lights[i] != null) lights[i].intensity = intensities[i];
        foreach (Panel panel in panels)
            if (panel.renderer != null) panel.renderer.SetPropertyBlock(panel.original, panel.slot);
        panels.Clear();
    }
}
