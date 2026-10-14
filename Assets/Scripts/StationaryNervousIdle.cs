// Level 2 NPC presentation: loop the woman's nervous idle and turn her toward the speaker.
// Attach to the interaction root; assign its child character, Humanoid clip and environment.
// The animation changes her pose. It does not make her walk around.

using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class StationaryNervousIdle : MonoBehaviour
{
    // Use only the map colliders to find the floor below the NPC.
    public Transform environmentRoot;
    // The visible character must be a child of the root that owns the interaction collider.
    [Tooltip("The placed char_npc visual, parented beneath this interaction root.")]
    public Transform character;
    // Humanoid idle clip assigned in the Inspector.
    public AnimationClip nervousClip;
    // The model Animator receives the pose produced by the animation graph.
    public Animator Animator { get; private set; }
    // The graph connects the idle clip to the model; we control its time ourselves.
    private PlayableGraph graph;
    // The clip node inside that graph.
    private AnimationClipPlayable playable;
    // Current position within the loop, measured in gameplay seconds.
    private float animationTime;

    // Validate the model and clip, start the animation, then place the feet on the map floor.
    private void Start()
    {
        // Reject a missing or unrelated model so the animation cannot move the wrong object.
        if (character == null || character == transform || !character.IsChildOf(transform))
        {
            Debug.LogError("[StationaryNervousIdle] Assign the char_npc child visual.", this);
            return;
        }
        Animator = character.GetComponentInChildren<Animator>();
        // Both the model avatar and clip must support Humanoid animation.
        if (Animator == null || Animator.avatar == null || !Animator.avatar.isValid ||
            !Animator.avatar.isHuman || nervousClip == null || !nervousClip.isHumanMotion || nervousClip.length <= 0f)
        {
            Debug.LogError("[StationaryNervousIdle] A Humanoid female model and nervous clip are required.", this);
            return;
        }
        // Ignore movement stored in the clip so the woman stays at her interaction location.
        Animator.applyRootMotion = false;
        // Keep her pose updating even when she is outside the camera view.
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Connect the clip to the Animator. LateUpdate supplies time, so automatic playback is disabled.
        graph = PlayableGraph.Create("Stationary nervous idle");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, nervousClip);
        // Enable Unity foot adjustment for the Humanoid pose.
        playable.SetApplyFootIK(true);
        playable.SetSpeed(0);
        AnimationPlayableOutput.Create(graph, "Nervous idle", Animator).SetSourcePlayable(playable);
        graph.Play();
        // Apply the sampled pose without adding any automatic animation time.
        graph.Evaluate(0f);
        // Use the first pose to align the model once; avoid changing position on every animation frame.
        AlignFeetToFloor();
    }

    // Find the nearest floor below the root, then offset the visible model so its soles touch it.
    // GhostNavigation creates the map colliders in Awake, before this Start method runs.
    private void AlignFeetToFloor()
    {
        if (environmentRoot == null) return;
        // Look down from two units above the NPC; test up to four units along that ray.
        Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
        // No floor has been found yet. Keep the distance and height of the nearest valid hit.
        float nearest = float.PositiveInfinity;
        float floorY = transform.position.y;
        // Check map surfaces only, so the player, Notes and NPC cannot be mistaken for the floor.
        foreach (Collider surface in environmentRoot.GetComponentsInChildren<Collider>())
        {
            // Ignore triggers, disabled surfaces, steep faces and hits farther than the best floor hit.
            if (!surface.enabled || surface.isTrigger || !surface.Raycast(ray, out RaycastHit hit, 4f) ||
                hit.normal.y < 0.7f || hit.distance >= nearest) continue;
            nearest = hit.distance;
            floorY = hit.point.y;
        }
        // Without a floor hit, report the setup error and leave the placement unchanged.
        if (float.IsPositiveInfinity(nearest))
        {
            Debug.LogError("[StationaryNervousIdle] No current Level 2 floor beneath the NPC.", this);
            return;
        }
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
        // Measure the lowest vertex of the current posed mesh; animation bounds can be misleading.
        float soleY = float.PositiveInfinity;
        Mesh mesh = new Mesh();
        foreach (SkinnedMeshRenderer skin in Animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            // Copy the current animated shape into a temporary mesh for measurement.
            skin.BakeMesh(mesh);
            foreach (Vector3 vertex in mesh.vertices)
                // Convert each vertex to world space and keep the lowest height.
                soleY = Mathf.Min(soleY, skin.transform.TransformPoint(vertex).y);
        }
        // Release the temporary measurement mesh after use.
        Destroy(mesh);
        // Raise or lower the visual model by the gap between its soles and the floor.
        if (!float.IsPositiveInfinity(soleY)) Animator.transform.position += Vector3.up * (floorY - soleY);
    }

    // Rotate toward the speaker around the vertical axis; keep the NPC position fixed.
    public void FacePlayer(Transform player)
    {
        Vector3 direction = player.position - transform.position;
        // Ignore height differences so the woman does not tilt up or down.
        direction.y = 0f;
        // Turn only when there is a clear horizontal direction to face.
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
    }

    // Advance the idle using gameplay time; pausing also pauses the animation.
    private void LateUpdate()
    {
        if (!graph.IsValid() || Time.timeScale <= 0f) return;
        // Wrap back to the beginning when the clip reaches its end.
        animationTime = (animationTime + Time.deltaTime) % nervousClip.length;
        playable.SetTime(animationTime);
        graph.Evaluate(0f);
    }

    // Release the animation graph when the NPC or scene is removed.
    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
