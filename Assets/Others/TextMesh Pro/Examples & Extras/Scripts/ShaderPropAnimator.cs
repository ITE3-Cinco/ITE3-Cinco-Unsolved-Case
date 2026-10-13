// Purpose: Animates a sample material glow using an animation curve.
// Use: Attach to a Renderer with a glow-capable material and assign GlowCurve.
// How it works: Create a material instance and sample the curve repeatedly to change glow power.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{
    
    public class ShaderPropAnimator : MonoBehaviour
    {

        // m_Renderer: renderer. Awake retrieves it from the first Renderer component on this GameObject.
        private Renderer m_Renderer;
        // m_Material: material. Awake sets it from m_Renderer material reference (Renderer.material may create an
        // instance).
        private Material m_Material;

        // GlowCurve: glow curve. Assigned in the scene/prefab Inspector.
        public AnimationCurve GlowCurve;

        // m_frame: frame. Updated by AnimateProperties.
        public float m_frame;

        // Cache the Renderer and create a material instance for this object.
        void Awake()
        {
            // Cache a reference to object's renderer
            // Store the first Renderer component on this GameObject in m_Renderer.
            m_Renderer = GetComponent<Renderer>();

            // Cache a reference to object's material and create an instance by doing so.
            // Set m_Material to m_Renderer material reference (Renderer.material may create an instance).
            m_Material = m_Renderer.material;
        }

        // Start the repeating glow animation.
        void Start()
        {
            // Start coroutine AnimateProperties(); Unity resumes it after each yield.
            StartCoroutine(AnimateProperties());
        }

        // Loop through the glow curve and apply its value to the material each frame.
        IEnumerator AnimateProperties()
        {
            //float lightAngle;
            // Declare local glowPower to hold float data supplied later in this method.
            float glowPower;
            // Store Unity random value between 0 and 1 in m_frame.
            m_frame = Random.Range(0f, 1f);

            // Repeat while true.
            while (true)
            {
                //lightAngle = (m_Material.GetFloat(ShaderPropertyIDs.ID_LightAngle) + Time.deltaTime) % 6.2831853f;
                //m_Material.SetFloat(ShaderPropertyIDs.ID_LightAngle, lightAngle);

                // Store the curve/graph value from GlowCurve at m_frame in glowPower.
                glowPower = GlowCurve.Evaluate(m_frame);
                // Set m_Material numeric property ShaderUtilities.ID_GlowPower from glowPower.
                m_Material.SetFloat(ShaderUtilities.ID_GlowPower, glowPower);

                // Add Unity gameplay frame duration in seconds multiplied by Unity random value between 0.2f and
                // 0.3f to m_frame and store the updated value there.
                m_frame += Time.deltaTime * Random.Range(0.2f, 0.3f);
                // Yield a new WaitForEndOfFrame instance; Unity resumes the coroutine after it completes.
                yield return new WaitForEndOfFrame();
            }
        }
    }
}
