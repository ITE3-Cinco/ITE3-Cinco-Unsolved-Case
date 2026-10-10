using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Level3Exit : MonoBehaviour
{
    public Level3Flow flow;

    private void OnTriggerEnter(Collider other)
    {
        if (flow != null && flow.player != null && other.GetComponentInParent<PlayerMovement>() == flow.player)
            flow.Escape();
    }
}
