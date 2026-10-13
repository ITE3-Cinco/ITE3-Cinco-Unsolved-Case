// Purpose: Repairs known collider blockers in the active PoliceHQ scene.
// Use: Run Tools > Fix All Scene Colliders in Unity Edit Mode, then inspect before saving.
// How it works: Undo records changes; empty hierarchy boxes are disabled and pickups become non-blocking triggers.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// UnityEditor supplies Edit Mode tools and asset/prefab operations; this code runs in the Editor.
using UnityEditor;
// Unity Editor scene APIs open, close and mark scenes for saving.
using UnityEditor.SceneManagement;
// Unity SceneManagement supplies scene data, scene-loaded events and scene-loading calls.
using UnityEngine.SceneManagement;

/// <summary>
/// Repairs known PoliceHQ hierarchy blockers without moving map geometry or
/// fitting a solid box around a group of meshes. Changes can be undone and
/// are left unsaved for inspection in the Physics Debugger.
/// </summary>
public static class FixAllSceneColliders
{
    /// <summary>Inspect the active scene, repair known blockers and leave changes unsaved for review.</summary>
    [MenuItem("Tools/Fix All Scene Colliders")]
    public static void Execute()
    {
        // Stop this method early when EditorApplication.isPlayingOrWillChangePlaymode.
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            // Return now; skip the rest of this method.
            return;

        // Store the result of SceneManager.GetActiveScene in local scene.
        Scene scene = SceneManager.GetActiveScene();
        // Stop this method early when not (scene is a valid initialized Unity handle/object) or not
        // (scene.isLoaded).
        if (!scene.IsValid() || !scene.isLoaded)
            // Return now; skip the rest of this method.
            return;

        // Call Undo.IncrementCurrentGroup.
        Undo.IncrementCurrentGroup();
        // Store the result of Undo.GetCurrentGroup in local undoGroup.
        int undoGroup = Undo.GetCurrentGroup();
        // Call Undo.SetCurrentGroupName using "Fix scene colliders".
        Undo.SetCurrentGroupName("Fix scene colliders");
        // Initialize fixCount to 0.
        int fixCount = 0;

        // Work only in the active scene, including inactive nested objects.
        // Process each root in the top-level GameObjects belonging to scene.
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            // Process each child in all Transform components on or beneath root (including inactive children).
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                // Store GameObject owning child in local obj.
                GameObject obj = child.gameObject;
                // Store obj.name in local name.
                string name = obj.name;
                // Store whether name begins with "DOOR_Hinge_" in local hinge.
                bool hinge = name.StartsWith("DOOR_Hinge_");

                // Imported empty transforms received default unit boxes. At
                // the door's 100x scale these occupy entire sections of the map.
                // If (hinge || name == "PoliceHeadquarters") and obj.GetComponent<MeshFilter>() is missing and the
                // first Renderer component on obj is missing, run the following block.
                if ((hinge || name == "PoliceHeadquarters") &&
                    obj.GetComponent<MeshFilter>() == null &&
                    obj.GetComponent<Renderer>() == null)
                {
                    // Process each box in all BoxCollider components on obj.
                    foreach (BoxCollider box in obj.GetComponents<BoxCollider>())
                    {
                        // Skip this loop entry when !box.enabled or box.isTrigger or box.center differs from the
                        // zero vector (no movement or offset) or box.size differs from a vector with all three
                        // components equal to 1.
                        if (!box.enabled || box.isTrigger ||
                            box.center != Vector3.zero || box.size != Vector3.one)
                            // Skip this entry and continue with the next loop iteration.
                            continue;

                        // If hinge, run the following block.
                        if (hinge)
                            // Add the result of FixAllSceneColliders.EnsureDoorLeafColliders using obj to fixCount
                            // and store the updated value there.
                            fixCount += EnsureDoorLeafColliders(obj);

                        // Call Undo.RecordObject using box, "Disable hierarchy collider".
                        Undo.RecordObject(box, "Disable hierarchy collider");
                        // Disable box by changing its component enabled flag.
                        box.enabled = false;
                        // Call FixAllSceneColliders.RecordChange with box; record prefab overrides and mark the
                        // changed object dirty for later saving.
                        RecordChange(box);
                        // Increase fixCount by one, storing the next counter/index value.
                        fixCount++;
                    }
                }

                // Store whether name begins with "MARKER_" in local marker.
                bool marker = name.StartsWith("MARKER_");
                // Store whether name begins with "artifact sample" or whether name begins with "Agmiat sample" in
                // local sample.
                bool sample = name.StartsWith("artifact sample") || name.StartsWith("Agmiat sample");
                // Store name equals "Witness_Sketch_Note" or name equals "Image_Artifact" or name equals
                // "Sketch_Artifact" in local artifact.
                bool artifact = name == "Witness_Sketch_Note" ||
                    name == "Image_Artifact" || name == "Sketch_Artifact";

                // If marker or sample or artifact, run the following block.
                if (marker || sample || artifact)
                {
                    // Process each collider in all Collider components on obj.
                    foreach (Collider collider in obj.GetComponents<Collider>())
                    {
                        // Non-convex mesh colliders cannot be triggers.
                        // Skip this loop entry when collider.isTrigger or not ((collider is BoxCollider ||
                        // collider is CapsuleCollider)).
                        if (collider.isTrigger || !(collider is BoxCollider || collider is CapsuleCollider))
                            // Skip this entry and continue with the next loop iteration.
                            continue;

                        // Call Undo.RecordObject using collider, "Make marker or pickup non-blocking".
                        Undo.RecordObject(collider, "Make marker or pickup non-blocking");
                        // Set collider.isTrigger to true.
                        collider.isTrigger = true;
                        // Call FixAllSceneColliders.RecordChange with collider; record prefab overrides and mark
                        // the changed object dirty for later saving.
                        RecordChange(collider);
                        // Increase fixCount by one, storing the next counter/index value.
                        fixCount++;
                    }
                }
            }
        }

        // Call Undo.CollapseUndoOperations using undoGroup.
        Undo.CollapseUndoOperations(undoGroup);
        // If fixCount is above 0, run the following block.
        if (fixCount > 0)
            // Call EditorSceneManager.MarkSceneDirty using scene.
            EditorSceneManager.MarkSceneDirty(scene);

        // Write the diagnostic message to the Unity Console; it does not update game UI.
        Debug.Log($"[FixAllSceneColliders] {fixCount} fix(es) in {scene.name}. Inspect and save the scene when ready.");
    }

    /// <summary>Add solid mesh colliders to static door leaves before disabling the hinge box.</summary>
    // hinge is a GameObject argument supplied by FixAllSceneColliders.Execute.
    private static int EnsureDoorLeafColliders(GameObject hinge)
    {
        // Initialize count to 0.
        int count = 0;
        // Process each leaf in all MeshFilter components on or beneath hinge (including inactive children).
        foreach (MeshFilter leaf in hinge.GetComponentsInChildren<MeshFilter>(true))
        {
            // Skip this loop entry when not (leaf.name.StartsWith("DOOR_Leaf")) or leaf.sharedMesh is missing or
            // the first Collider component on leaf exists.
            if (!leaf.name.StartsWith("DOOR_Leaf") || leaf.sharedMesh == null ||
                leaf.GetComponent<Collider>() != null)
                // Skip this entry and continue with the next loop iteration.
                continue;

            // A non-convex collider preserves openings in the leaf mesh. These
            // environment doors are static; do not apply it to a dynamic body.
            // Store the first Rigidbody component on or above leaf in local body.
            Rigidbody body = leaf.GetComponentInParent<Rigidbody>();
            // If body exists and not (body.isKinematic), run the following block.
            if (body != null && !body.isKinematic)
            {
                // Report the missing/invalid setup or state as a Unity Console warning.
                Debug.LogWarning($"[FixAllSceneColliders] {leaf.name} needs a compound collider for its dynamic Rigidbody.", leaf);
                // Skip this entry and continue with the next loop iteration.
                continue;
            }

            // Store a MeshCollider component attached to Undo in local collider.
            MeshCollider collider = Undo.AddComponent<MeshCollider>(leaf.gameObject);
            // Set collider.sharedMesh to mesh asset referenced by leaf.
            collider.sharedMesh = leaf.sharedMesh;
            // Set collider.convex to false.
            collider.convex = false;
            // Call FixAllSceneColliders.RecordChange with collider; record prefab overrides and mark the changed
            // object dirty for later saving.
            RecordChange(collider);
            // Increase count by one, storing the next counter/index value.
            count++;
        }
        // Return count to the method caller.
        return count;
    }

    /// <summary>Record prefab overrides and mark the changed object dirty for later saving.</summary>
    // target is a Object argument supplied by FixAllSceneColliders.EnsureDoorLeafColliders,
    // FixAllSceneColliders.Execute.
    private static void RecordChange(Object target)
    {
        // Call EditorUtility.SetDirty using target.
        EditorUtility.SetDirty(target);
        // If the result of PrefabUtility.IsPartOfPrefabInstance using target, run the following block.
        if (PrefabUtility.IsPartOfPrefabInstance(target))
            // Call PrefabUtility.RecordPrefabInstancePropertyModifications using target.
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
    }
}
