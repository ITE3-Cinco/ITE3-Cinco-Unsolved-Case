using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Comprehensive one-shot scene collider fixer for PoliceHQ.
/// Searches ALL GameObjects in the scene recursively regardless of hierarchy depth.
///
/// Pass 1 — ARCH_ architectural pieces: fit BoxCollider to mesh bounds.
/// Pass 2 — CEILING_Corridor tiles: fit BoxCollider to mesh bounds.
/// Pass 3 — MARKER_* objects: BoxCollider.isTrigger = true.
/// Pass 4 — artifact sample / Agmiat sample: CapsuleCollider.isTrigger = true.
/// Pass 5 — Interactable artifact world objects: BoxCollider.isTrigger = true + negative scale fix.
/// Pass 6 — Wall Y-snap: snap wall bottom Y to dominant levels (0.40 / 1.60).
/// </summary>
public class FixAllSceneColliders
{
    const float WALL_GREEN_BOTTOM_Y   = 0.40f;
    const float WALL_PLASTER_BOTTOM_Y = 1.60f;
    const float SNAP_TOLERANCE        = 0.12f;

    [MenuItem("Tools/Fix All Scene Colliders")]
    public static void Execute()
    {
        int fixCount = 0;

        // Use Resources to get every GameObject in the scene, including
        // deeply nested children (Floors/, Ceiling/, etc.)
        GameObject[] all = GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

        foreach (GameObject obj in all)
        {
            string n = obj.name;

            // ----------------------------------------------------------
            // Pass 1 & 2: ARCH_ and CEILING_ — fit BoxCollider to mesh
            // ----------------------------------------------------------
            if (n.StartsWith("ARCH_") || n.StartsWith("CEILING_"))
            {
                MeshFilter mf = obj.GetComponent<MeshFilter>();
                BoxCollider bc = obj.GetComponent<BoxCollider>();

                if (mf != null && mf.sharedMesh != null && bc != null)
                {
                    Bounds mb = mf.sharedMesh.bounds;
                    bool centerWrong = (bc.center - mb.center).sqrMagnitude > 1e-6f;
                    bool sizeWrong   = (bc.size   - mb.size  ).sqrMagnitude > 1e-6f;

                    if (centerWrong || sizeWrong)
                    {
                        Undo.RecordObject(bc, $"Fix {n} collider");
                        bc.center = mb.center;
                        bc.size   = mb.size;
                        bc.providesContacts = false;
                        EditorUtility.SetDirty(bc);
                        Debug.Log($"[Fix] {n}: BoxCollider fitted to mesh bounds size={mb.size}");
                        fixCount++;
                    }
                }

                // Pass 6: snap wall bottom Y
                if (n.StartsWith("ARCH_Wall_"))
                {
                    MeshRenderer mr = obj.GetComponent<MeshRenderer>();
                    if (mr != null)
                    {
                        float worldBottomY = mr.bounds.min.y;
                        float targetY = -1f;

                        if (Mathf.Abs(worldBottomY - WALL_GREEN_BOTTOM_Y) <= SNAP_TOLERANCE)
                            targetY = WALL_GREEN_BOTTOM_Y;
                        else if (Mathf.Abs(worldBottomY - WALL_PLASTER_BOTTOM_Y) <= SNAP_TOLERANCE)
                            targetY = WALL_PLASTER_BOTTOM_Y;

                        if (targetY >= 0f && Mathf.Abs(worldBottomY - targetY) > 0.005f)
                        {
                            float delta = targetY - worldBottomY;
                            Undo.RecordObject(obj.transform, $"Snap {n} Y");
                            Vector3 wp = obj.transform.position;
                            obj.transform.position = new Vector3(wp.x, wp.y + delta, wp.z);
                            EditorUtility.SetDirty(obj.transform);
                            Debug.Log($"[Fix] {n}: wall bottom Y snapped {worldBottomY:F3} -> {targetY:F3}");
                            fixCount++;
                        }
                    }
                }

                continue;
            }

            // ----------------------------------------------------------
            // Pass 3: MARKER_* — must never block the player
            // ----------------------------------------------------------
            if (n.StartsWith("MARKER_"))
            {
                BoxCollider bc = obj.GetComponent<BoxCollider>();
                if (bc != null && !bc.isTrigger)
                {
                    Undo.RecordObject(bc, $"Fix {n} isTrigger");
                    bc.isTrigger = true;
                    EditorUtility.SetDirty(bc);
                    Debug.Log($"[Fix] {n}: BoxCollider.isTrigger = true");
                    fixCount++;
                }
                continue;
            }

            // ----------------------------------------------------------
            // Pass 4: artifact sample / Agmiat sample — CapsuleCollider trigger
            // ----------------------------------------------------------
            if (n.StartsWith("artifact sample") || n.StartsWith("Agmiat sample"))
            {
                CapsuleCollider cc = obj.GetComponent<CapsuleCollider>();
                if (cc != null && !cc.isTrigger)
                {
                    Undo.RecordObject(cc, $"Fix {n} CapsuleCollider isTrigger");
                    cc.isTrigger = true;
                    EditorUtility.SetDirty(cc);
                    Debug.Log($"[Fix] {n}: CapsuleCollider.isTrigger = true");
                    fixCount++;
                }
                continue;
            }

            // ----------------------------------------------------------
            // Pass 5: Interactable artifact world objects
            // ----------------------------------------------------------
            if (n == "Witness_Sketch_Note" || n == "Image_Artifact" || n == "Sketch_Artifact")
            {
                BoxCollider bc = obj.GetComponent<BoxCollider>();
                if (bc != null && !bc.isTrigger)
                {
                    Undo.RecordObject(bc, $"Fix {n} BoxCollider isTrigger");
                    bc.isTrigger = true;
                    EditorUtility.SetDirty(bc);
                    fixCount++;
                }

                Vector3 sc = obj.transform.localScale;
                if (sc.x < 0f || sc.y < 0f || sc.z < 0f)
                {
                    Undo.RecordObject(obj.transform, $"Fix {n} negative scale");
                    obj.transform.localScale = new Vector3(
                        Mathf.Abs(sc.x), Mathf.Abs(sc.y), Mathf.Abs(sc.z));
                    EditorUtility.SetDirty(obj.transform);
                    fixCount++;
                }
            }
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        Debug.Log($"[FixAllSceneColliders] Complete. {fixCount} fix(es) applied. Scene saved.");
    }
}
