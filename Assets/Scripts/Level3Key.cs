// Purpose: Lets the player collect the Level 3 exit key through normal E interaction.
// Use: Attach to the key model with a BoxCollider and assign Level3Flow.
// How it works: Fit a pickup trigger to the model; remove the key only after the flow accepts collection.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

/// <summary>Collects the scene's existing key through the normal E-key interaction.</summary>
[RequireComponent(typeof(BoxCollider))]
public class Level3Key : Interactable
{
    // flow: Level3Flow coordinating key, catch and escape outcomes. Assigned in the scene/prefab Inspector.
    public Level3Flow flow;

    /// <summary>Cache pickup identity, then fit a non-blocking interaction box around the child model bounds.</summary>
    protected override void Awake()
    {
        // Run the inherited Interactable.Awake to cache this key pickup CollectionId before fitting its collider.
        base.Awake();
        // The imported model's root has no mesh. Fit a trigger to its child renderers
        // so the existing interaction ray can hit it without blocking navigation.
        // Store a new Bounds instance in local localBounds.
        Bounds localBounds = new Bounds();
        // Initialize initialized to false.
        bool initialized = false;
        // Process each renderer in all Renderer components on or beneath this GameObject.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            // Store renderer world bounding box in local bounds.
            Bounds bounds = renderer.bounds;
            // Repeat this block while i is below 8; start from int i = 0.
            for (int i = 0; i < 8; i++)
            {
                // Store bounds.center + Vector3.Scale(bounds.extents, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2)
                // == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1)) (calculated from the values shown) in local corner.
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                // Store corner converted from world coordinates into transform local coordinates in local point.
                Vector3 point = transform.InverseTransformPoint(corner);
                // If not (initialized), run the following block.
                if (!initialized) { localBounds = new Bounds(point, Vector3.zero); initialized = true; }
                else localBounds.Encapsulate(point);
            }
        }
        // Store the first BoxCollider component on this GameObject in local pickup.
        BoxCollider pickup = GetComponent<BoxCollider>();
        // Set pickup.isTrigger to true.
        pickup.isTrigger = true;
        // Set pickup.center to localBounds center.
        pickup.center = localBounds.center;
        // Store the largest of localBounds.size, a vector with all three components equal to 1 multiplied by 0.15f
        // in pickup.size.
        pickup.size = Vector3.Max(localBounds.size, Vector3.one * 0.15f);
    }

    /// <summary>Find the interacting player and remove the world key only after a successful inventory pickup.</summary>
    // playerObject comes from PlayerInteraction; the found PlayerMovement must match Level3Flow.player before
    // collection is accepted.
    public override void Interact(GameObject playerObject)
    {
        // If playerObject is missing or flow is missing, return now; skip the rest of this method.
        if (playerObject == null || flow == null) return;
        // Store the first PlayerMovement component on or beneath playerObject in local collector.
        PlayerMovement collector = playerObject.GetComponentInChildren<PlayerMovement>();
        // If collector is missing, store the first PlayerMovement component on or above playerObject in collector.
        if (collector == null) collector = playerObject.GetComponentInParent<PlayerMovement>();
        // If not (the result of Level3Flow.CollectKey using collector), return now; skip the rest of this method.
        if (!flow.CollectKey(collector)) return;
        // Deactivate gameObject.
        gameObject.SetActive(false);
        // Schedule the GameObject that owns this component for destruction at Unity end-of-frame cleanup.
        Destroy(gameObject);
    }
}
