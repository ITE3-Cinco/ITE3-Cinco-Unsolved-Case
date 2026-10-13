// Purpose: Moves the player, manages stamina and controls the held flashlight.
// Use: Attach to the player with CharacterController, Inventory and assigned stamina and flashlight UI.
// How it works: WASD or arrows move, Shift sprints, Space jumps and F toggles the selected flashlight.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity UI supplies canvas widgets such as images, sliders and buttons.
using UnityEngine.UI;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    // CharacterController moves the player while checking walls, floors and slopes.
    // controller: CharacterController cached for collision-aware player motion. Start retrieves it from the first
    // CharacterController component on this GameObject.
    private CharacterController controller;

    // walkSpeed: walking distance per gameplay second. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Movement Speeds")]
    public float walkSpeed = 6f;
    // runSpeed: normal sprint distance per gameplay second. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float runSpeed = 12f;
    // gravity: vertical acceleration applied to stored player velocity. Assigned in the scene/prefab Inspector;
    // the initializer below is the default.
    public float gravity = -9.81f;
    // jumpHeight: target jump height used to calculate takeoff speed. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float jumpHeight = 1.5f;

    // stepOffset: Maximum height the player can step over. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Header("Character Controller")]
    [Tooltip("Maximum height the player can step over.")]
    public float stepOffset = 0.5f;
    // slopeLimit: Maximum slope the player can walk up. Assigned in the scene/prefab Inspector; the initializer
    // below is the default.
    [Tooltip("Maximum slope the player can walk up.")]
    [Range(0f, 89f)] public float slopeLimit = 60f;
    // skinWidth: Small collision skin that helps prevent the controller from snagging on seams. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    [Tooltip("Small collision skin that helps prevent the controller from snagging on seams.")]
    public float skinWidth = 0.08f;

    // maxStamina: full stamina capacity. Assigned in the scene/prefab Inspector; the initializer below is the
    // default.
    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    // staminaDrainRate: stamina spent per sprinting second. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float staminaDrainRate = 15f;
    // staminaRegenRate: stamina regained per resting/walking second. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float staminaRegenRate = 14.28f;
    // useLowStaminaSprintBoost: whether GetMovementSpeed uses the alternate speed below the threshold. Assigned in
    // the scene/prefab Inspector; the initializer below is the default.
    public bool useLowStaminaSprintBoost = false;
    // sprintBoostThreshold: stamina boundary for the optional alternate sprint speed. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public float sprintBoostThreshold = 45f;
    // lowStaminaRunSpeed: sprint speed used below sprintBoostThreshold when the option is enabled. Assigned in the
    // scene/prefab Inspector; the initializer below is the default.
    public float lowStaminaRunSpeed = 5.5f;

    // currentStamina: remaining stamina, filled from maxStamina at startup. Start sets it from maxStamina.
    [Header("Stamina State")]
    public float currentStamina;
    // isExhausted: whether sprint must wait for stamina recovery. Updated by Update.
    private bool isExhausted = false;

    // staminaSlider: HUD slider showing currentStamina against maxStamina. Assigned in the scene/prefab Inspector.
    [Header("UI Reference")]
    public Slider staminaSlider;

    // flashlight: held Light switched by F and inventory selection. Assigned in the scene/prefab Inspector.
    [Header("Flashlight Settings")]
    public Light flashlight;
    // flashlightItemData: ItemData asset compared with the selected slot to identify the flashlight. Assigned in
    // the scene/prefab Inspector.
    public ItemData flashlightItemData;
    // hasFlashlight: whether the player owns the flashlight. Updated by PickUpFlashlight.
    public bool hasFlashlight = false;
    // isFlashlightOn: last flashlight toggle state, retained when switching slots. Updated by Start, Update.
    private bool isFlashlightOn = false;

    // HasFlashlight exposes hasFlashlight: get reads that field and set copies the caller value into it.
    public bool HasFlashlight
    {
        get { return hasFlashlight; }
        set { hasFlashlight = value; }
    }

    // velocity: stored movement velocity, including vertical gravity/jump speed. Updated by ResetVelocity.
    private Vector3 velocity;
    // inventory: Inventory holding player items and selection. Start retrieves it from the first Inventory
    // component on this GameObject.
    private Inventory inventory;

    /// <summary>Configure collision movement, fill stamina and set the starting flashlight state.</summary>
    void Start()
    {
        // Retrieve CharacterController from this player GameObject; store it in controller for collision-aware
        // movement.
        controller = GetComponent<CharacterController>();
        // Retrieve Inventory from this GameObject; inventory now points to the component storing its items and
        // selected slot.
        inventory = GetComponent<Inventory>();

        // If inventory is missing, run the following block.
        if (inventory == null)
            // Retrieve Inventory from this GameObject or its parents; keep the found component in inventory.
            inventory = GetComponentInParent<Inventory>();

        // Keep the controller from getting stuck on small floor/wall seams.
        // Copy stepOffset from this component into controller.stepOffset.
        controller.stepOffset = stepOffset;
        // Copy slopeLimit from this component into controller.slopeLimit.
        controller.slopeLimit = slopeLimit;
        // Copy skinWidth from this component into controller.skinWidth.
        controller.skinWidth = skinWidth;
        // Set controller.minMoveDistance to 0.
        controller.minMoveDistance = 0f;

        // Initialize currentStamina from maxStamina, the full capacity configured on PlayerMovement in the
        // Inspector.
        currentStamina = maxStamina;

        // If staminaSlider exists, run the following block.
        if (staminaSlider != null)
        {
            // Copy maxStamina into the assigned HUD Slider maximum so a full bar means full player capacity.
            staminaSlider.maxValue = maxStamina;
            // Copy the latest currentStamina into the assigned HUD Slider value to display remaining stamina.
            staminaSlider.value = currentStamina;
        }

        // Set isFlashlightOn to false.
        isFlashlightOn = false;

        // If flashlight exists, run the following block.
        if (flashlight != null)
            // Disable flashlight by changing its component enabled flag.
            flashlight.enabled = false;

        // If inventory exists, run the following block.
        if (inventory != null)
            // Call PlayerMovement.UpdateHeldItem with the selected ItemData from inventory.GetSelectedItem (null
            // for an empty slot); show the flashlight only when owned and selected, keeping its saved on or off
            // state.
            UpdateHeldItem(inventory.GetSelectedItem());
    }

    /// <summary>Each playable frame, read keys, update sprint stamina, apply movement and gravity, and handle F.</summary>
    void Update()
    {
        // If SceneTransition.IsLoading, return now; skip the rest of this method.
        if (SceneTransition.IsLoading) return;
        // If the current Input System keyboard is missing or PauseManager.IsGamePaused or not (enabled state of
        // controller), return now; skip the rest of this method.
        if (Keyboard.current == null || PauseManager.IsGamePaused || !controller.enabled) return;

        // Read the previous collision result and keep a small downward push while on the floor.
        // Store controller floor-contact result from its last Move in local wasGrounded.
        bool wasGrounded = controller.isGrounded;
        // If wasGrounded and velocity Y component is below 0, run the following block.
        if (wasGrounded && velocity.y < 0)
            // Update the vertical Y component velocity.y from negative 2.
            velocity.y = -2f;

        // Build horizontal input from WASD or arrows; normalize it so diagonals are not faster.
        // Initialize x to 0.
        float x = 0f;
        // Initialize z to 0.
        float z = 0f;

        // If d key on the current Input System keyboard is held down or rightArrow key on the current Input System
        // keyboard is held down, add 1 to x and store the updated value there.
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
        // If a key on the current Input System keyboard is held down or leftArrow key on the current Input System
        // keyboard is held down, subtract 1 from x and store the updated value there.
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
        // If w key on the current Input System keyboard is held down or upArrow key on the current Input System
        // keyboard is held down, add 1 to z and store the updated value there.
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
        // If s key on the current Input System keyboard is held down or downArrow key on the current Input System
        // keyboard is held down, subtract 1 from z and store the updated value there.
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;

        // Build a horizontal direction from the accumulated X/Z key input and limit its length to one so
        // diagonal movement has the same speed.
        Vector3 moveInput = new Vector3(x, 0f, z).normalized;

        // Store length of moveInput is above 0 in local isMoving.
        bool isMoving = moveInput.magnitude > 0;
        // Store leftShift key on the current Input System keyboard is held down in local shiftPressed.
        bool shiftPressed = Keyboard.current.leftShiftKey.isPressed;

        // After exhaustion, require 20% stamina before allowing another sprint.
        // If isExhausted and currentStamina is at least maxStamina multiplied by 0.2f, run the following block.
        if (isExhausted && currentStamina >= maxStamina * 0.2f)
            // Set isExhausted to false.
            isExhausted = false;

        // Store shiftPressed and isMoving and currentStamina is above 0 and not (isExhausted) in local canSprint.
        bool canSprint = shiftPressed && isMoving && currentStamina > 0 && !isExhausted;

        // Sprint drains stamina each second; walking or standing lets it recover.
        // If canSprint, run the following block.
        if (canSprint)
        {
            // Subtract staminaDrainRate times Unity gameplay frame duration; this makes sprint drain depend on
            // seconds, not frame count.
            currentStamina -= staminaDrainRate * Time.deltaTime;

            // If currentStamina is at most 0, run the following block.
            if (currentStamina <= 0)
            {
                // Set currentStamina to 0.
                currentStamina = 0;
                // Set isExhausted to true.
                isExhausted = true;
            }
        }
        else if (currentStamina < maxStamina)
        {
            // Add staminaRegenRate times Unity gameplay frame duration to recover stamina at the configured rate
            // per second.
            currentStamina += staminaRegenRate * Time.deltaTime;
            // Limit recovered stamina between zero and the Inspector maxStamina capacity so the HUD cannot exceed
            // its maximum.
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        }

        // If staminaSlider exists, run the following block.
        if (staminaSlider != null)
            // Copy the latest currentStamina into the assigned HUD Slider value to display remaining stamina.
            staminaSlider.value = currentStamina;

        // Store the speed chosen by PlayerMovement.GetMovementSpeed using canSprint in local currentSpeed.
        float currentSpeed = GetMovementSpeed(canSprint);

        // Combine the player right/forward world-space axes with the X/Z input values; move now follows
        // the direction the player body is facing.
        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.z;
        // isGrounded describes the previous Move. Check it before moving and
        // combine horizontal motion and gravity so floor contact is not lost
        // between two separate Move calls.
        // If space key on the current Input System keyboard was newly pressed this frame and wasGrounded, run the
        // following block.
        if (Keyboard.current.spaceKey.wasPressedThisFrame && wasGrounded)
            // Calculate the upward takeoff speed from the configured jumpHeight and negative gravity;
            // store it in the vertical velocity component.
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

        // F toggles the light only after pickup and while its inventory slot is selected.
        // If Keyboard.current.fKey was newly pressed this frame and flashlight exists and hasFlashlight, run the
        // following block.
        if (Keyboard.current.fKey.wasPressedThisFrame && flashlight != null && hasFlashlight)
        {
            // Store inventory is missing or the selected ItemData from inventory.GetSelectedItem (null for an
            // empty slot) equals flashlightItemData in local flashlightSlotSelected.
            bool flashlightSlotSelected =
                inventory == null || inventory.GetSelectedItem() == flashlightItemData;

            // If flashlightSlotSelected, run the following block.
            if (flashlightSlotSelected)
            {
                // Invert the cached on/off flag from the previous F press; later slot changes preserve this
                // choice.
                isFlashlightOn = !isFlashlightOn;
                // Copy the cached toggle flag into the assigned Light component enabled state.
                flashlight.enabled = isFlashlightOn;
            }
        }

        // Apply gravity and move once; collision flags stop upward motion at ceilings and falling at floors.
        // Add gravity multiplied by Unity gameplay frame duration in seconds to velocity.y and store the updated
        // value there.
        velocity.y += gravity * Time.deltaTime;
        // Combine horizontal direction times speed with vertical velocity, then convert to this frame
        // displacement using deltaTime. CharacterController moves with collision checks and returns the
        // contacts in flags.
        CollisionFlags flags = controller.Move((move * currentSpeed + velocity) * Time.deltaTime);

        // If (flags & CollisionFlags.Above) differs from 0 and velocity Y component is above 0, run the following
        // block.
        if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            // Update the vertical Y component velocity.y from 0.
            velocity.y = 0f;
        else if ((flags & CollisionFlags.Below) != 0 && velocity.y < 0f)
            // Update the vertical Y component velocity.y from negative 2.
            velocity.y = -2f;
    }

    /// <summary>Choose walk speed, normal run speed or the optional low-stamina run setting.</summary>
    // sprinting is a bool argument supplied by PlayerMovement.Update.
    public float GetMovementSpeed(bool sprinting)
    {
        // If not (sprinting), return walkSpeed to the method caller.
        if (!sprinting) return walkSpeed;
        // Return lowStaminaRunSpeed when useLowStaminaSprintBoost and currentStamina is below
        // sprintBoostThreshold; otherwise runSpeed to the method caller.
        return useLowStaminaSprintBoost && currentStamina < sprintBoostThreshold
            ? lowStaminaRunSpeed : runSpeed;
    }

    /// <summary>Clear stored falling or jumping motion after the player is teleported.</summary>
    public void ResetVelocity()
    {
        // Clear every component of the stored velocity so respawn does not carry old falling or jumping motion.
        velocity = Vector3.zero;
    }

    /// <summary>Show the flashlight only when owned and selected, keeping its saved on or off state.</summary>
    // currentItem is the selected ItemData supplied by Inventory.SelectSlot (or startup selection); null means an
    // empty slot.
    public void UpdateHeldItem(ItemData currentItem)
    {
        // If flashlight is missing, return now; skip the rest of this method.
        if (flashlight == null) return;

        // If not (hasFlashlight) or currentItem is missing or currentItem differs from flashlightItemData, run the
        // following block.
        if (!hasFlashlight || currentItem == null || currentItem != flashlightItemData)
        {
            // Disable flashlight by changing its component enabled flag.
            flashlight.enabled = false;
            // Return now; skip the rest of this method.
            return;
        }

        // Copy the cached toggle flag into the assigned Light component enabled state.
        flashlight.enabled = isFlashlightOn;
    }

    /// <summary>Grant flashlight ownership and try adding its item data to the inventory.</summary>
    public void PickUpFlashlight()
    {
        // Set hasFlashlight to true.
        hasFlashlight = true;

        // If inventory exists and flashlightItemData exists, run the following block.
        if (inventory != null && flashlightItemData != null)
        {
            // Call Inventory.AddItem with flashlightItemData; reject a full inventory; otherwise store the item,
            // refresh the HUD and update the held item.
            inventory.AddItem(flashlightItemData);
        }
        else
        {
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("Flashlight picked up, but Inventory or flashlightItemData is missing!");
        }
    }
}
