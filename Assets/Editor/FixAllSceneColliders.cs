using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Repairs known PoliceHQ hierarchy blockers without moving map geometry or
/// fitting a solid box around a group of meshes. Changes can be undone and
/// are left unsaved for inspection in the Physics Debugger.
/// </summary>
public static class FixAllSceneColliders
{
    [MenuItem("Tools/Fix All Scene Colliders")]
    public static void Execute()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
            return;

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Fix scene colliders");
        int fixCount = 0;

        // Work only in the active scene, including inactive nested objects.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                GameObject obj = child.gameObject;
                string name = obj.name;
                bool hinge = name.StartsWith("DOOR_Hinge_");

                // Imported empty transforms received default unit boxes. At
                // the door's 100x scale these occupy entire sections of the map.
                if ((hinge || name == "PoliceHeadquarters") &&
                    obj.GetComponent<MeshFilter>() == null &&
                    obj.GetComponent<Renderer>() == null)
                {
                    foreach (BoxCollider box in obj.GetComponents<BoxCollider>())
                    {
                        if (!box.enabled || box.isTrigger ||
                            box.center != Vector3.zero || box.size != Vector3.one)
                            continue;

                        if (hinge)
                            fixCount += EnsureDoorLeafColliders(obj);

                        Undo.RecordObject(box, "Disable hierarchy collider");
                        box.enabled = false;
                        RecordChange(box);
                        fixCount++;
                    }
                }

                bool marker = name.StartsWith("MARKER_");
                bool sample = name.StartsWith("artifact sample") || name.StartsWith("Agmiat sample");
                bool artifact = name == "Witness_Sketch_Note" ||
                    name == "Image_Artifact" || name == "Sketch_Artifact";

                if (marker || sample || artifact)
                {
                    foreach (Collider collider in obj.GetComponents<Collider>())
                    {
                        // Non-convex mesh colliders cannot be triggers.
                        if (collider.isTrigger || !(collider is BoxCollider || collider is CapsuleCollider))
                            continue;

                        Undo.RecordObject(collider, "Make marker or pickup non-blocking");
                        collider.isTrigger = true;
                        RecordChange(collider);
                        fixCount++;
                    }
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
        if (fixCount > 0)
            EditorSceneManager.MarkSceneDirty(scene);

        Debug.Log($"[FixAllSceneColliders] {fixCount} fix(es) in {scene.name}. Inspect and save the scene when ready.");
    }

    private static int EnsureDoorLeafColliders(GameObject hinge)
    {
        int count = 0;
        foreach (MeshFilter leaf in hinge.GetComponentsInChildren<MeshFilter>(true))
        {
            if (!leaf.name.StartsWith("DOOR_Leaf") || leaf.sharedMesh == null ||
                leaf.GetComponent<Collider>() != null)
                continue;

            // A non-convex collider preserves openings in the leaf mesh. These
            // environment doors are static; do not apply it to a dynamic body.
            Rigidbody body = leaf.GetComponentInParent<Rigidbody>();
            if (body != null && !body.isKinematic)
            {
                Debug.LogWarning($"[FixAllSceneColliders] {leaf.name} needs a compound collider for its dynamic Rigidbody.", leaf);
                continue;
            }

            MeshCollider collider = Undo.AddComponent<MeshCollider>(leaf.gameObject);
            collider.sharedMesh = leaf.sharedMesh;
            collider.convex = false;
            RecordChange(collider);
            count++;
        }
        return count;
    }

    private static void RecordChange(Object target)
    {
        EditorUtility.SetDirty(target);
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
