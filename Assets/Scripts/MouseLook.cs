// Purpose: Rotates the first-person camera and player body from mouse movement.
// Use: Attach to the player camera and assign playerBody for left and right turning.
// How it works: Mouse Y changes a clamped camera pitch; mouse X turns the body around its vertical axis.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity Input System supplies the current keyboard/mouse and their key or pointer states.
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    // mouseSensitivity: scale applied to mouse deltas. Update sets it from targetSensitivity.
    [Header("Sensitivity Settings")]
    [Range(0.1f, 10f)]
    public float mouseSensitivity = 2f;

    // lockSensitivity: If checked, ignores external scripts trying to change sensitivity at runtime. Assigned in
    // the scene/prefab Inspector; the initializer below is the default.
    [Tooltip("If checked, ignores external scripts trying to change sensitivity at runtime.")]
    public bool lockSensitivity = true;

    // playerBody: Transform rotated horizontally by MouseLook. Assigned in the scene/prefab Inspector.
    public Transform playerBody;
    // xRotation: accumulated vertical camera angle, limited from -90 to 90 degrees. Update sets it from mouseY.
    private float xRotation = 0f;
    // targetSensitivity: startup sensitivity saved for lockSensitivity. Awake sets it from mouseSensitivity.
    private float targetSensitivity;

    /// <summary>Remember the Inspector sensitivity for use when sensitivity locking is enabled.</summary>
    private void Awake()
    {
        // Copy mouseSensitivity from this component into targetSensitivity.
        targetSensitivity = mouseSensitivity;
    }

    /// <summary>Capture and hide the cursor for first-person control.</summary>
    private void Start()
    {
        // Call MouseLook.LockCursor; keep the pointer centered and hidden so mouse movement controls the camera.
        LockCursor();
    }

    /// <summary>During playable frames, read mouse movement and apply vertical camera and horizontal body rotation.</summary>
    private void Update()
    {
        // Stop this method early when the current Input System mouse is missing or PauseManager.IsGamePaused or
        // SceneTransition.IsLoading.
        if (Mouse.current == null || PauseManager.IsGamePaused || SceneTransition.IsLoading)
            // Return now; skip the rest of this method.
            return;

        // If lockSensitivity, run the following block.
        if (lockSensitivity)
            // Copy targetSensitivity cached by this component into mouseSensitivity.
            mouseSensitivity = targetSensitivity;

        // Store the current Input System value from Mouse.current.delta in local delta.
        Vector2 delta = Mouse.current.delta.ReadValue();

        // Stop this method early when delta equals the zero 2D vector.
        if (delta == Vector2.zero)
            // Return now; skip the rest of this method.
            return;

        // Scale mouse movement by sensitivity; pitch looks up/down and body yaw turns left/right.
        // Store delta X component multiplied by (mouseSensitivity multiplied by 0.1f) in local mouseX.
        float mouseX = delta.x * (mouseSensitivity * 0.1f);
        // Store delta Y component multiplied by (mouseSensitivity multiplied by 0.1f) in local mouseY.
        float mouseY = delta.y * (mouseSensitivity * 0.1f);

        // Subtract mouseY from xRotation and store the updated value there.
        xRotation -= mouseY;
        // Limit pitch to a straight up or down view so the camera cannot flip upside down.
        // Store xRotation limited between negative 90f and 90 in xRotation.
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Store a rotation built from angles xRotation, 0, 0 in degrees in transform.localRotation.
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // If playerBody exists, run the following block.
        if (playerBody != null)
            // Rotate playerBody by Vector3.up * mouseX.
            playerBody.Rotate(Vector3.up * mouseX);
    }

    /// <summary>When focus returns, recapture the cursor only if gameplay controls are still active.</summary>
    // Unity supplies hasFocus when the application gains or loses focus.
    private void OnApplicationFocus(bool hasFocus)
    {
        // Focus callbacks can run on disabled behaviours after the player is caught.
        // Frozen outcome screens and scene transitions must keep UI pointer control.
        // If hasFocus && isActiveAndEnabled and Time.timeScale > 0f and not (PauseManager.IsGamePaused) and not
        // (SceneTransition.IsLoading), run the following block.
        if (hasFocus && isActiveAndEnabled && Time.timeScale > 0f &&
            !PauseManager.IsGamePaused && !SceneTransition.IsLoading)
            // Call MouseLook.LockCursor; keep the pointer centered and hidden so mouse movement controls the
            // camera.
            LockCursor();
    }

    /// <summary>Keep the pointer centered and hidden so mouse movement controls the camera.</summary>
    private void LockCursor()
    {
        // Capture the Unity pointer at the view center for first-person mouse look.
        Cursor.lockState = CursorLockMode.Locked;
        // Hide Unity mouse pointer during first-person control.
        Cursor.visible = false;
    }
}