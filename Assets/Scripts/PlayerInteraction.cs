using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    public LayerMask interactableLayer; // Optional layer filtering

    [Header("UI Reference")]
    public TMP_Text interactionText;

    private Camera mainCam;
    private InvisibilityConsumable agimat;
    private Inventory inventory;

    private void Start()
    {
        FindMainCamera();
        agimat = GetComponentInParent<InvisibilityConsumable>();
        inventory = GetComponentInParent<Inventory>();

        if (interactionText != null)
            interactionText.gameObject.SetActive(false);
    }

    private void Update()
    {
        if (SceneTransition.IsLoading) return;
        if (Keyboard.current == null || PauseManager.IsGamePaused || Time.timeScale <= 0f)
            return;

        if (mainCam == null)
        {
            FindMainCamera();

            if (mainCam == null)
                return;
        }

        bool usePressed = Keyboard.current.eKey.wasPressedThisFrame;
        ItemData selected = inventory != null ? inventory.GetSelectedItem() : null;
        bool agimatReady = agimat != null && !agimat.isInvisible && inventory != null && inventory.HasAgimat;
        // A warning or selected agimat gives defense priority over a nearby pickup.
        if (agimatReady && (agimat.GhostNearby || (selected != null && selected.isAgimat)))
        {
            if (interactionText != null)
            {
                interactionText.text = "Press [E] to use Agimat";
                interactionText.gameObject.SetActive(true);
            }
            if (usePressed && agimat.TryUseAgimat()) return;
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

        if (usePressed && agimatReady) agimat.TryUseAgimat();

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