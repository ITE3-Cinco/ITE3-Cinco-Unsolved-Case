// Purpose: Spawns example labels with animated floating counters.
// Use: Attach in a sample scene and choose SpawnType and NumberOfNPC.
// How it works: Create text objects at random positions and start their floating-text routines.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{
    
    public class TextMeshSpawner : MonoBehaviour
    {

        // SpawnType: spawn type. Assigned in the scene/prefab Inspector; the initializer below is the default.
        public int SpawnType = 0;
        // NumberOfNPC: number of npc. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public int NumberOfNPC = 12;

        // TheFont: the font. Assigned in the scene/prefab Inspector.
        public Font TheFont;

        // floatingText_Script: floating text script. Start retrieves it from a TextMeshProFloatingText component
        // attached to go.
        private TextMeshProFloatingText floatingText_Script;

        // This example leaves its early initialization callback empty.
        void Awake()
        {

        }

        // When the example starts, create text objects at random positions and start their floating-text routines.
        void Start()
        {

            // Repeat this block while i is below NumberOfNPC; start from int i = 0.
            for (int i = 0; i < NumberOfNPC; i++)
            {
                // If SpawnType equals 0, run the following block.
                if (SpawnType == 0)
                {
                    // TextMesh Pro Implementation     
                    //go.transform.localScale = new Vector3(2, 2, 2);
                    // Store a new GameObject named by Unity in local go.
                    GameObject go = new GameObject(); //"NPC " + i);
                    // Set go.transform.position to a Vector3 built from Unity random value between -95f and 95f,
                    // 0.5, Unity random value between -95f and 95f.
                    go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));

                    //go.transform.position = new Vector3(0, 1.01f, 0);
                    //go.renderer.castShadows = false;
                    //go.renderer.receiveShadows = false;
                    //go.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), 0);

                    // Store a TextMeshPro component attached to go in local textMeshPro.
                    TextMeshPro textMeshPro = go.AddComponent<TextMeshPro>();
                    //textMeshPro.FontAsset = Resources.Load("Fonts & Materials/LiberationSans SDF", typeof(TextMeshProFont)) as TextMeshProFont;
                    //textMeshPro.anchor = AnchorPositions.Bottom;
                    // Set textMeshPro.fontSize to 96.
                    textMeshPro.fontSize = 96;

                    // Display "!" in textMeshPro.text (the assigned TMP/UI text component).
                    textMeshPro.text = "!";
                    // Set textMeshPro.color to a Color32 built from 255, 255, 0, 255.
                    textMeshPro.color = new Color32(255, 255, 0, 255);
                    //textMeshPro.Text = "!";


                    // Spawn Floating Text
                    // Store a TextMeshProFloatingText component attached to go in floatingText_Script.
                    floatingText_Script = go.AddComponent<TextMeshProFloatingText>();
                    // Set floatingText_Script.SpawnType to 0.
                    floatingText_Script.SpawnType = 0;
                }
                else
                {
                    // TextMesh Implementation
                    // Store a new GameObject named by Unity in local go.
                    GameObject go = new GameObject(); //"NPC " + i);
                    // Set go.transform.position to a Vector3 built from Unity random value between -95f and 95f,
                    // 0.5, Unity random value between -95f and 95f.
                    go.transform.position = new Vector3(Random.Range(-95f, 95f), 0.5f, Random.Range(-95f, 95f));

                    //go.transform.position = new Vector3(0, 1.01f, 0);

                    // Store a TextMesh component attached to go in local textMesh.
                    TextMesh textMesh = go.AddComponent<TextMesh>();
                    // Set textMesh.GetComponent<Renderer>().sharedMaterial to TheFont material reference
                    // (Renderer.material may create an instance).
                    textMesh.GetComponent<Renderer>().sharedMaterial = TheFont.material;
                    // Copy TheFont from this component into textMesh.font.
                    textMesh.font = TheFont;
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
            }
        }

    }
}
