// Purpose: Rotates the environment reflection matrix used by a text material.
// Use: Attach to TMP text using a material that supports _EnvMatrix.
// How it works: A coroutine updates the material matrix from rotation speeds every frame.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;

public class EnvMapAnimator : MonoBehaviour {

    //private Vector3 TranslationSpeeds;
    // RotationSpeeds: rotation speeds. Assigned in the scene/prefab Inspector.
    public Vector3 RotationSpeeds;
    // m_textMeshPro: text mesh pro. Awake retrieves it from the first TMP_Text component on this GameObject.
    private TMP_Text m_textMeshPro;
    // m_material: material. Awake sets it from m_textMeshPro.fontSharedMaterial.
    private Material m_material;
    

    // Find the TMP text and its shared material.
    void Awake()
    {
        //Debug.Log("Awake() on Script called.");
        // Store the first TMP_Text component on this GameObject in m_textMeshPro.
        m_textMeshPro = GetComponent<TMP_Text>();
        // Set m_material to m_textMeshPro.fontSharedMaterial.
        m_material = m_textMeshPro.fontSharedMaterial;
    }

	// Rotate the material environment matrix every frame using the configured speeds.
    // Use this for initialization
	IEnumerator Start ()
    {
        // Store a new Matrix4x4 instance in local matrix.
        Matrix4x4 matrix = new Matrix4x4(); 
        
        // Repeat while true.
        while (true)
        {
            //matrix.SetTRS(new Vector3 (Time.time * TranslationSpeeds.x, Time.time * TranslationSpeeds.y, Time.time * TranslationSpeeds.z), Quaternion.Euler(Time.time * RotationSpeeds.x, Time.time * RotationSpeeds.y , Time.time * RotationSpeeds.z), Vector3.one);
             // Call matrix.SetTRS using the zero vector (no movement or offset), a rotation built from angles
             // Time.time * RotationSpeeds.x, Time.time * RotationSpeeds.y, Time.time * RotationSpeeds.z in
             // degrees, a vector with all three components equal to 1.
             matrix.SetTRS(Vector3.zero, Quaternion.Euler(Time.time * RotationSpeeds.x, Time.time * RotationSpeeds.y , Time.time * RotationSpeeds.z), Vector3.one);

            // Set m_material shader matrix "_EnvMatrix" from matrix.
            m_material.SetMatrix("_EnvMatrix", matrix);

            // Pause the coroutine until the next Unity frame so the animation/sequence can render.
            yield return null;
        }
	}
}
