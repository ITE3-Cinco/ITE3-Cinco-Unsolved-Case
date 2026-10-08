using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    public LayerMask interactableLayer; // Optional layer filtering

    [Header("UI Reference")]
    public TMP_Text interactionText;

    private Camera mainCam;

    private void Start()
    {
        FindMainCamera();

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (Keyboard.current == null || PauseManager.IsGamePaused)
            return;

        if (mainCam == null)
        {
            FindMainCamera();

            if (mainCam == null)
                return;
        }

        // Cast a ray from the center of the screen.
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int layerMask = interactableLayer.value == 0
            ? Physics.DefaultRaycastLayers
            : interactableLayer.value;

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, layerMask, QueryTriggerInteraction.Collide))
        {
            // Allow the collider to be on a child object of the actual
            // interactable, which is common for detailed map/item models.
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            if (interactable != null)
            {
                if (interactionText != null)
                {
                    interactionText.text = interactable.promptMessage;
                    interactionText.gameObject.SetActive(true);
                }

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactable.Interact(transform.root.gameObject);

                    if (interactionText != null)
                        interactionText.gameObject.SetActive(false);
                }

                return;
            }
        }

        if (interactionText != null && interactionText.gameObject.activeSelf)
            interactionText.gameObject.SetActive(false);
    }

    private void FindMainCamera()
    {
        mainCam = Camera.main;

        if (mainCam == null)
            Debug.LogWarning("[PlayerInteraction] No camera tagged MainCamera was found.");
    }
}