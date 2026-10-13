// Purpose: Animates the frightened NPC in place and turns her toward the player.
// Use: Attach to the interaction root and assign its child character, nervous clip and environment.
// How it works: A manually sampled animation graph keeps the clip looping without root-motion movement.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity animation APIs supply clip mixers and outputs that send poses to Animators.
using UnityEngine.Animations;
// Unity Playables supplies graphs that connect and evaluate animation clips.
using UnityEngine.Playables;

/// <summary>Plays the existing nervous clip with the same Playables approach as
/// GhostAnimation/RenatoAnimation. No agent, velocity, patrol or root motion.</summary>
public class StationaryNervousIdle : MonoBehaviour
{
    // environmentRoot: scene environment used to find floor support below the NPC. Assigned in the scene/prefab
    // Inspector.
    public Transform environmentRoot;
    // character: The placed char_npc visual, parented beneath this interaction root. Assigned in the scene/prefab
    // Inspector.
    [Tooltip("The placed char_npc visual, parented beneath this interaction root.")]
    public Transform character;
    // nervousClip: Humanoid AnimationClip looped without moving the NPC root. Assigned in the scene/prefab
    // Inspector.
    public AnimationClip nervousClip;
    // Animator can be read by other scripts; only this class can write its backing state.
    public Animator Animator { get; private set; }
    // graph: PlayableGraph connecting clips, mixers and Animator output. Start retrieves it from the result of
    // PlayableGraph.Create using "Stationary nervous idle".
    private PlayableGraph graph;
    // playable: animation clip node sampled by the stationary NPC graph. Start retrieves it from the result of
    // AnimationClipPlayable.Create using graph, nervousClip.
    private AnimationClipPlayable playable;
    // animationTime: elapsed clip sampling time for the stationary nervous loop. Updated by LateUpdate.
    private float animationTime;

    /// <summary>Validate the child Humanoid model, build the nervous animation graph and align the feet.</summary>
    private void Start()
    {
        // If character is missing or character equals the Transform on this component GameObject or not (character
        // belongs to the Transform on this component GameObject Transform hierarchy), run the following block.
        if (character == null || character == transform || !character.IsChildOf(transform))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[StationaryNervousIdle] Assign the char_npc child visual.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Store the first Animator component on or beneath character in Animator.
        Animator = character.GetComponentInChildren<Animator>();
        // If Animator == null || Animator.avatar == null || !Animator.avatar.isValid || !Animator.avatar.isHuman
        // or nervousClip == null or not (nervousClip.isHumanMotion) or nervousClip clip length in seconds is at
        // most 0, run the following block.
        if (Animator == null || Animator.avatar == null || !Animator.avatar.isValid ||
            !Animator.avatar.isHuman || nervousClip == null || !nervousClip.isHumanMotion || nervousClip.length <= 0f)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[StationaryNervousIdle] A Humanoid female model and nervous clip are required.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Set Animator.applyRootMotion to false.
        Animator.applyRootMotion = false;
        // Set Animator.cullingMode to AnimatorCullingMode.AlwaysAnimate.
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Store the result of PlayableGraph.Create using "Stationary nervous idle" in graph.
        graph = PlayableGraph.Create("Stationary nervous idle");
        // Call graph.SetTimeUpdateMode using DirectorUpdateMode.Manual.
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        // Store the result of AnimationClipPlayable.Create using graph, nervousClip in playable.
        playable = AnimationClipPlayable.Create(graph, nervousClip);
        // Call playable.SetApplyFootIK using true.
        playable.SetApplyFootIK(true);
        // Stop automatic clip advancement because LateUpdate supplies the animation time manually.
        // Set playable playback rate to 0 (0 means time is supplied manually).
        playable.SetSpeed(0);
        // Call AnimationPlayableOutput.Create(graph, "Nervous idle", Animator).SetSourcePlayable using playable.
        AnimationPlayableOutput.Create(graph, "Nervous idle", Animator).SetSourcePlayable(playable);
        // Start/resume playback on graph.
        graph.Play();
        // Evaluate the PlayableGraph at clip times already assigned; zero adds no extra automatic animation time.
        graph.Evaluate(0f);
        // Call StationaryNervousIdle.AlignFeetToFloor; find the environment floor below the NPC and offset the
        // posed model soles to that height.
        AlignFeetToFloor();
    }

    /// <summary>Find the environment floor below the NPC and offset the posed model soles to that height.</summary>
    private void AlignFeetToFloor()
    {
        // GhostNavigation has already created the current environment's colliders
        // in Awake. Query only that hierarchy so Notes, NPC and player cannot be hit.
        // If environmentRoot is missing, return now; skip the rest of this method.
        if (environmentRoot == null) return;
        // Store a new Ray initialized with world position of transform plus the world-up direction (positive Y)
        // multiplied by 2f, the world-down direction (negative Y) in local ray.
        Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
        // Store float.PositiveInfinity in local nearest.
        float nearest = float.PositiveInfinity;
        // Store transform.position Y component in local floorY.
        float floorY = transform.position.y;
        // Process each surface in all Collider components on or beneath environmentRoot.
        foreach (Collider surface in environmentRoot.GetComponentsInChildren<Collider>())
        {
            // Skip this loop entry when !surface.enabled || surface.isTrigger or !surface.Raycast(ray, out
            // RaycastHit hit, 4f) or hit.normal.y is below 0.7f or hit.distance is at least nearest.
            if (!surface.enabled || surface.isTrigger || !surface.Raycast(ray, out RaycastHit hit, 4f) ||
                hit.normal.y < 0.7f || hit.distance >= nearest) continue;
            // Set nearest to hit.distance.
            nearest = hit.distance;
            // Set floorY to hit.point Y component.
            floorY = hit.point.y;
        }
        // If the result of float.IsPositiveInfinity using nearest, run the following block.
        if (float.IsPositiveInfinity(nearest))
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[StationaryNervousIdle] No current Level 2 floor beneath the NPC.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Set transform.position to a Vector3 built from transform.position X component, floorY,
        // transform.position Z component.
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
        // Sample the posed skinned mesh once; renderer bounds may include unused
        // animation poses and do not reliably locate the soles of the feet.
        // Store float.PositiveInfinity in local soleY.
        float soleY = float.PositiveInfinity;
        // Store a new Mesh instance in local mesh.
        Mesh mesh = new Mesh();
        // Process each skin in all SkinnedMeshRenderer components on or beneath Animator.
        foreach (SkinnedMeshRenderer skin in Animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            // Write skin current skinned pose into mesh mesh for measurement.
            skin.BakeMesh(mesh);
            // Process each vertex in mesh vertices array from its mesh data.
            foreach (Vector3 vertex in mesh.vertices)
                // Store the smallest of soleY, skin.transform.TransformPoint(vertex) Y component in soleY.
                soleY = Mathf.Min(soleY, skin.transform.TransformPoint(vertex).y);
        }
        // Schedule mesh for destruction at Unity end-of-frame cleanup.
        Destroy(mesh);
        // If not (the result of float.IsPositiveInfinity using soleY), add the world-up direction (positive Y)
        // multiplied by (floorY minus soleY) to Animator.transform.position and store the updated value there.
        if (!float.IsPositiveInfinity(soleY)) Animator.transform.position += Vector3.up * (floorY - soleY);
    }

    /// <summary>Turn the NPC toward the player on the horizontal plane when a conversation begins.</summary>
    // player is a Transform argument supplied by Level2NoteNpc.Interact.
    public void FacePlayer(Transform player)
    {
        // Store world position of player minus world position of transform in local direction.
        Vector3 direction = player.position - transform.position;
        // Update the vertical Y component direction.y from 0.
        direction.y = 0f;
        // If squared length of direction (avoids a square root) is above 0.01, store a rotation facing direction
        // direction in transform.rotation.
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>Advance the looping nervous clip while gameplay time is running.</summary>
    private void LateUpdate()
    {
        // If not (graph is a valid initialized Unity handle/object) or Unity gameplay clock multiplier (0 freezes
        // gameplay time) is at most 0, return now; skip the rest of this method.
        if (!graph.IsValid() || Time.timeScale <= 0f) return;
        // Recalculate animationTime from (animationTime + Time.deltaTime) % nervousClip.length (calculated from
        // the values shown).
        animationTime = (animationTime + Time.deltaTime) % nervousClip.length;
        // Sample playable at animation time animationTime.
        playable.SetTime(animationTime);
        // Evaluate the PlayableGraph at clip times already assigned; zero adds no extra automatic animation time.
        graph.Evaluate(0f);
    }

    /// <summary>Destroy the animation graph when the NPC is removed.</summary>
    private void OnDestroy()
    {
        // If graph is a valid initialized Unity handle/object, schedule  for destruction at Unity end-of-frame
        // cleanup.
        if (graph.IsValid()) graph.Destroy();
    }
}
