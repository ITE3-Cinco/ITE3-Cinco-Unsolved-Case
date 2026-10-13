// Purpose: Provides orbit, zoom and target-follow controls for TextMesh Pro sample scenes.
// Use: Attach to a sample camera; optionally assign CameraTarget.
// How it works: Input changes angles and distance; LateUpdate places the camera around its target.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;


namespace TMPro.Examples
{
    
    public class CameraController : MonoBehaviour
    {
        public enum CameraModes { Follow, Isometric, Free }

        // cameraTransform: camera transform. Awake sets it from the Transform on this component GameObject.
        private Transform cameraTransform;
        // dummyTarget: dummy target. Start sets it from Transform of new GameObject("Camera Target").
        private Transform dummyTarget;

        // CameraTarget: camera target. Start sets it from dummyTarget.
        public Transform CameraTarget;

        // FollowDistance: follow distance. Updated by GetPlayerInput.
        public float FollowDistance = 30.0f;
        // MaxFollowDistance: max follow distance. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public float MaxFollowDistance = 100.0f;
        // MinFollowDistance: min follow distance. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public float MinFollowDistance = 2.0f;

        // ElevationAngle: elevation angle. Updated by GetPlayerInput.
        public float ElevationAngle = 30.0f;
        // MaxElevationAngle: max elevation angle. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public float MaxElevationAngle = 85.0f;
        // MinElevationAngle: min elevation angle. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public float MinElevationAngle = 0f;

        // OrbitalAngle: orbital angle. Updated by GetPlayerInput.
        public float OrbitalAngle = 0f;

        // CameraMode: camera mode. GetPlayerInput sets it from CameraModes.Isometric.
        public CameraModes CameraMode = CameraModes.Follow;

        // MovementSmoothing: movement smoothing. GetPlayerInput sets it from previousSmoothing.
        public bool MovementSmoothing = true;
        // RotationSmoothing: rotation smoothing. Assigned in the scene/prefab Inspector; the initializer below is
        // the default.
        public bool RotationSmoothing = false;
        // previousSmoothing: previous smoothing. Awake sets it from MovementSmoothing.
        private bool previousSmoothing;

        // MovementSmoothingValue: movement smoothing value. Assigned in the scene/prefab Inspector; the
        // initializer below is the default.
        public float MovementSmoothingValue = 25f;
        // RotationSmoothingValue: rotation smoothing value. Assigned in the scene/prefab Inspector; the
        // initializer below is the default.
        public float RotationSmoothingValue = 5.0f;

        // MoveSensitivity: move sensitivity. Assigned in the scene/prefab Inspector; the initializer below is the
        // default.
        public float MoveSensitivity = 2.0f;

        // currentVelocity: current velocity. Initialized from the zero vector (no movement or offset).
        private Vector3 currentVelocity = Vector3.zero;
        // desiredPosition: desired position. Updated by LateUpdate.
        private Vector3 desiredPosition;
        // mouseX: mouse x. GetPlayerInput retrieves it from the result of Input.GetAxis using "Mouse X".
        private float mouseX;
        // mouseY: mouse y. GetPlayerInput retrieves it from the result of Input.GetAxis using "Mouse Y".
        private float mouseY;
        // moveVector: move vector. Updated by GetPlayerInput.
        private Vector3 moveVector;
        // mouseWheel: mouse wheel. GetPlayerInput retrieves it from the result of Input.GetAxis using "Mouse
        // ScrollWheel".
        private float mouseWheel;

        // Controls for Touches on Mobile devices
        //private float prev_ZoomDelta;


        // event_SmoothingValue: event smoothing value. Fixed script constant; callers cannot change it.
        private const string event_SmoothingValue = "Slider - Smoothing Value";
        // event_FollowDistance: event follow distance. Fixed script constant; callers cannot change it.
        private const string event_FollowDistance = "Slider - Camera Zoom";


        // Configure frame-rate and touch behavior, then cache the camera transform.
        void Awake()
        {
            // If QualitySettings.vSyncCount is above 0, run the following block.
            if (QualitySettings.vSyncCount > 0)
                // Set Application.targetFrameRate to 60.
                Application.targetFrameRate = 60;
            else
                // Set Application.targetFrameRate to negative 1.
                Application.targetFrameRate = -1;

            // If Application.platform equals RuntimePlatform.IPhonePlayer or Application.platform equals
            // RuntimePlatform.Android, run the following block.
            if (Application.platform == RuntimePlatform.IPhonePlayer || Application.platform == RuntimePlatform.Android)
                // Set Input.simulateMouseWithTouches to false.
                Input.simulateMouseWithTouches = false;

            // Copy transform into cameraTransform.
            cameraTransform = transform;
            // Copy MovementSmoothing from this component into previousSmoothing.
            previousSmoothing = MovementSmoothing;
        }


        // Create a central dummy target if none was assigned.
        // Use this for initialization
        void Start()
        {
            // If CameraTarget is missing, run the following block.
            if (CameraTarget == null)
            {
                // If we don't have a target (assigned by the player, create a dummy in the center of the scene).
                // Set dummyTarget to Transform of new GameObject("Camera Target").
                dummyTarget = new GameObject("Camera Target").transform;
                // Copy dummyTarget cached by this component into CameraTarget.
                CameraTarget = dummyTarget;
            }
        }

        // Read sample controls, calculate the target-relative camera pose and optionally smooth movement.
        // Update is called once per frame
        void LateUpdate()
        {
            // Call CameraController.GetPlayerInput; read orbit, zoom, target selection and panning controls for
            // the sample camera.
            GetPlayerInput();


            // Check if we still have a valid target
            // If CameraTarget exists, run the following block.
            if (CameraTarget != null)
            {
                // If CameraMode equals CameraModes.Isometric, run the following block.
                if (CameraMode == CameraModes.Isometric)
                {
                    // Recalculate desiredPosition from CameraTarget.position + Quaternion.Euler(ElevationAngle,
                    // OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance) (calculated from the values shown).
                    desiredPosition = CameraTarget.position + Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * new Vector3(0, 0, -FollowDistance);
                }
                else if (CameraMode == CameraModes.Follow)
                {
                    // Recalculate desiredPosition from CameraTarget.position +
                    // CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * (new
                    // Vector3(0, 0, -FollowDistance))) (calculated from the values shown).
                    desiredPosition = CameraTarget.position + CameraTarget.TransformDirection(Quaternion.Euler(ElevationAngle, OrbitalAngle, 0f) * (new Vector3(0, 0, -FollowDistance)));
                }
                else
                {
                    // Free Camera implementation
                }

                // If MovementSmoothing equals true, run the following block.
                if (MovementSmoothing == true)
                {
                    // Using Smoothing
                    // Store the result of Vector3.SmoothDamp using world position of cameraTransform,
                    // desiredPosition, currentVelocity, MovementSmoothingValue multiplied by Time.fixedDeltaTime
                    // in cameraTransform.position.
                    cameraTransform.position = Vector3.SmoothDamp(cameraTransform.position, desiredPosition, ref currentVelocity, MovementSmoothingValue * Time.fixedDeltaTime);
                    //cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, Time.deltaTime * 5.0f);
                }
                else
                {
                    // Not using Smoothing
                    // Copy desiredPosition cached by this component into cameraTransform.position.
                    cameraTransform.position = desiredPosition;
                }

                // If RotationSmoothing equals true, run the following block.
                if (RotationSmoothing == true)
                    // Store a blend from world rotation of cameraTransform to a rotation facing direction
                    // CameraTarget.position - cameraTransform.position using fraction RotationSmoothingValue
                    // multiplied by Unity gameplay frame duration in seconds in cameraTransform.rotation.
                    cameraTransform.rotation = Quaternion.Lerp(cameraTransform.rotation, Quaternion.LookRotation(CameraTarget.position - cameraTransform.position), RotationSmoothingValue * Time.deltaTime);
                else
                {
                    // Turn cameraTransform to face CameraTarget.
                    cameraTransform.LookAt(CameraTarget);
                }

            }

        }



        // Read orbit, zoom, target selection and panning controls for the sample camera.
        void GetPlayerInput()
        {
            // Set moveVector to the zero vector (no movement or offset).
            moveVector = Vector3.zero;

            // Check Mouse Wheel Input prior to Shift Key so we can apply multiplier on Shift for Scrolling
            // Store the result of Input.GetAxis using "Mouse ScrollWheel" in mouseWheel.
            mouseWheel = Input.GetAxis("Mouse ScrollWheel");

            // Store Input.touchCount in local touchCount.
            float touchCount = Input.touchCount;

            // If key KeyCode.LeftShift is held down or key KeyCode.RightShift is held down or touchCount is above
            // 0, run the following block.
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) || touchCount > 0)
            {
                // Multiply the current mouseWheel by 10 and store the result back in mouseWheel.
                mouseWheel *= 10;

                // If key KeyCode.I was newly pressed this frame, run the following block.
                if (Input.GetKeyDown(KeyCode.I))
                    // Set CameraMode to CameraModes.Isometric.
                    CameraMode = CameraModes.Isometric;

                // If key KeyCode.F was newly pressed this frame, run the following block.
                if (Input.GetKeyDown(KeyCode.F))
                    // Set CameraMode to CameraModes.Follow.
                    CameraMode = CameraModes.Follow;

                // If key KeyCode.S was newly pressed this frame, run the following block.
                if (Input.GetKeyDown(KeyCode.S))
                    // Set MovementSmoothing to not (MovementSmoothing).
                    MovementSmoothing = !MovementSmoothing;


                // Check for right mouse button to change camera follow and elevation angle
                // If the result of Input.GetMouseButton using 1, run the following block.
                if (Input.GetMouseButton(1))
                {
                    // Store the result of Input.GetAxis using "Mouse Y" in mouseY.
                    mouseY = Input.GetAxis("Mouse Y");
                    // Store the result of Input.GetAxis using "Mouse X" in mouseX.
                    mouseX = Input.GetAxis("Mouse X");

                    // If mouseY is above 0.01 or mouseY is below negative 0.01f, run the following block.
                    if (mouseY > 0.01f || mouseY < -0.01f)
                    {
                        // Subtract mouseY multiplied by MoveSensitivity from ElevationAngle and store the updated
                        // value there.
                        ElevationAngle -= mouseY * MoveSensitivity;
                        // Limit Elevation angle between min & max values.
                        // Store ElevationAngle limited between MinElevationAngle and MaxElevationAngle in
                        // ElevationAngle.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }

                    // If mouseX is above 0.01 or mouseX is below negative 0.01f, run the following block.
                    if (mouseX > 0.01f || mouseX < -0.01f)
                    {
                        // Add mouseX multiplied by MoveSensitivity to OrbitalAngle and store the updated value
                        // there.
                        OrbitalAngle += mouseX * MoveSensitivity;
                        // If OrbitalAngle is above 360, run the following block.
                        if (OrbitalAngle > 360)
                            // Subtract 360 from OrbitalAngle and store the updated value there.
                            OrbitalAngle -= 360;
                        // If OrbitalAngle is below 0, run the following block.
                        if (OrbitalAngle < 0)
                            // Add 360 to OrbitalAngle and store the updated value there.
                            OrbitalAngle += 360;
                    }
                }

                // Get Input from Mobile Device
                // If touchCount equals 1 and Input.GetTouch(0).phase equals TouchPhase.Moved, run the following
                // block.
                if (touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Moved)
                {
                    // Store Input.GetTouch(0).deltaPosition in local deltaPosition.
                    Vector2 deltaPosition = Input.GetTouch(0).deltaPosition;

                    // Handle elevation changes
                    // If deltaPosition Y component is above 0.01 or deltaPosition Y component is below negative
                    // 0.01f, run the following block.
                    if (deltaPosition.y > 0.01f || deltaPosition.y < -0.01f)
                    {
                        // Subtract deltaPosition Y component multiplied by 0.1 from ElevationAngle and store the
                        // updated value there.
                        ElevationAngle -= deltaPosition.y * 0.1f;
                        // Limit Elevation angle between min & max values.
                        // Store ElevationAngle limited between MinElevationAngle and MaxElevationAngle in
                        // ElevationAngle.
                        ElevationAngle = Mathf.Clamp(ElevationAngle, MinElevationAngle, MaxElevationAngle);
                    }


                    // Handle left & right 
                    // If deltaPosition X component is above 0.01 or deltaPosition X component is below negative
                    // 0.01f, run the following block.
                    if (deltaPosition.x > 0.01f || deltaPosition.x < -0.01f)
                    {
                        // Add deltaPosition X component multiplied by 0.1 to OrbitalAngle and store the updated
                        // value there.
                        OrbitalAngle += deltaPosition.x * 0.1f;
                        // If OrbitalAngle is above 360, run the following block.
                        if (OrbitalAngle > 360)
                            // Subtract 360 from OrbitalAngle and store the updated value there.
                            OrbitalAngle -= 360;
                        // If OrbitalAngle is below 0, run the following block.
                        if (OrbitalAngle < 0)
                            // Add 360 to OrbitalAngle and store the updated value there.
                            OrbitalAngle += 360;
                    }

                }

                // Check for left mouse button to select a new CameraTarget or to reset Follow position
                // If the result of Input.GetMouseButton using 0, run the following block.
                if (Input.GetMouseButton(0))
                {
                    // Store a camera ray through the screen pixel position Input.mousePosition in local ray.
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    // Declare local hit to hold RaycastHit data supplied later in this method.
                    RaycastHit hit;

                    // If the ray hits a collider within the specified range/layers; store collider, point and
                    // distance in hit, run the following block.
                    if (Physics.Raycast(ray, out hit, 300, 1 << 10 | 1 << 11 | 1 << 12 | 1 << 14))
                    {
                        // If Transform of hit equals CameraTarget, run the following block.
                        if (hit.transform == CameraTarget)
                        {
                            // Reset Follow Position
                            // Set OrbitalAngle to 0.
                            OrbitalAngle = 0;
                        }
                        else
                        {
                            // Set CameraTarget to Transform of hit.
                            CameraTarget = hit.transform;
                            // Set OrbitalAngle to 0.
                            OrbitalAngle = 0;
                            // Copy previousSmoothing cached by this component into MovementSmoothing.
                            MovementSmoothing = previousSmoothing;
                        }

                    }
                }


                // If the result of Input.GetMouseButton using 2, run the following block.
                if (Input.GetMouseButton(2))
                {
                    // If dummyTarget is missing, run the following block.
                    if (dummyTarget == null)
                    {
                        // We need a Dummy Target to anchor the Camera
                        // Set dummyTarget to Transform of new GameObject("Camera Target").
                        dummyTarget = new GameObject("Camera Target").transform;
                        // Set dummyTarget.position to world position of CameraTarget.
                        dummyTarget.position = CameraTarget.position;
                        // Set dummyTarget.rotation to world rotation of CameraTarget.
                        dummyTarget.rotation = CameraTarget.rotation;
                        // Copy dummyTarget cached by this component into CameraTarget.
                        CameraTarget = dummyTarget;
                        // Copy MovementSmoothing from this component into previousSmoothing.
                        previousSmoothing = MovementSmoothing;
                        // Set MovementSmoothing to false.
                        MovementSmoothing = false;
                    }
                    else if (dummyTarget != CameraTarget)
                    {
                        // Move DummyTarget to CameraTarget
                        // Set dummyTarget.position to world position of CameraTarget.
                        dummyTarget.position = CameraTarget.position;
                        // Set dummyTarget.rotation to world rotation of CameraTarget.
                        dummyTarget.rotation = CameraTarget.rotation;
                        // Copy dummyTarget cached by this component into CameraTarget.
                        CameraTarget = dummyTarget;
                        // Copy MovementSmoothing from this component into previousSmoothing.
                        previousSmoothing = MovementSmoothing;
                        // Set MovementSmoothing to false.
                        MovementSmoothing = false;
                    }


                    // Store the result of Input.GetAxis using "Mouse Y" in mouseY.
                    mouseY = Input.GetAxis("Mouse Y");
                    // Store the result of Input.GetAxis using "Mouse X" in mouseX.
                    mouseX = Input.GetAxis("Mouse X");

                    // Store mouseX rotated from cameraTransform local axes into world direction in moveVector.
                    moveVector = cameraTransform.TransformDirection(mouseX, mouseY, 0);

                    // Call dummyTarget.Translate using negative moveVector, Space.World.
                    dummyTarget.Translate(-moveVector, Space.World);

                }

            }

            // Check Pinching to Zoom in - out on Mobile device
            // If touchCount equals 2, run the following block.
            if (touchCount == 2)
            {
                // Store the result of Input.GetTouch using 0 in local touch0.
                Touch touch0 = Input.GetTouch(0);
                // Store the result of Input.GetTouch using 1 in local touch1.
                Touch touch1 = Input.GetTouch(1);

                // Store world position of touch0 minus touch0.deltaPosition in local touch0PrevPos.
                Vector2 touch0PrevPos = touch0.position - touch0.deltaPosition;
                // Store world position of touch1 minus touch1.deltaPosition in local touch1PrevPos.
                Vector2 touch1PrevPos = touch1.position - touch1.deltaPosition;

                // Store length of (touch0PrevPos - touch1PrevPos) in local prevTouchDelta.
                float prevTouchDelta = (touch0PrevPos - touch1PrevPos).magnitude;
                // Store length of (touch0.position - touch1.position) in local touchDelta.
                float touchDelta = (touch0.position - touch1.position).magnitude;

                // Store prevTouchDelta minus touchDelta in local zoomDelta.
                float zoomDelta = prevTouchDelta - touchDelta;

                // If zoomDelta is above 0.01 or zoomDelta is below negative 0.01f, run the following block.
                if (zoomDelta > 0.01f || zoomDelta < -0.01f)
                {
                    // Add zoomDelta multiplied by 0.25 to FollowDistance and store the updated value there.
                    FollowDistance += zoomDelta * 0.25f;
                    // Limit FollowDistance between min & max values.
                    // Store FollowDistance limited between MinFollowDistance and MaxFollowDistance in
                    // FollowDistance.
                    FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
                }


            }

            // Check MouseWheel to Zoom in-out
            // If mouseWheel is below negative 0.01f or mouseWheel is above 0.01, run the following block.
            if (mouseWheel < -0.01f || mouseWheel > 0.01f)
            {

                // Subtract mouseWheel multiplied by 5.0 from FollowDistance and store the updated value there.
                FollowDistance -= mouseWheel * 5.0f;
                // Limit FollowDistance between min & max values.
                // Store FollowDistance limited between MinFollowDistance and MaxFollowDistance in FollowDistance.
                FollowDistance = Mathf.Clamp(FollowDistance, MinFollowDistance, MaxFollowDistance);
            }


        }
    }
}