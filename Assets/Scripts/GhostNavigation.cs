// Purpose: Builds a walkable navigation map from the assigned level environment.
// Use: Attach to an environment manager; it runs before enemy agents start.
// How it works: Solid colliders become NavMesh sources, and optional settings control enemy clearance.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity AI supplies NavMesh agents, paths and walkable-surface building/query tools.
using UnityEngine.AI;

/// <summary>Build the level's walkable surface before the ghost enables its agent.</summary>
[DefaultExecutionOrder(-200)]
public class GhostNavigation : MonoBehaviour
{
    // environmentRoot: Only this environment hierarchy contributes geometry; players, ghosts and UI are excluded.
    // Assigned in the scene/prefab Inspector.
    [Tooltip("Only this environment hierarchy contributes geometry; players, ghosts and UI are excluded.")]
    public Transform environmentRoot;
    // agentTypeID: navigation-agent type ID used to select bake/query settings. Assigned in the scene/prefab
    // Inspector.
    public int agentTypeID;

    // addMissingMeshColliders: Add mesh colliders to imported environment meshes that have no solid collider,
    // before building navigation. Assigned in the scene/prefab Inspector.
    [Tooltip("Add mesh colliders to imported environment meshes that have no solid collider, before building navigation.")]
    public bool addMissingMeshColliders;

    // voxelSize: Voxel size used for the runtime NavMesh bake. Smaller values are more accurate but significantly
    // slower. 0.25 is a good balance for corridor-scale levels. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Tooltip("Voxel size used for the runtime NavMesh bake. Smaller values are more accurate but significantly slower. 0.25 is a good balance for corridor-scale levels.")]
    [Min(0.05f)] public float voxelSize = 0.25f;

    // agentRadiusOverride: Optional scene-specific clearance. Zero preserves the project's agent settings.
    // Assigned in the scene/prefab Inspector.
    [Tooltip("Optional scene-specific clearance. Zero preserves the project's agent settings.")]
    [Min(0f)] public float agentRadiusOverride;
    // agentHeightOverride: optional height clearance for navigation; zero keeps the project value. Assigned in the
    // scene/prefab Inspector.
    [Min(0f)] public float agentHeightOverride;

    // data: NavMeshData generated from the scene environment. Awake retrieves it from the result of
    // NavMeshBuilder.BuildNavMeshData using settings, sources, bounds, the zero vector (no movement or offset),
    // the identity rotation (no rotation).
    private NavMeshData data;
    // instance: handle for the NavMesh data registered with Unity. Updated by Awake.
    private NavMeshDataInstance instance;

    /// <summary>Prepare environment colliders, collect geometry and build the runtime navigation surface.</summary>
    private void Awake()
    {
        // If environmentRoot is missing, run the following block.
        if (environmentRoot == null)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[GhostNavigation] Assign the level environment root.", this);
            // Return now; skip the rest of this method.
            return;
        }

        // glTF environments do not include physics colliders. Their actual mesh
        // surfaces preserve corridor openings for navigation and catch sight checks.
        // If addMissingMeshColliders, run the following block.
        if (addMissingMeshColliders)
        {
            // Process each filter in all MeshFilter components on or beneath environmentRoot.
            foreach (MeshFilter filter in environmentRoot.GetComponentsInChildren<MeshFilter>())
            {
                // If mesh asset referenced by filter is missing, skip this entry and continue with the next loop
                // iteration.
                if (filter.sharedMesh == null) continue;
                // Initialize hasSolidCollider to false.
                bool hasSolidCollider = false;
                // Process each collider in all Collider components on filter.
                foreach (Collider collider in filter.GetComponents<Collider>())
                    // If enabled state of collider and not (collider.isTrigger), set hasSolidCollider to true.
                    if (collider.enabled && !collider.isTrigger) hasSolidCollider = true;
                // If hasSolidCollider, skip this entry and continue with the next loop iteration.
                if (hasSolidCollider) continue;
                // Store a MeshCollider component attached to GameObject owning filter in local meshCollider.
                MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                // Set meshCollider.sharedMesh to mesh asset referenced by filter.
                meshCollider.sharedMesh = filter.sharedMesh;
            }
            // Update Unity Physics with Transform changes before collision queries.
            Physics.SyncTransforms();
        }

        // Collect only environment physics shapes; actors and UI are outside this hierarchy.
        // Store a new list with no entries in local sources.
        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        // Call NavMeshBuilder.CollectSources using environmentRoot, Unity default layers for raycast collision
        // queries, NavMeshCollectGeometry.PhysicsColliders, 0, a new list with no entries, sources.
        NavMeshBuilder.CollectSources(environmentRoot, Physics.DefaultRaycastLayers,
            NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
        // Runtime bakes cannot read non-readable imported meshes. Use their local
        // bounds conservatively (notably PoliceHQ's solid door leaves).
        // Repeat this block while i is below number of entries in sources; start from int i = 0.
        for (int i = 0; i < sources.Count; i++)
        {
            // Store sources[i] (entry read from sources) in local source.
            NavMeshBuildSource source = sources[i];
            // If source.shape equals NavMeshBuildSourceShape.Mesh and source.sourceObject is Mesh mesh and not
            // (mesh.isReadable), run the following block.
            if (source.shape == NavMeshBuildSourceShape.Mesh && source.sourceObject is Mesh mesh && !mesh.isReadable)
            {
                // Set source.shape to NavMeshBuildSourceShape.Box.
                source.shape = NavMeshBuildSourceShape.Box;
                // Multiply the current source.transform by the result of Matrix4x4.Translate using mesh.bounds
                // center and store the result back in source.transform.
                source.transform *= Matrix4x4.Translate(mesh.bounds.center);
                // Set source.size to mesh.bounds.size.
                source.size = mesh.bounds.size;
                // Clear source.sourceObject to null so it no longer holds a reference.
                source.sourceObject = null;
                // Copy source into sources[i].
                sources[i] = source;
            }
        }

        // Combine solid collider bounds to define the area that the navigation builder should inspect.
        // Store a new Bounds initialized with world position of environmentRoot, the zero vector (no movement or
        // offset) in local bounds.
        Bounds bounds = new Bounds(environmentRoot.position, Vector3.zero);
        // Initialize hasBounds to false.
        bool hasBounds = false;
        // Process each collider in all Collider components on or beneath environmentRoot.
        foreach (Collider collider in environmentRoot.GetComponentsInChildren<Collider>())
        {
            // Skip this loop entry when not (collider.enabled) or collider.isTrigger or ((1 <<
            // collider.gameObject.layer) & Physics.DefaultRaycastLayers) equals 0.
            if (!collider.enabled || collider.isTrigger ||
                ((1 << collider.gameObject.layer) & Physics.DefaultRaycastLayers) == 0) continue;
            // If not (hasBounds), run the following block.
            if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        // If not (hasBounds) or number of entries in sources equals 0, run the following block.
        if (!hasBounds || sources.Count == 0)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[GhostNavigation] No solid environment colliders found.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Allow a small margin and apply agent clearance and voxel settings before baking the surface.
        // Call bounds.Expand using 2.
        bounds.Expand(2f);
        // Store the result of NavMesh.GetSettingsByID using agentTypeID in local settings.
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeID);
        // If agentRadiusOverride is above 0, copy agentRadiusOverride from this component into
        // settings.agentRadius.
        if (agentRadiusOverride > 0f) settings.agentRadius = agentRadiusOverride;
        // If agentHeightOverride is above 0, copy agentHeightOverride from this component into
        // settings.agentHeight.
        if (agentHeightOverride > 0f) settings.agentHeight = agentHeightOverride;
        // Set settings.overrideVoxelSize to true.
        settings.overrideVoxelSize = true;
        // Use the configurable voxelSize instead of the previous hard-coded 0.1f,
        // which was unnecessarily fine and caused slow bake times.
        // Copy voxelSize from this component into settings.voxelSize.
        settings.voxelSize = voxelSize;
        // Store the result of NavMeshBuilder.BuildNavMeshData using settings, sources, bounds, the zero vector (no
        // movement or offset), the identity rotation (no rotation) in data.
        data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        // If data exists, store the result of NavMesh.AddNavMeshData using data in instance.
        if (data != null) instance = NavMesh.AddNavMeshData(data);
    }

    /// <summary>Remove this navigation surface and free its data when leaving the scene.</summary>
    private void OnDestroy()
    {
        // If instance.valid, remove  from instance.
        if (instance.valid) instance.Remove();
        // If data exists, schedule data for destruction at Unity end-of-frame cleanup.
        if (data != null) Destroy(data);
    }
}
