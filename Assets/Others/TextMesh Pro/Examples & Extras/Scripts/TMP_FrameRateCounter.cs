// Purpose: Displays frames per second and milliseconds per frame in world text.
// Use: Attach in a sample scene with a main camera and the bundled font resources.
// How it works: Count frames over real-time intervals and position the readout at a camera corner.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class TMP_FrameRateCounter : MonoBehaviour
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

        // m_TextMeshPro: text mesh pro. Awake retrieves it from a TextMeshPro component attached to frameCounter.
        private TextMeshPro m_TextMeshPro;
        // m_frameCounter_transform: frame counter transform. Awake sets it from Transform of frameCounter.
        private Transform m_frameCounter_transform;
        // m_camera: camera. Awake retrieves it from Unity first enabled camera tagged MainCamera.
        private Camera m_camera;

        // last_AnchorPosition: last anchor position. Awake sets it from AnchorPosition.
        private FpsCounterAnchorPositions last_AnchorPosition;

        // Create and format the counter text and place it at the chosen corner.
        void Awake()
        {
            // Stop this method early when not (enabled).
            if (!enabled)
                // Return now; skip the rest of this method.
                return;

            // Set m_camera to Unity first enabled camera tagged MainCamera.
            m_camera = Camera.main;
            // Set Application.targetFrameRate to 9999.
            Application.targetFrameRate = 9999;

            // Store a new GameObject named "Frame Counter" in local frameCounter.
            GameObject frameCounter = new GameObject("Frame Counter");

            // Store a TextMeshPro component attached to frameCounter in m_TextMeshPro.
            m_TextMeshPro = frameCounter.AddComponent<TextMeshPro>();
            // Store the TMP_FontAsset asset from a Resources folder at "Fonts & Materials/LiberationSans SDF" in
            // m_TextMeshPro.font.
            m_TextMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            // Store the Material asset from a Resources folder at "Fonts & Materials/LiberationSans SDF - Overlay"
            // in m_TextMeshPro.fontSharedMaterial.
            m_TextMeshPro.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");


            // Set m_frameCounter_transform to Transform of frameCounter.
            m_frameCounter_transform = frameCounter.transform;
            // Parent m_frameCounter_transform beneath Transform of m_camera.
            m_frameCounter_transform.SetParent(m_camera.transform);
            // Set m_frameCounter_transform.localRotation to the identity rotation (no rotation).
            m_frameCounter_transform.localRotation = Quaternion.identity;

            // Set m_TextMeshPro.textWrappingMode to TextWrappingModes.NoWrap.
            m_TextMeshPro.textWrappingMode = TextWrappingModes.NoWrap;
            // Set m_TextMeshPro.fontSize to 24.
            m_TextMeshPro.fontSize = 24;
            //m_TextMeshPro.FontColor = new Color32(255, 255, 255, 128);
            //m_TextMeshPro.edgeWidth = .15f;
            //m_TextMeshPro.isOverlay = true;

            //m_TextMeshPro.FaceColor = new Color32(255, 128, 0, 0);
            //m_TextMeshPro.EdgeColor = new Color32(0, 255, 0, 255);
            //m_TextMeshPro.FontMaterial.renderQueue = 4000;

            //m_TextMeshPro.CreateSoftShadowClone(new Vector2(1f, -1f));

            // Call TMP_FrameRateCounter.Set_FrameCounter_Position with AnchorPosition; place and align the readout
            // at the selected screen or viewport corner.
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
                // Call TMP_FrameRateCounter.Set_FrameCounter_Position with AnchorPosition; place and align the
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

                //string format = System.String.Format(htmlColorTag + "{0:F2} </color>FPS \n{1:F2} <#8080ff>MS",fps, ms);
                //m_TextMeshPro.text = format;

                // Call m_TextMeshPro.SetText using htmlColorTag plus fpsLabel, fps, ms.
                m_TextMeshPro.SetText(htmlColorTag + fpsLabel, fps, ms);

                // Set m_Frames to 0.
                m_Frames = 0;
                // Copy timeNow into m_LastInterval.
                m_LastInterval = timeNow;
            }
        }


        // Place and align the readout at the selected screen or viewport corner.
        // anchor_position is a FpsCounterAnchorPositions argument supplied by TMP_FrameRateCounter.Awake,
        // TMP_FrameRateCounter.Update.
        void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
        {
            //Debug.Log("Changing frame counter anchor position.");
            // Set m_TextMeshPro.margin to a new Vector4 initialized with 1, 1, 1, 1.
            m_TextMeshPro.margin = new Vector4(1f, 1f, 1f, 1f);

            // Choose the matching branch using anchor_position.
            switch (anchor_position)
            {
                case FpsCounterAnchorPositions.TopLeft:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.TopLeft.
                    m_TextMeshPro.alignment = TextAlignmentOptions.TopLeft;
                    // Set m_TextMeshPro.rectTransform.pivot to a Vector2 built from 0, 1.
                    m_TextMeshPro.rectTransform.pivot = new Vector2(0, 1);
                    // Store a world point from m_camera viewport position a Vector3 built from 0, 1, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0, 1, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomLeft:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.BottomLeft.
                    m_TextMeshPro.alignment = TextAlignmentOptions.BottomLeft;
                    // Set m_TextMeshPro.rectTransform.pivot to a Vector2 built from 0, 0.
                    m_TextMeshPro.rectTransform.pivot = new Vector2(0, 0);
                    // Store a world point from m_camera viewport position a Vector3 built from 0, 0, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0, 0, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.TopRight:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.TopRight.
                    m_TextMeshPro.alignment = TextAlignmentOptions.TopRight;
                    // Set m_TextMeshPro.rectTransform.pivot to a Vector2 built from 1, 1.
                    m_TextMeshPro.rectTransform.pivot = new Vector2(1, 1);
                    // Store a world point from m_camera viewport position a Vector3 built from 1, 1, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1, 1, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomRight:
                    // Set m_TextMeshPro.alignment to TextAlignmentOptions.BottomRight.
                    m_TextMeshPro.alignment = TextAlignmentOptions.BottomRight;
                    // Set m_TextMeshPro.rectTransform.pivot to a Vector2 built from 1, 0.
                    m_TextMeshPro.rectTransform.pivot = new Vector2(1, 0);
                    // Store a world point from m_camera viewport position a Vector3 built from 1, 0, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1, 0, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
            }
        }
    }
}
