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
                interactionText.gameObject.SetActive(!DialogueBox.IsOpen);
            }
            if (usePressed && agimat.TryUseAgimat()) return;
            return;
        }

        // Cast a ray from the center of the screen.
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        int layerMask = interactableLayer.value == 0
            ? Physics.DefaultRaycastLayers
            : interactableLayer.value;

        Interactable interactable = FindInteractable(ray, layerMask);
        if (interactable != null)
        {
            if (interactionText != null)
            {
                interactionText.text = interactable.promptMessage;
                interactionText.gameObject.SetActive(!DialogueBox.IsOpen);
            }

            if (usePressed)
            {
                interactable.Interact(transform.root.gameObject);
                if (interactionText != null)
                    interactionText.gameObject.SetActive(false);
            }
            return;
        }

        if (usePressed && agimatReady) agimat.TryUseAgimat();

        if (interactionText != null && interactionText.gameObject.activeSelf)
            interactionText.gameObject.SetActive(false);
    }

    private Interactable FindInteractable(Ray ray, int layerMask)
    {
        RaycastHit[] hits = Physics.RaycastAll(ray, interactDistance, layerMask, QueryTriggerInteraction.Collide);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            // Trigger volumes must not obscure pickups, but solid walls still do.
            Interactable item = hit.collider.GetComponentInParent<Interactable>();
            if (item != null && item.isActiveAndEnabled) return item;
            if (!hit.collider.isTrigger) return null;
        }
        return null;
    }

    private void FindMainCamera()
    {
        mainCam = Camera.main;

        if (mainCam == null)
            Debug.LogWarning("[PlayerInteraction] No camera tagged MainCamera was found.");
    }
}
