using UnityEngine;

public class Interactable : MonoBehaviour
{
    public string promptMessage = "Press [E] to pick up Flashlight";

    public void Interact(GameObject playerObject)
    {
        PlayerMovement player = playerObject.GetComponentInParent<PlayerMovement>();
        if (player == null) player = playerObject.GetComponentInChildren<PlayerMovement>();

        if (player != null)
        {
            player.HasFlashlight = true;

            // Show HUD notification for 4 seconds
            if (NotificationManager.Instance != null)
            {
                NotificationManager.Instance.ShowNotification("Press [F] to toggle flashlight.", 4.0f);
            }

            Destroy(gameObject);
        }
    }
}