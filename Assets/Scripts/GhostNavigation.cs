using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>Build the level's walkable surface before the ghost enables its agent.</summary>
[DefaultExecutionOrder(-200)]
public class GhostNavigation : MonoBehaviour
{
    [Tooltip("Only this environment hierarchy contributes geometry; players, ghosts and UI are excluded.")]
    public Transform environmentRoot;
    public int agentTypeID;

    [Tooltip("Add mesh colliders to imported environment meshes that have no solid collider, before building navigation.")]
    public bool addMissingMeshColliders;

    [Tooltip("Voxel size used for the runtime NavMesh bake. Smaller values are more accurate but significantly slower. 0.25 is a good balance for corridor-scale levels.")]
    [Min(0.05f)] public float voxelSize = 0.25f;

    [Tooltip("Optional scene-specific clearance. Zero preserves the project's agent settings.")]
    [Min(0f)] public float agentRadiusOverride;
    [Min(0f)] public float agentHeightOverride;

    private NavMeshData data;
    private NavMeshDataInstance instance;

    private void Awake()
    {
        if (environmentRoot == null)
        {
            Debug.LogError("[GhostNavigation] Assign the level environment root.", this);
            return;
        }

        // glTF environments do not include physics colliders. Their actual mesh
        // surfaces preserve corridor openings for navigation and catch sight checks.
        if (addMissingMeshColliders)
        {
            foreach (MeshFilter filter in environmentRoot.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                bool hasSolidCollider = false;
                foreach (Collider collider in filter.GetComponents<Collider>())
                    if (collider.enabled && !collider.isTrigger) hasSolidCollider = true;
                if (hasSolidCollider) continue;
                MeshCollider meshCollider = filter.gameObject.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = filter.sharedMesh;
            }
            Physics.SyncTransforms();
        }

        List<NavMeshBuildSource> sources = new List<NavMeshBuildSource>();
        NavMeshBuilder.CollectSources(environmentRoot, Physics.DefaultRaycastLayers,
            NavMeshCollectGeometry.PhysicsColliders, 0, new List<NavMeshBuildMarkup>(), sources);
        // Runtime bakes cannot read non-readable imported meshes. Use their local
        // bounds conservatively (notably PoliceHQ's solid door leaves).
        for (int i = 0; i < sources.Count; i++)
        {
            NavMeshBuildSource source = sources[i];
            if (source.shape == NavMeshBuildSourceShape.Mesh && source.sourceObject is Mesh mesh && !mesh.isReadable)
            {
                source.shape = NavMeshBuildSourceShape.Box;
                source.transform *= Matrix4x4.Translate(mesh.bounds.center);
                source.size = mesh.bounds.size;
                source.sourceObject = null;
                sources[i] = source;
            }
        }

        Bounds bounds = new Bounds(environmentRoot.position, Vector3.zero);
        bool hasBounds = false;
        foreach (Collider collider in environmentRoot.GetComponentsInChildren<Collider>())
        {
            if (!collider.enabled || collider.isTrigger ||
                ((1 << collider.gameObject.layer) & Physics.DefaultRaycastLayers) == 0) continue;
            if (!hasBounds) { bounds = collider.bounds; hasBounds = true; }
            else bounds.Encapsulate(collider.bounds);
        }
        if (!hasBounds || sources.Count == 0)
        {
            Debug.LogError("[GhostNavigation] No solid environment colliders found.", this);
            return;
        }
        bounds.Expand(2f);
        NavMeshBuildSettings settings = NavMesh.GetSettingsByID(agentTypeID);
        if (agentRadiusOverride > 0f) settings.agentRadius = agentRadiusOverride;
        if (agentHeightOverride > 0f) settings.agentHeight = agentHeightOverride;
        settings.overrideVoxelSize = true;
        // Use the configurable voxelSize instead of the previous hard-coded 0.1f,
        // which was unnecessarily fine and caused slow bake times.
        settings.voxelSize = voxelSize;
        data = NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
        if (data != null) instance = NavMesh.AddNavMeshData(data);
    }

    private void OnDestroy()
    {
        if (instance.valid) instance.Remove();
        if (data != null) Destroy(data);
    }
}
