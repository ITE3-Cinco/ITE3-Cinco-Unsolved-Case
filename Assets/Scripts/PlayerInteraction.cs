// Purpose: Finds what the player is looking at and handles E-key interaction.
// Use: Attach under the player; assign the prompt text and optional interaction layer mask.
// How it works: A camera-center ray finds Interactable parents; selected or warning agimat protection takes priority.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public class PlayerInteraction : MonoBehaviour
{
    // interactDistance: maximum camera-ray interaction distance. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    [Header("Raycast Settings")]
    public float interactDistance = 3f;
    // interactableLayer: layers the interaction ray may hit; zero selects Unity default raycast layers. Assigned
    // in the scene/prefab Inspector.
    public LayerMask interactableLayer; // Optional layer filtering

    // interactionText: HUD text that displays the currently targeted interaction prompt. Assigned in the
    // scene/prefab Inspector.
    [Header("UI Reference")]
    public TMP_Text interactionText;

    // mainCam: MainCamera-tagged camera used to aim the interaction ray. FindMainCamera retrieves it from Unity
    // first enabled camera tagged MainCamera.
    private Camera mainCam;
    // agimat: InvisibilityConsumable checked before ordinary E-key pickup. Start retrieves it from the first
    // InvisibilityConsumable component on or above this GameObject.
    private InvisibilityConsumable agimat;
    // inventory: Inventory holding player items and selection. Start retrieves it from the first Inventory
    // component on or above this GameObject.
    private Inventory inventory;

    /// <summary>Find the camera, inventory and agimat controller, then hide the unused interaction prompt.</summary>
    private void Start()
    {
        // Call PlayerInteraction.FindMainCamera; find the MainCamera-tagged camera used to aim the interaction
        // ray.
        FindMainCamera();
        // Retrieve the protection controller from this GameObject or its parents; cache it in agimat for E-key
        // defense.
        agimat = GetComponentInParent<InvisibilityConsumable>();
        // Retrieve Inventory from this GameObject or its parents; keep the found component in inventory.
        inventory = GetComponentInParent<Inventory>();

        // If interactionText exists, run the following block.
        if (interactionText != null)
            // Deactivate interactionText.gameObject.
            interactionText.gameObject.SetActive(false);
    }

    /// <summary>During active gameplay, prioritize agimat use, raycast for a target and process one E press.</summary>
    private void Update()
    {
        // If SceneTransition.IsLoading, return now; skip the rest of this method.
        if (SceneTransition.IsLoading) return;
        // Stop this method early when the current Input System keyboard is missing or PauseManager.IsGamePaused or
        // Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0.
        if (Keyboard.current == null || PauseManager.IsGamePaused || Time.timeScale <= 0f)
            // Return now; skip the rest of this method.
            return;

        // If mainCam is missing, run the following block.
        if (mainCam == null)
        {
            // Call PlayerInteraction.FindMainCamera; find the MainCamera-tagged camera used to aim the interaction
            // ray.
            FindMainCamera();

            // Stop this method early when mainCam is missing.
            if (mainCam == null)
                // Return now; skip the rest of this method.
                return;
        }

        // Read the current Input System keyboard E-key press into usePressed; true means the key was
        // pressed this frame.
        bool usePressed = Keyboard.current.eKey.wasPressedThisFrame;
        // Read the selected ItemData from the cached Inventory; store null when the inventory is missing
        // or the selected slot is empty.
        ItemData selected = inventory != null ? inventory.GetSelectedItem() : null;
        // Set agimatReady only when the cached protection controller exists, protection is inactive and
        // Inventory contains an agimat.
        bool agimatReady = agimat != null && !agimat.isInvisible && inventory != null && inventory.HasAgimat;
        // Give agimat use priority when protection is ready and either InvisibilityConsumable reports a
        // nearby ghost or the selected ItemData is an agimat.
        if (agimatReady && (agimat.GhostNearby || (selected != null && selected.isAgimat)))
        {
            // If interactionText exists, run the following block.
            if (interactionText != null)
            {
                // Display "Press [E] to use Agimat" in interactionText.text (the assigned TMP/UI text component).
                interactionText.text = "Press [E] to use Agimat";
                // Show the interaction label only when DialogueBox reports that no conversation is open.
                interactionText.gameObject.SetActive(!DialogueBox.IsOpen);
            }
            // If usePressed and the result of InvisibilityConsumable.TryUseAgimat, return now; skip the rest of
            // this method.
            if (usePressed && agimat.TryUseAgimat()) return;
            // Return now; skip the rest of this method.
            return;
        }

        // Ask the cached player camera for a ray through the screen center; viewport X and Y of 0.5 select
        // the middle of the view.
        Ray ray = mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        // A zero Inspector mask means use default raycast layers; otherwise use the assigned filter.
        // Store Unity default layers for raycast collision queries when interactableLayer.value equals 0;
        // otherwise interactableLayer.value in local layerMask.
        int layerMask = interactableLayer.value == 0
            ? Physics.DefaultRaycastLayers
            : interactableLayer.value;

        // Cast the camera ray up to interactDistance through layerMask, including triggers. On a hit,
        // Physics writes the collider, position and distance into hit and enters this block.
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, layerMask, QueryTriggerInteraction.Collide))
        {
            // Search the hit collider GameObject and its ancestors for Interactable; this connects a child
            // model collider to its pickup or conversation script.
            Interactable interactable = hit.collider.GetComponentInParent<Interactable>();

            // If interactable exists, run the following block.
            if (interactable != null)
            {
                // If interactionText exists, run the following block.
                if (interactionText != null)
                {
                    // Copy promptMessage from the raycast-selected Interactable into the Inspector-assigned TMP
                    // interaction label.
                    interactionText.text = interactable.promptMessage;
                    // Show the interaction label only when DialogueBox reports that no conversation is
                    // open.
                    interactionText.gameObject.SetActive(!DialogueBox.IsOpen);
                }

                // If e key on the current Input System keyboard was newly pressed this frame, run the following
                // block.
                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    // Pass this component topmost player GameObject to the selected Interactable; its
                    // implementation handles pickup, checkpoint or conversation.
                    interactable.Interact(transform.root.gameObject);

                    // If interactionText exists, run the following block.
                    if (interactionText != null)
                        // Deactivate interactionText.gameObject.
                        interactionText.gameObject.SetActive(false);
                }

                // Return now; skip the rest of this method.
                return;
            }
        }

        // If no world object was targeted, E can still use a carried agimat.
        // If usePressed and agimatReady, call InvisibilityConsumable.TryUseAgimat; during active gameplay, consume
        // one agimat and start protection if it is not already active.
        if (usePressed && agimatReady) agimat.TryUseAgimat();

        // If interactionText exists and interactionText.gameObject own activation flag, run the following block.
        if (interactionText != null && interactionText.gameObject.activeSelf)
            // Deactivate interactionText.gameObject.
            interactionText.gameObject.SetActive(false);
    }

    /// <summary>Find the MainCamera-tagged camera used to aim the interaction ray.</summary>
    private void FindMainCamera()
    {
        // Retrieve Unity first enabled MainCamera-tagged camera; cache it in mainCam to aim the interaction ray.
        mainCam = Camera.main;

        // If mainCam is missing, run the following block.
        if (mainCam == null)
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[PlayerInteraction] No camera tagged MainCamera was found.");
    }
}
