using UnityEngine;

public class FlashlightPickup : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement player = other.GetComponent<PlayerMovement>();
            if (player != null)
            {
                player.HasFlashlight = true; // Unlock the flashlight for the player
                Debug.Log("Picked up Flashlight!");
                Destroy(gameObject); // Remove the pickup object from the scene
            }
        }
    }
}