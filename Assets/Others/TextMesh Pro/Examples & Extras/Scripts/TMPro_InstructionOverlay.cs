// Purpose: Displays sample camera-control instructions near the camera view edge.
// Use: Attach in a TextMesh Pro sample scene with a main camera.
// How it works: Create camera-parented text and place it at the selected viewport corner.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{
    
    public class TMPro_InstructionOverlay : MonoBehaviour
    {

        public enum FpsCounterAnchorPositions { TopLeft, BottomLeft, TopRight, BottomRight };

        // AnchorPosition: anchor position. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public FpsCounterAnchorPositions AnchorPosition = FpsCounterAnchorPositions.BottomLeft;

        // instructions: instructions. Fixed script constant; callers cannot change it.
        private const string instructions = "Camera Control - <#ffff00>Shift + RMB\n</color>Zoom - <#ffff00>Mouse wheel.";

        // m_TextMeshPro: text mesh pro. Awake retrieves it from a TextMeshPro component attached to frameCounter.
        private TextMeshPro m_TextMeshPro;
        // m_textContainer: text container. Awake retrieves it from the first TextContainer component on
        // frameCounter.
        private TextContainer m_textContainer;
        // m_frameCounter_transform: frame counter transform. Awake sets it from Transform of frameCounter.
        private Transform m_frameCounter_transform;
        // m_camera: camera. Awake retrieves it from Unity first enabled camera tagged MainCamera.
        private Camera m_camera;

        //private FpsCounterAnchorPositions last_AnchorPosition;

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Stop this method early when not (enabled).
            if (!enabled)
                // Return now; skip the rest of this method.
                return;

            // Set m_camera to Unity first enabled camera tagged MainCamera.
            m_camera = Camera.main;

            // Store a new GameObject named "Frame Counter" in local frameCounter.
            GameObject frameCounter = new GameObject("Frame Counter");
            // Set m_frameCounter_transform to Transform of frameCounter.
            m_frameCounter_transform = frameCounter.transform;
            // Set m_frameCounter_transform.parent to Transform of m_camera.
            m_frameCounter_transform.parent = m_camera.transform;
            // Set m_frameCounter_transform.localRotation to the identity rotation (no rotation).
            m_frameCounter_transform.localRotation = Quaternion.identity;


            // Store a TextMeshPro component attached to frameCounter in m_TextMeshPro.
            m_TextMeshPro = frameCounter.AddComponent<TextMeshPro>();
            // Store the TMP_FontAsset asset from a Resources folder at "Fonts & Materials/LiberationSans SDF" in
            // m_TextMeshPro.font.
            m_TextMeshPro.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            // Store the Material asset from a Resources folder at "Fonts & Materials/LiberationSans SDF - Overlay"
            // in m_TextMeshPro.fontSharedMaterial.
            m_TextMeshPro.fontSharedMaterial = Resources.Load<Material>("Fonts & Materials/LiberationSans SDF - Overlay");

            // Set m_TextMeshPro.fontSize to 30.
            m_TextMeshPro.fontSize = 30;

            // Set m_TextMeshPro.isOverlay to true.
            m_TextMeshPro.isOverlay = true;
            // Store the first TextContainer component on frameCounter in m_textContainer.
            m_textContainer = frameCounter.GetComponent<TextContainer>();

            // Call TMPro_InstructionOverlay.Set_FrameCounter_Position with AnchorPosition; place and align the
            // readout at the selected screen or viewport corner.
            Set_FrameCounter_Position(AnchorPosition);
            //last_AnchorPosition = AnchorPosition;

            // Copy instructions cached by this component into m_TextMeshPro.text.
            m_TextMeshPro.text = instructions;

        }




        // Place and align the readout at the selected screen or viewport corner.
        // anchor_position is a FpsCounterAnchorPositions argument supplied by TMPro_InstructionOverlay.Awake.
        void Set_FrameCounter_Position(FpsCounterAnchorPositions anchor_position)
        {

            // Choose the matching branch using anchor_position.
            switch (anchor_position)
            {
                case FpsCounterAnchorPositions.TopLeft:
                    //m_TextMeshPro.anchor = AnchorPositions.TopLeft;
                    // Set m_textContainer.anchorPosition to TextContainerAnchors.TopLeft.
                    m_textContainer.anchorPosition = TextContainerAnchors.TopLeft;
                    // Store a world point from m_camera viewport position a Vector3 built from 0, 1, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0, 1, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomLeft:
                    //m_TextMeshPro.anchor = AnchorPositions.BottomLeft;
                    // Set m_textContainer.anchorPosition to TextContainerAnchors.BottomLeft.
                    m_textContainer.anchorPosition = TextContainerAnchors.BottomLeft;
                    // Store a world point from m_camera viewport position a Vector3 built from 0, 0, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(0, 0, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.TopRight:
                    //m_TextMeshPro.anchor = AnchorPositions.TopRight;
                    // Set m_textContainer.anchorPosition to TextContainerAnchors.TopRight.
                    m_textContainer.anchorPosition = TextContainerAnchors.TopRight;
                    // Store a world point from m_camera viewport position a Vector3 built from 1, 1, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1, 1, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case FpsCounterAnchorPositions.BottomRight:
                    //m_TextMeshPro.anchor = AnchorPositions.BottomRight;
                    // Set m_textContainer.anchorPosition to TextContainerAnchors.BottomRight.
                    m_textContainer.anchorPosition = TextContainerAnchors.BottomRight;
                    // Store a world point from m_camera viewport position a Vector3 built from 1, 0, 100.0f in
                    // m_frameCounter_transform.position.
                    m_frameCounter_transform.position = m_camera.ViewportToWorldPoint(new Vector3(1, 0, 100.0f));
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
            }
        }
    }
}
