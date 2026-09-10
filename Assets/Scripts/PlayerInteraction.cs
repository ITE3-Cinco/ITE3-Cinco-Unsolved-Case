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

    void Start()
    {
        mainCam = Camera.main;
        if (interactionText != null)
        {
            interactionText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // Cast a ray from the center of the screen
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactDistance))
        {
            Interactable interactable = hit.collider.GetComponent<Interactable>();

            if (interactable != null)
            {
                // Show prompt UI
                if (interactionText != null)
                {
                    interactionText.text = interactable.promptMessage;
                    interactionText.gameObject.SetActive(true);
                }

                // Check for 'E' key press
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactable.Interact(transform.root.gameObject);

                    if (interactionText != null)
                        interactionText.gameObject.SetActive(false);
                }

                return; // Stop execution if we hit an interactable
            }
        }

        // Hide UI prompt if not pointing at an interactable object
        if (interactionText != null && interactionText.gameObject.activeSelf)
        {
            interactionText.gameObject.SetActive(false);
        }
    }
}