// Purpose: Creates many labels and floating counters for a text performance sample.
// Use: Attach in a sample scene and choose the number and type of labels.
// How it works: A startup loop spawns text objects and gives each a floating-text component.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{

    public class Benchmark02 : MonoBehaviour
    {

        // SpawnType: spawn type. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public int SpawnType = 0;
        // NumberOfNPC: number of npc. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int NumberOfNPC = 12;

        // IsTextObjectScaleStatic: is text object scale static. Assigned in the scene/prefab Inspector.
        public bool IsTextObjectScaleStatic;
        // floatingText_Script: floating text script. Start retrieves it from a TextMeshProFloatingText component
        // attached to go.
        private TextMeshProFloatingText floatingText_Script;


        // When the example starts, a startup loop spawns text objects and gives each a floating-text component.
        void Start()
        {

            // Repeat this block while i is below NumberOfNPC; start from int i = 0.
            for (int i = 0; i < NumberOfNPC; i++)
            {


                // If SpawnType equals 0, run the following block.
                if (SpawnType == 0)
                {
                    // TextMesh Pro Implementation
                    // Store a new GameObject named by Unity in local go.
                    GameObject go = new GameObject();
                    // Set go.transform.position to a Vector3 built from Unity random value between -95f and 95f,
                    // 0.25, Unity random value between -95f and 95f.
                    go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));

                    // Store a TextMeshPro component attached to go in local textMeshPro.
                    TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();

                    // Set textMeshPro.autoSizeTextContainer to true.
                    textMeshPro.autoSizeTextContainer = true;
                    // Set textMeshPro.rectTransform.pivot to a Vector2 built from 0.5, 0.
                    textMeshPro.rectTransform.pivot = new Vector2(0.5f, 0);

                    // Set textMeshPro.alignment to TextAlignmentOptions.Bottom.
                    textMeshPro.alignment = TextAlignmentOptions.Bottom;
                    // Set textMeshPro.fontSize to 96.
                    textMeshPro.fontSize = 96;
                    // Remove all entries from textMeshPro.fontFeatures.
                    textMeshPro.fontFeatures.Clear();

                    // Set textMeshPro.color to a Color32 built from 255, 255, 0, 255.
                    textMeshPro.color = new Color32(255, 255, 0, 255);
                    // Display "!" in textMeshPro.text (the assigned TMP/UI text component).
                    textMeshPro.text = "!";
                    // Copy IsTextObjectScaleStatic from this component into textMeshPro.isTextObjectScaleStatic.
                    textMeshPro.isTextObjectScaleStatic = IsTextObjectScaleStatic;

                    // Spawn Floating Text
                    // Store a TextMeshProFloatingText component attached to go in floatingText_Script.
                    floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
                    // Set floatingText_Script.SpawnType to 0.
                    floatingText_Script.SpawnType = 0;
                    // Copy IsTextObjectScaleStatic from this component into
                    // floatingText_Script.IsTextObjectScaleStatic.
                    floatingText_Script.IsTextObjectScaleStatic = IsTextObjectScaleStatic;
                }
                else if (SpawnType == 1)
                {
                    // TextMesh Implementation
                    // Store a new GameObject named by Unity in local go.
                    GameObject go = new GameObject();
                    // Set go.transform.position to a Vector3 built from Unity random value between -95f and 95f,
                    // 0.25, Unity random value between -95f and 95f.
                    go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.25f, Random.Range(-95f, 95f));

                    // Store a TextMesh component attached to go in local textMesh.
                    TextMesh textMesh = go.AddComponent<TextMesh>();
                    // Store the Font asset from a Resources folder at "Fonts/ARIAL" in textMesh.font.
                    textMesh.font = Resources.Load<Font>("Fonts/ARIAL");
                    // Set textMesh.GetComponent<Renderer>().sharedMaterial to textMesh.font material reference
                    // (Renderer.material may create an instance).
                    textMesh.GetComponent<Renderer>().sharedMaterial = textMesh.font.material;

                    // Set textMesh.anchor to TextAnchor.LowerCenter.
                    textMesh.anchor = TextAnchor.LowerCenter;
                    // Set textMesh.fontSize to 96.
                    textMesh.fontSize = 96;

                    // Set textMesh.color to a Color32 built from 255, 255, 0, 255.
                    textMesh.color = new Color32(255, 255, 0, 255);
                    // Display "!" in textMesh.text (the assigned TMP/UI text component).
                    textMesh.text = "!";

                    // Spawn Floating Text
                    // Store a TextMeshProFloatingText component attached to go in floatingText_Script.
                    floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
                    // Set floatingText_Script.SpawnType to 1.
                    floatingText_Script.SpawnType = 1;
                }
                else if (SpawnType == 2)
                {
                    // Canvas WorldSpace Camera
                    // Store a new GameObject named by Unity in local go.
                    GameObject go = new GameObject();
                    // Store a Canvas component attached to go in local canvas.
                    Canvas canvas = go.AddComponent<Canvas>();
                    // Set canvas.worldCamera to Unity first enabled camera tagged MainCamera.
                    canvas.worldCamera = Camera.main;

                    // Set go.transform.localScale to a Vector3 built from 0.1, 0.1, 0.1.
                    go.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
                    // Set go.transform.position to a Vector3 built from Unity random value between -95f and 95f,
                    // 5, Unity random value between -95f and 95f.
                    go.transform.position = new Vector3(Random.Range(-95f, 95f), 5f, Random.Range(-95f, 95f));

                    // Store a TextMeshProUGUI component attached to a new GameObject named by Unity in local
                    // textObject.
                    TextMeshProUGUI textObject = new GameObject().AddComponent<TextMeshProUGUI>();
                    // Parent textObject.rectTransform beneath Transform of go; keep its local coordinates.
                    textObject.rectTransform.SetParent(go.transform, false);

                    // Set textObject.color to a Color32 built from 255, 255, 0, 255.
                    textObject.color = new Color32(255, 255, 0, 255);
                    // Set textObject.alignment to TextAlignmentOptions.Bottom.
                    textObject.alignment = TextAlignmentOptions.Bottom;
                    // Set textObject.fontSize to 96.
                    textObject.fontSize = 96;
                    // Display "!" in textObject.text (the assigned TMP/UI text component).
                    textObject.text = "!";

                    // Spawn Floating Text
                    // Store a TextMeshProFloatingText component attached to go in floatingText_Script.
                    floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
                    // Set floatingText_Script.SpawnType to 0.
                    floatingText_Script.SpawnType = 0;
                }



            }
        }
    }
}
