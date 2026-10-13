// Purpose: Passes player entry at the main door to Level3Flow.
// Use: Attach to the main exit trigger with a BoxCollider and assign the scene flow.
// How it works: Only the configured player can request escape; Level3Flow checks whether the key was collected.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Level3Exit : MonoBehaviour
{
    // flow: Level3Flow coordinating key, catch and escape outcomes. Assigned in the scene/prefab Inspector.
    public Level3Flow flow;

    /// <summary>When something enters the trigger, request escape only if it belongs to the configured player.</summary>
    // Unity Physics supplies other as the Collider entering this trigger; its parent components identify the
    // player.
    private void OnTriggerEnter(Collider other)
    {
        // If flow exists and flow.player exists and the first PlayerMovement component on or above other equals
        // flow.player, run the following block.
        if (flow != null && flow.player != null && other.GetComponentInParent<PlayerMovement>() == flow.player)
            // Call Level3Flow.Escape; start the ending only during active gameplay after collecting the key.
            flow.Escape();
    }
}
