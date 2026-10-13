// Purpose: Displays frames per second and frame time in a canvas corner.
// Use: Attach inside a sample canvas with the bundled font resources available.
// How it works: Create UI text, count frames over real time and update its corner anchors.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class TMP_UiFrameRateCounter : MonoBehaviour
    {
        // UpdateInterval: update interval. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public float UpdateInterval = 5.0f;
        // m_LastInterval: last interval. Start sets it from Unity elapsed real time since startup.
        private float m_LastInterval = 0;
        // m_Frames: frames. Updated by Start, Update.
        private int m_Frames = 0;

        public enum FpsCounterAnchorPositions { TopLeft, BottomLeft, TopRight, BottomRight };

        // AnchorPosition: anchor position. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public FpsCounterAnchorPositions AnchorPosition = FpsCounterAnchorPositions.TopRight;

        // htmlColorTag: html color tag. Updated by Update.
        private string htmlColorTag;
        // fpsLabel: fps label. Fixed script constant; callers cannot change it.
        private const string fpsLabel = "{0:2}</color> <#8080ff>FPS \n<#FF8000>{1:2} <#8080ff>MS";

        // m_TextMeshPro: text mesh pro. Awake retrieves it from a TextMeshProUGUI component attached to
        // frameCounter.
        private TextMeshProUGUI m_TextMeshPro;
        // m_frameCounter_transform: frame counter transform. Awake retrieves it from a RectTransform component
        // attached to frameCounter.
        private RectTransform m_frameCounter_transform;

        // last_AnchorPosition: last anchor position. Awake sets it from AnchorPosition.
        private FpsCounterAnchorPositions last_AnchorPosition;

        // Create and format the counter text and place it at the chosen corner.
        void Awake()
        {
            // Stop this method early when not (enabled).
            if (!enabled)
                // Return now; skip the rest of this method.
                return;

            // Set Application.targetFrameRate to 1000.
            Application.targetFrameRate = 1000;

            // Store a new GameObject named "Frame Counter" in local frameCounter.
            GameObject frameCounter = new GameObject("Frame Counter");
            // Store a RectTransform component attached to frameCounter in m_frameCounter_transform.
            m_frameCounter_transform = frameCounter.AddComponent<RectTransform>();

            // Parent m_frameCounter_transform beneath Transform of this; keep its local coordinates.
            m_frameCounter_transform.SetParent(this.transform, false);

            // Store a TextMeshProUGUI component attached to frameCounter in m_TextMeshPro.
            m_TextMeshPro = frameCounter.AddComponent<TextMeshProUGUI>();
            // Store the TMP_FontAsset asset from a Resources folder at "Fonts & Materials/LiberationSans SDF" in
            // m_TextMeshPro.font.
            m_TextMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            // Store the Material asset from a Resources folder at "Fonts & Materials/LiberationSans SDF - Overlay"
            // in m_TextMeshPro.fontSharedMaterial.
            m_TextMeshPro.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");

            // Set m_TextMeshPro.textWrappingMode to TextWrappingModes.NoWrap.
            m_TextMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
            // Set m_TextMeshPro.fontSize to 36.
            m_TextMeshPro.fontSize = 36;

            // Set m_TextMeshPro.isOverlay to true.
            m_TextMeshPro.isOverlay = true;

            // Call TMP_UiFrameRateCounter.Set_FrameCounter_Position with AnchorPosition; place and align the
            // readout at the selected screen or viewport corner.
            Set_FrameCounter_Position(AnchorPosition);
            // Copy AnchorPosition from this component into last_AnchorPosition.
            last_AnchorPosition = AnchorPosition;
        }


        // Start the real-time measurement interval with zero counted frames.
        void Start()
        {
            // Set m_LastInterval to Unity elapsed real time since startup.
            m_LastInterval = Time.realtimeSinceStartup;
            // Set m_Frames to 0.
            m_Frames = 0;
        }


        // Count rendered frames, calculate FPS and frame time at each interval, then update the label.
        void Update()
        {
            // If AnchorPosition differs from last_AnchorPosition, run the following block.
            if (AnchorPosition != last_AnchorPosition)
                // Call TMP_UiFrameRateCounter.Set_FrameCounter_Position with AnchorPosition; place and align the
                // readout at the selected screen or viewport corner.
                Set_FrameCounter_Position(AnchorPosition);

            // Copy AnchorPosition from this component into last_AnchorPosition.
            last_AnchorPosition = AnchorPosition;

            // Add 1 to m_Frames and store the updated value there.
            m_Frames += 1;
            // Store Unity elapsed real time since startup in local timeNow.
            float timeNow = Time.realtimeSinceStartup;

            // If timeNow is above m_LastInterval plus UpdateInterval, run the following block.
            if (timeNow > m_LastInterval + UpdateInterval)
            {
                // display two fractional digits (f2 format)
                // Store m_Frames divided by (timeNow minus m_LastInterval) in local fps.
                float fps = m_Frames / (timeNow - m_LastInterval);
                // Store 1000.0 divided by the largest of fps, 0.00001f in local ms.
                float ms = 1000.0f / Mathf.Max(fps, 0.00001f);

                // If fps is below 30, run the following block.
                if (fps < 30)
                    // Set htmlColorTag to "<color=yellow>".
                    htmlColorTag = "<color=yellow>";
                else if (fps < 10)
                    // Set htmlColorTag to "<color=red>".
                    htmlColorTag = "<color=red>";
                else
                    // Set htmlColorTag to "<color=green>".
                    htmlColorTag = "<color=green>";

                // Call m_TextMeshPro.SetText using htmlColorTag plus fpsLabel, fps, ms.
                m_TextMeshPro.SetText(htmlColorTag + fpsLabel, fps, ms);

                // Set m_Frames to 0.
                m_Frames = 0;
                // Copy timeNow into m_LastInterval.
                m_LastInterval = timeNow;
            }
        }


        // Place and align the readout at the selected screen or viewport corner.
        // anchor_position is a FpsCounterAnchorPositions argument supplied by TMP_UiFrameRateCounter.Awake,
        // TMP_UiFrameRateCounter.Update.
        void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
        {
            // Choose the matching branch using anchor_position.
            switch (anchor_position)
            {
                case FpsCounterAnchorPositions.TopLeft:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.TopLeft.
                    m_TextMeshPro.alignment = TextAlignmentOptions.TopLeft;
                    // Set m_frameCounter_transform.pivot to a Vector2 built from 0, 1.
                    m_frameCounter_transform.pivot = new Vector2(0, 1);
                    // Set m_frameCounter_transform.anchorMin to a Vector2 built from 0.01, 0.99.
                    m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.99f);
                    // Set m_frameCounter_transform.anchorMax to a Vector2 built from 0.01, 0.99.
                    m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.99f);
                    // Set m_frameCounter_transform.anchoredPosition to a Vector2 built from 0, 1.
                    m_frameCounter_transform.anchoredPosition = new Vector2(0, 1);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomLeft:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.BottomLeft.
                    m_TextMeshPro.alignment = TextAlignmentOptions.BottomLeft;
                    // Set m_frameCounter_transform.pivot to a Vector2 built from 0, 0.
                    m_frameCounter_transform.pivot = new Vector2(0, 0);
                    // Set m_frameCounter_transform.anchorMin to a Vector2 built from 0.01, 0.01.
                    m_frameCounter_transform.anchorMin = new Vector2(0.01f, 0.01f);
                    // Set m_frameCounter_transform.anchorMax to a Vector2 built from 0.01, 0.01.
                    m_frameCounter_transform.anchorMax = new Vector2(0.01f, 0.01f);
                    // Set m_frameCounter_transform.anchoredPosition to a Vector2 built from 0, 0.
                    m_frameCounter_transform.anchoredPosition = new Vector2(0, 0);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.TopRight:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.TopRight.
                    m_TextMeshPro.alignment = TextAlignmentOptions.TopRight;
                    // Set m_frameCounter_transform.pivot to a Vector2 built from 1, 1.
                    m_frameCounter_transform.pivot = new Vector2(1, 1);
                    // Set m_frameCounter_transform.anchorMin to a Vector2 built from 0.99, 0.99.
                    m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.99f);
                    // Set m_frameCounter_transform.anchorMax to a Vector2 built from 0.99, 0.99.
                    m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.99f);
                    // Set m_frameCounter_transform.anchoredPosition to a Vector2 built from 1, 1.
                    m_frameCounter_transform.anchoredPosition = new Vector2(1, 1);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomRight:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.BottomRight.
                    m_TextMeshPro.alignment = TextAlignmentOptions.BottomRight;
                    // Set m_frameCounter_transform.pivot to a Vector2 built from 1, 0.
                    m_frameCounter_transform.pivot = new Vector2(1, 0);
                    // Set m_frameCounter_transform.anchorMin to a Vector2 built from 0.99, 0.01.
                    m_frameCounter_transform.anchorMin = new Vector2(0.99f, 0.01f);
                    // Set m_frameCounter_transform.anchorMax to a Vector2 built from 0.99, 0.01.
                    m_frameCounter_transform.anchorMax = new Vector2(0.99f, 0.01f);
                    // Set m_frameCounter_transform.anchoredPosition to a Vector2 built from 1, 0.
                    m_frameCounter_transform.anchoredPosition = new Vector2(1, 0);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
            }
        }
    }
}