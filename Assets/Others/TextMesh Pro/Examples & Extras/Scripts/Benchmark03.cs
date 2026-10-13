// Purpose: Compares font rendering styles using many text samples.
// Use: Attach in a sample scene, assign SourceFont and choose a benchmark style.
// How it works: Choose a font rendering style, then spawn overlapping text samples at one position.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// Unity TextCore supplies font glyph-rendering modes for the benchmark.
using UnityEngine.TextCore.LowLevel;


namespace TMPro.Examples
{

    public class Benchmark03 : MonoBehaviour
    {
        public enum BenchmarkType { TMP_SDF_MOBILE = 0, TMP_SDF__MOBILE_SSD = 1, TMP_SDF = 2, TMP_BITMAP_MOBILE = 3, TEXTMESH_BITMAP = 4 }

        // NumberOfSamples: number of samples. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int NumberOfSamples = 100;
        // Benchmark: benchmark. Assigned in the scene/prefab Inspector.
        public BenchmarkType Benchmark;

        // SourceFont: source font. Assigned in the scene/prefab Inspector.
        public Font SourceFont;


        // This example leaves its early initialization callback empty.
        void Awake()
        {

        }


        // At startup, prepare the selected font style and create text samples at the same position.
        void Start()
        {
            // Initialize fontAsset to no reference (null).
            TMP_FontAsset fontAsset = null;

            // Create Dynamic Font Asset for the given font file.
            // Choose the matching branch using Benchmark.
            switch (Benchmark)
            {
                case BenchmarkType.TMP_SDF_MOBILE:
                    // Store the result of TMP_FontAsset.CreateFontAsset using SourceFont, 90, 9,
                    // GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic in fontAsset.
                    fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case BenchmarkType.TMP_SDF__MOBILE_SSD:
                    // Store the result of TMP_FontAsset.CreateFontAsset using SourceFont, 90, 9,
                    // GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic in fontAsset.
                    fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic);
                    // Store the result of Shader.Find using "TextMeshPro/Mobile/Distance Field SSD" in
                    // fontAsset.material.shader.
                    fontAsset.material.shader = Shader.Find("TextMeshPro/Mobile/Distance Field SSD");
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case BenchmarkType.TMP_SDF:
                    // Store the result of TMP_FontAsset.CreateFontAsset using SourceFont, 90, 9,
                    // GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic in fontAsset.
                    fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SDFAA, 256, 256, AtlasPopulationMode.Dynamic);
                    // Store the result of Shader.Find using "TextMeshPro/Distance Field" in
                    // fontAsset.material.shader.
                    fontAsset.material.shader = Shader.Find("TextMeshPro/Distance Field");
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case BenchmarkType.TMP_BITMAP_MOBILE:
                    // Store the result of TMP_FontAsset.CreateFontAsset using SourceFont, 90, 9,
                    // GlyphRenderMode.SMOOTH, 256, 256, AtlasPopulationMode.Dynamic in fontAsset.
                    fontAsset = TMP_FontAsset.CreateFontAsset(SourceFont, 90, 9, GlyphRenderMode.SMOOTH, 256, 256, AtlasPopulationMode.Dynamic);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
            }

            // Repeat this block while i is below NumberOfSamples; start from int i = 0.
            for (int i = 0; i < NumberOfSamples; i++)
            {
                // Choose the matching branch using Benchmark.
                switch (Benchmark)
                {
                    case BenchmarkType.TMP_SDF_MOBILE:
                    case BenchmarkType.TMP_SDF__MOBILE_SSD:
                    case BenchmarkType.TMP_SDF:
                    case BenchmarkType.TMP_BITMAP_MOBILE:
                        {
                            // Store a new GameObject named by Unity in local go.
                            GameObject go = new GameObject();
                            // Set go.transform.position to a Vector3 built from 0, 1.2, 0.
                            go.transform.position = new Vector3(0, 1.2f, 0);

                            // Store a TextMeshPro component attached to go in local textComponent.
                            TextMeshPro textComponent = go.AddComponent<TextMeshPro>();
                            // Copy fontAsset into textComponent.font.
                            textComponent.font = fontAsset;
                            // Set textComponent.fontSize to 128.
                            textComponent.fontSize = 128;
                            // Display "@" in textComponent.text (the assigned TMP/UI text component).
                            textComponent.text = "@";
                            // Set textComponent.alignment to TextAlignmentOptions.Center.
                            textComponent.alignment = TextAlignmentOptions.Center;
                            // Set textComponent.color to a Color32 built from 255, 255, 0, 255.
                            textComponent.color = new Color32(255, 255, 0, 255);

                            // If Benchmark equals BenchmarkType.TMP_BITMAP_MOBILE, run the following block.
                            if (Benchmark == BenchmarkType.TMP_BITMAP_MOBILE)
                                // Set textComponent.fontSize to 132.
                                textComponent.fontSize = 132;

                        }
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case BenchmarkType.TEXTMESH_BITMAP:
                        {
                            // Store a new GameObject named by Unity in local go.
                            GameObject go = new GameObject();
                            // Set go.transform.position to a Vector3 built from 0, 1.2, 0.
                            go.transform.position = new Vector3(0, 1.2f, 0);

                            // Store a TextMesh component attached to go in local textMesh.
                            TextMesh textMesh = go.AddComponent<TextMesh>();
                            // Set textMesh.GetComponent<Renderer>().sharedMaterial to SourceFont material
                            // reference (Renderer.material may create an instance).
                            textMesh.GetComponent<Renderer>().sharedMaterial = SourceFont.material;
                            // Copy SourceFont from this component into textMesh.font.
                            textMesh.font = SourceFont;
                            // Set textMesh.anchor to TextAnchor.MiddleCenter.
                            textMesh.anchor = TextAnchor.MiddleCenter;
                            // Set textMesh.fontSize to 130.
                            textMesh.fontSize = 130;

                            // Set textMesh.color to a Color32 built from 255, 255, 0, 255.
                            textMesh.color = new Color32(255, 255, 0, 255);
                            // Display "@" in textMesh.text (the assigned TMP/UI text component).
                            textMesh.text = "@";
                        }
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                }
            }
        }

    }
}
