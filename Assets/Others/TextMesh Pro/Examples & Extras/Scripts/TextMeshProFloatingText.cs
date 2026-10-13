// Purpose: Displays a floating, fading countdown above a sample object.
// Use: Used by the text spawner examples; choose TMP or legacy TextMesh.
// How it works: Create the label, face it toward the camera and animate its height, value and opacity.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class TextMeshProFloatingText : MonoBehaviour
    {
        // TheFont: the font. Assigned in the scene/prefab Inspector.
        public Font TheFont;

        // m_floatingText: floating text. Updated by Awake.
        private GameObject m_floatingText;
        // m_textMeshPro: text mesh pro. Start retrieves it from a TextMeshPro component attached to
        // m_floatingText.
        private TextMeshPro m_textMeshPro;
        // m_textMesh: text mesh. Start retrieves it from a TextMesh component attached to m_floatingText.
        private TextMesh m_textMesh;

        // m_transform: transform. Awake sets it from the Transform on this component GameObject.
        private Transform m_transform;
        // m_floatingText_Transform: floating text transform. Start sets it from Transform of m_floatingText.
        private Transform m_floatingText_Transform;
        // m_cameraTransform: camera transform. Awake sets it from Transform of Camera.main.
        private Transform m_cameraTransform;

        // lastPOS: last pos. DisplayTextMeshProFloatingText sets it from world position of m_cameraTransform.
        Vector3 lastPOS = Vector3.zero;
        // lastRotation: last rotation. DisplayTextMeshProFloatingText sets it from world rotation of
        // m_cameraTransform.
        Quaternion lastRotation = Quaternion.identity;

        // SpawnType: spawn type. Assigned in the scene/prefab Inspector.
        public int SpawnType;
        // IsTextObjectScaleStatic: is text object scale static. Assigned in the scene/prefab Inspector.
        public bool IsTextObjectScaleStatic;

        //private int m_frame = 0;

        // k_WaitForEndOfFrame: k wait for end of frame. Shared across instances; scene reload alone does not reset
        // static state.
        static WaitForEndOfFrame k_WaitForEndOfFrame = new WaitForEndOfFrame();
        // k_WaitForSecondsRandom: k wait for seconds random. Shared across instances; scene reload alone does not
        // reset static state.
        static WaitForSeconds[] k_WaitForSecondsRandom = new WaitForSeconds[]
        {
            new WaitForSeconds(0.05f), new WaitForSeconds(0.1f), new WaitForSeconds(0.15f), new WaitForSeconds(0.2f), new WaitForSeconds(0.25f),
            new WaitForSeconds(0.3f), new WaitForSeconds(0.35f), new WaitForSeconds(0.4f), new WaitForSeconds(0.45f), new WaitForSeconds(0.5f),
            new WaitForSeconds(0.55f), new WaitForSeconds(0.6f), new WaitForSeconds(0.65f), new WaitForSeconds(0.7f), new WaitForSeconds(0.75f),
            new WaitForSeconds(0.8f), new WaitForSeconds(0.85f), new WaitForSeconds(0.9f), new WaitForSeconds(0.95f), new WaitForSeconds(1.0f),
        };

        // Before the example starts, cache its components and prepare the starting display.
        void Awake()
        {
            // Copy transform into m_transform.
            m_transform = transform;
            // Set m_floatingText to a new GameObject named this.name plus " floating text".
            m_floatingText = new GameObject(this.name + " floating text");

            // Reference to Transform is lost when TMP component is added since it replaces it by a RectTransform.
            //m_floatingText_Transform = m_floatingText.transform;
            //m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

            // Set m_cameraTransform to Transform of Camera.main.
            m_cameraTransform = Camera.main.transform;
        }

        // When the example starts, create the label, face it toward the camera and animate its height, value and opacity.
        void Start()
        {
            // If SpawnType equals 0, run the following block.
            if (SpawnType == 0)
            {
                // TextMesh Pro Implementation
                // Store a TextMeshPro component attached to m_floatingText in m_textMeshPro.
                m_textMeshPro = m_floatingText.AddComponent<TextMeshPro>();
                // Set m_textMeshPro.rectTransform.sizeDelta to a Vector2 built from 3, 3.
                m_textMeshPro.rectTransform.sizeDelta = new Vector2(3, 3);

                // Set m_floatingText_Transform to Transform of m_floatingText.
                m_floatingText_Transform = m_floatingText.transform;
                // Set m_floatingText_Transform.position to world position of m_transform plus a Vector3 built from
                // 0, 15, 0.
                m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

                //m_textMeshPro.fontAsset = Resources.Load("Fonts & Materials/JOKERMAN SDF", typeof(TextMeshProFont)) as TextMeshProFont; // User should only provide a string to the resource.
                //m_textMeshPro.fontSharedMaterial = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(Material)) as Material;

                // Set m_textMeshPro.alignment to TextAlignmentOptions.Center.
                m_textMeshPro.alignment = TextAlignmentOptions.Center;
                // Set m_textMeshPro.color to a Color32 built from Unity random value between 0 and 255 (integer
                // upper bound excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound excluded)
                // viewed as byte, 255.
                m_textMeshPro.color = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                // Set m_textMeshPro.fontSize to 24.
                m_textMeshPro.fontSize = 24;
                //m_textMeshPro.enableExtraPadding = true;
                //m_textMeshPro.enableShadows = false;
                // Remove all entries from m_textMeshPro.fontFeatures.
                m_textMeshPro.fontFeatures.Clear();
                // Display string.Empty in m_textMeshPro.text (the assigned TMP/UI text component).
                m_textMeshPro.text = string.Empty;
                // Copy IsTextObjectScaleStatic from this component into m_textMeshPro.isTextObjectScaleStatic.
                m_textMeshPro.isTextObjectScaleStatic = IsTextObjectScaleStatic;

                // Start coroutine DisplayTextMeshProFloatingText(); Unity resumes it after each yield.
                StartCoroutine(DisplayTextMeshProFloatingText());
            }
            else if (SpawnType == 1)
            {
                //Debug.Log("Spawning TextMesh Objects.");

                // Set m_floatingText_Transform to Transform of m_floatingText.
                m_floatingText_Transform = m_floatingText.transform;
                // Set m_floatingText_Transform.position to world position of m_transform plus a Vector3 built from
                // 0, 15, 0.
                m_floatingText_Transform.position = m_transform.position + new Vector3(0, 15f, 0);

                // Store a TextMesh component attached to m_floatingText in m_textMesh.
                m_textMesh = m_floatingText.AddComponent<TextMesh>();
                // Store the Font asset from a Resources folder at "Fonts/ARIAL" in m_textMesh.font.
                m_textMesh.font = Resources.Load<Font>("Fonts/ARIAL");
                // Set m_textMesh.GetComponent<Renderer>().sharedMaterial to m_textMesh.font material reference
                // (Renderer.material may create an instance).
                m_textMesh.GetComponent<Renderer>().sharedMaterial = m_textMesh.font.material;
                // Set m_textMesh.color to a Color32 built from Unity random value between 0 and 255 (integer upper
                // bound excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound
                // excluded) viewed as byte, Unity random value between 0 and 255 (integer upper bound excluded)
                // viewed as byte, 255.
                m_textMesh.color = new Color32((byte)Random.Range(0, 255), (byte)Random.Range(0, 255), (byte)Random.Range(0, 255), 255);
                // Set m_textMesh.anchor to TextAnchor.LowerCenter.
                m_textMesh.anchor = TextAnchor.LowerCenter;
                // Set m_textMesh.fontSize to 24.
                m_textMesh.fontSize = 24;

                // Start coroutine DisplayTextMeshFloatingText(); Unity resumes it after each yield.
                StartCoroutine(DisplayTextMeshFloatingText());
            }
            else if (SpawnType == 2)
            {

            }

        }


        //void Update()
        //{
        //    if (SpawnType == 0)
        //    {
        //        m_textMeshPro.SetText("{0}", m_frame);
        //    }
        //    else
        //    {
        //        m_textMesh.text = m_frame.ToString();
        //    }
        //    m_frame = (m_frame + 1) % 1000;

        //}


        // Animate a TMP countdown upward, face the camera and fade it before repeating.
        public IEnumerator DisplayTextMeshProFloatingText()
        {
            // Initialize CountDuration to 2.0.
            float CountDuration = 2.0f; // How long is the countdown alive.
            // Store Unity random value between 5 and 20 in local starting_Count.
            float starting_Count = Random.Range(5f, 20f); // At what number is the counter starting at.
            // Keep starting_Count in local current_Count.
            float current_Count = starting_Count;

            // Store world position of m_floatingText_Transform in local start_pos.
            Vector3 start_pos = m_floatingText_Transform.position;
            // Store m_textMeshPro.color in local start_color.
            Color32 start_color = m_textMeshPro.color;
            // Initialize alpha to 255.
            float alpha = 255;
            // Initialize int_counter to 0.
            int int_counter = 0;


            // Store 3 divided by starting_Count multiplied by CountDuration in local fadeDuration.
            float fadeDuration = 3 / starting_Count * CountDuration;

            // Repeat while current_Count is above 0.
            while (current_Count > 0)
            {
                // Subtract (Time.deltaTime / CountDuration) * starting_Count (calculated from the values shown)
                // from current_Count and store the updated value there.
                current_Count -= (Time.deltaTime / CountDuration) * starting_Count;

                // If current_Count is at most 3, run the following block.
                if (current_Count <= 3)
                {
                    //Debug.Log("Fading Counter ... " + current_Count.ToString("f2"));
                    // Store alpha minus (Time.deltaTime / fadeDuration) * 255 limited between 0 and 255 in alpha.
                    alpha = Mathf.Clamp(alpha - (Time.deltaTime / fadeDuration) * 255, 0, 255);
                }

                // Set int_counter to current_Count viewed as int.
                int_counter = (int)current_Count;
                // Display the result of int_counter.ToString in m_textMeshPro.text (the assigned TMP/UI text
                // component).
                m_textMeshPro.text = int_counter.ToString();
                //m_textMeshPro.SetText("{0}", (int)current_Count);

                // Set m_textMeshPro.color to a Color32 built from start_color red component, start_color green
                // component, start_color blue component, alpha viewed as byte.
                m_textMeshPro.color = new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha);

                // Move the floating text upward each update
                // Add a Vector3 built from 0, starting_Count multiplied by Unity gameplay frame duration in
                // seconds, 0 to m_floatingText_Transform.position and store the updated value there.
                m_floatingText_Transform.position += new Vector3(0, starting_Count * Time.deltaTime, 0);

                // Align floating text perpendicular to Camera.
                // If not (the result of lastPOS.Compare using m_cameraTransform.position, 1000) or not (the result
                // of lastRotation.Compare using m_cameraTransform.rotation, 1000), run the following block.
                if (!lastPOS.Compare(m_cameraTransform.position, 1000) || !lastRotation.Compare(m_cameraTransform.rotation, 1000))
                {
                    // Set lastPOS to world position of m_cameraTransform.
                    lastPOS = m_cameraTransform.position;
                    // Set lastRotation to world rotation of m_cameraTransform.
                    lastRotation = m_cameraTransform.rotation;
                    // Copy lastRotation cached by this component into m_floatingText_Transform.rotation.
                    m_floatingText_Transform.rotation = lastRotation;
                    // Store world position of m_transform minus lastPOS in local dir.
                    Vector3 dir = m_transform.position - lastPOS;
                    // Set m_transform.forward to a Vector3 built from dir X component, 0, dir Z component.
                    m_transform.forward = new Vector3(dir.x, 0, dir.z);
                }

                // Yield k_WaitForEndOfFrame; Unity resumes the coroutine after it completes.
                yield return k_WaitForEndOfFrame;
            }

            //Debug.Log("Done Counting down.");

            // Yield k_WaitForSecondsRandom[Random.Range(0, 19)] (entry read from k_WaitForSecondsRandom); Unity
            // resumes the coroutine after it completes.
            yield return k_WaitForSecondsRandom[Random.Range(0, 19)];

            // Copy start_pos into m_floatingText_Transform.position.
            m_floatingText_Transform.position = start_pos;

            // Start coroutine DisplayTextMeshProFloatingText(); Unity resumes it after each yield.
            StartCoroutine(DisplayTextMeshProFloatingText());
        }


        // Animate the legacy TextMesh countdown upward and fade it before repeating.
        public IEnumerator DisplayTextMeshFloatingText()
        {
            // Initialize CountDuration to 2.0.
            float CountDuration = 2.0f; // How long is the countdown alive.
            // Store Unity random value between 5 and 20 in local starting_Count.
            float starting_Count = Random.Range(5f, 20f); // At what number is the counter starting at.
            // Keep starting_Count in local current_Count.
            float current_Count = starting_Count;

            // Store world position of m_floatingText_Transform in local start_pos.
            Vector3 start_pos = m_floatingText_Transform.position;
            // Store m_textMesh.color in local start_color.
            Color32 start_color = m_textMesh.color;
            // Initialize alpha to 255.
            float alpha = 255;
            // Initialize int_counter to 0.
            int int_counter = 0;

            // Store 3 divided by starting_Count multiplied by CountDuration in local fadeDuration.
            float fadeDuration = 3 / starting_Count * CountDuration;

            // Repeat while current_Count is above 0.
            while (current_Count > 0)
            {
                // Subtract (Time.deltaTime / CountDuration) * starting_Count (calculated from the values shown)
                // from current_Count and store the updated value there.
                current_Count -= (Time.deltaTime / CountDuration) * starting_Count;

                // If current_Count is at most 3, run the following block.
                if (current_Count <= 3)
                {
                    //Debug.Log("Fading Counter ... " + current_Count.ToString("f2"));
                    // Store alpha minus (Time.deltaTime / fadeDuration) * 255 limited between 0 and 255 in alpha.
                    alpha = Mathf.Clamp(alpha - (Time.deltaTime / fadeDuration) * 255, 0, 255);
                }

                // Set int_counter to current_Count viewed as int.
                int_counter = (int)current_Count;
                // Display the result of int_counter.ToString in m_textMesh.text (the assigned TMP/UI text
                // component).
                m_textMesh.text = int_counter.ToString();
                //Debug.Log("Current Count:" + current_Count.ToString("f2"));

                // Set m_textMesh.color to a Color32 built from start_color red component, start_color green
                // component, start_color blue component, alpha viewed as byte.
                m_textMesh.color = new Color32(start_color.r, start_color.g, start_color.b, (byte)alpha);

                // Move the floating text upward each update
                // Add a Vector3 built from 0, starting_Count multiplied by Unity gameplay frame duration in
                // seconds, 0 to m_floatingText_Transform.position and store the updated value there.
                m_floatingText_Transform.position += new Vector3(0, starting_Count * Time.deltaTime, 0);

                // Align floating text perpendicular to Camera.
                // If not (the result of lastPOS.Compare using m_cameraTransform.position, 1000) or not (the result
                // of lastRotation.Compare using m_cameraTransform.rotation, 1000), run the following block.
                if (!lastPOS.Compare(m_cameraTransform.position, 1000) || !lastRotation.Compare(m_cameraTransform.rotation, 1000))
                {
                    // Set lastPOS to world position of m_cameraTransform.
                    lastPOS = m_cameraTransform.position;
                    // Set lastRotation to world rotation of m_cameraTransform.
                    lastRotation = m_cameraTransform.rotation;
                    // Copy lastRotation cached by this component into m_floatingText_Transform.rotation.
                    m_floatingText_Transform.rotation = lastRotation;
                    // Store world position of m_transform minus lastPOS in local dir.
                    Vector3 dir = m_transform.position - lastPOS;
                    // Set m_transform.forward to a Vector3 built from dir X component, 0, dir Z component.
                    m_transform.forward = new Vector3(dir.x, 0, dir.z);
                }

                // Yield k_WaitForEndOfFrame; Unity resumes the coroutine after it completes.
                yield return k_WaitForEndOfFrame;
            }

            //Debug.Log("Done Counting down.");

            // Yield k_WaitForSecondsRandom[Random.Range(0, 20)] (entry read from k_WaitForSecondsRandom); Unity
            // resumes the coroutine after it completes.
            yield return k_WaitForSecondsRandom[Random.Range(0, 20)];

            // Copy start_pos into m_floatingText_Transform.position.
            m_floatingText_Transform.position = start_pos;

            // Start coroutine DisplayTextMeshFloatingText(); Unity resumes it after each yield.
            StartCoroutine(DisplayTextMeshFloatingText());
        }
    }
}
