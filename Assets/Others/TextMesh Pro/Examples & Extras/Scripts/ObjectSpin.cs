// Purpose: Rotates, sweeps or translates a sample object.
// Use: Attach to the example object and choose Motion in the Inspector.
// How it works: Remember the starting pose, then apply the chosen movement each frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class ObjectSpin : MonoBehaviour
    {
        #pragma warning disable 0414
        public enum MotionType { Rotation, SearchLight, Translation };
        // Motion: motion. Assigned in the scene/prefab Inspector.
        public MotionType Motion;

        // TranslationDistance: translation distance. Assigned in the scene/prefab Inspector; the initializer below
        // is the default.
        public Vector3 TranslationDistance = new Vector3(5, 0, 0);
        // TranslationSpeed: translation speed. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public float TranslationSpeed = 1.0f;
        // SpinSpeed: spin speed. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public float SpinSpeed = 5;
        // RotationRange: rotation range. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int RotationRange = 15;
        // m_transform: transform. Awake sets it from the Transform on this component GameObject.
        private Transform m_transform;

        // m_time: time. Updated by Update.
        private float m_time;
        // m_prevPOS: prev pos. Update sets it from world position of m_transform.
        private Vector3 m_prevPOS;
        // m_initial_Rotation: initial rotation. Awake sets it from m_transform.rotation.eulerAngles.
        private Vector3 m_initial_Rotation;
        // m_initial_Position: initial position. Awake sets it from world position of m_transform.
        private Vector3 m_initial_Position;
        // m_lightColor: light color. Updated by Awake.
        private Color32 m_lightColor;

        // Remember the object starting pose and optional light color.
        void Awake()
        {
            // Copy transform into m_transform.
            m_transform = transform;
            // Set m_initial_Rotation to m_transform.rotation.eulerAngles.
            m_initial_Rotation = m_transform.rotation.eulerAngles;
            // Set m_initial_Position to world position of m_transform.
            m_initial_Position = m_transform.position;

            // Store the first Light component on this GameObject in local light.
            Light light = GetComponent<Light>();
            // Set m_lightColor to light.color when light exists; otherwise Color.black.
            m_lightColor = light != null ? light.color : Color.black;
        }


        // Rotate, sweep or translate the object according to its selected Motion mode.
        // Update is called once per frame
        void Update()
        {
            // Choose the matching branch using Motion.
            switch (Motion)
            {
                case MotionType.Rotation:
                    // Rotate m_transform by 0.
                    m_transform.Rotate(0, SpinSpeed * Time.deltaTime, 0);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case MotionType.SearchLight:
                    // Add SpinSpeed multiplied by Unity gameplay frame duration in seconds to m_time and store the
                    // updated value there.
                    m_time += SpinSpeed * Time.deltaTime;
                    // Store a rotation built from angles m_initial_Rotation X component, Mathf.Sin(m_time) *
                    // RotationRange + m_initial_Rotation.y (calculated from the values shown), m_initial_Rotation
                    // Z component in degrees in m_transform.rotation.
                    m_transform.rotation = Quaternion.Euler(m_initial_Rotation.x, Mathf.Sin(m_time) * RotationRange + m_initial_Rotation.y, m_initial_Rotation.z);
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
                case MotionType.Translation:
                    // Add TranslationSpeed multiplied by Unity gameplay frame duration in seconds to m_time and
                    // store the updated value there.
                    m_time += TranslationSpeed * Time.deltaTime;

                    // Store TranslationDistance X component multiplied by the cosine-wave value for m_time in
                    // local x.
                    float x = TranslationDistance.x * Mathf.Cos(m_time);
                    // Store TranslationDistance.y * Mathf.Sin(m_time) * Mathf.Cos(m_time * 1f) (calculated from
                    // the values shown) in local y.
                    float y = TranslationDistance.y * Mathf.Sin(m_time) * Mathf.Cos(m_time * 1f);
                    // Store TranslationDistance Z component multiplied by the sine-wave value for m_time in local
                    // z.
                    float z = TranslationDistance.z * Mathf.Sin(m_time);

                    // Set m_transform.position to m_initial_Position plus a Vector3 built from x, z, y.
                    m_transform.position = m_initial_Position + new Vector3(x, z, y);

                    // Drawing light patterns because they can be cool looking.
                    //if (Time.frameCount > 1)
                    //    Debug.DrawLine(m_transform.position, m_prevPOS, m_lightColor, 100f);

                    // Set m_prevPOS to world position of m_transform.
                    m_prevPOS = m_transform.position;
                    // Leave this loop or switch case so no later statements in that block run.
                    break;
            }
        }
    }
}