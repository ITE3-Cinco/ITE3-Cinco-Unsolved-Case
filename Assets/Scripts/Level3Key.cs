using UnityEngine;

/// <summary>Collects the scene's existing key through the normal E-key interaction.</summary>
[RequireComponent(typeof(BoxCollider))]
public class Level3Key : Interactable
{
    public Level3Flow flow;

    private void Awake()
    {
        // The imported model's root has no mesh. Fit a trigger to its child renderers
        // so the existing interaction ray can hit it without blocking navigation.
        Bounds localBounds = new Bounds();
        bool initialized = false;
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>())
        {
            Bounds bounds = renderer.bounds;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                    new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                Vector3 point = transform.InverseTransformPoint(corner);
                if (!initialized) { localBounds = new Bounds(point, Vector3.zero); initialized = true; }
                else localBounds.Encapsulate(point);
            }
        }
        BoxCollider pickup = GetComponent<BoxCollider>();
        pickup.isTrigger = true;
        pickup.center = localBounds.center;
        pickup.size = Vector3.Max(localBounds.size, Vector3.one * 0.15f);
    }

    public override void Interact(GameObject playerObject)
    {
        if (playerObject == null || flow == null) return;
        PlayerMovement collector = playerObject.GetComponentInChildren<PlayerMovement>();
        if (collector == null) collector = playerObject.GetComponentInParent<PlayerMovement>();
        if (!flow.CollectKey(collector)) return;
        gameObject.SetActive(false);
        Destroy(gameObject);
    }
}
