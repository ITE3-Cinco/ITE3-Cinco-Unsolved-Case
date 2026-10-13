// Purpose: Blends Renato walking, running, crouching and room-search animations.
// Use: Attach beside RenatoEnemy and assign the existing Humanoid Animator and four clips.
// How it works: Manual clip sampling uses enemy state and actual body motion; an upper-body layer adds searching.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity animation APIs supply clip mixers and outputs that send poses to Animators.
using UnityEngine.Animations;
// Unity Playables supplies graphs that connect and evaluate animation clips.
using UnityEngine.Playables;

/// <summary>Reuses GhostAnimation's manual Playables approach with Renato's existing
/// Humanoid Animator/avatar. The stand clip is sampled backwards for crouching.</summary>
[DefaultExecutionOrder(100)]
public class RenatoAnimation : MonoBehaviour
{
    // animator: Animator receiving poses or parameter changes. Assigned in the scene/prefab Inspector.
    public Animator animator;
    // walkingClip: AnimationClip sampled during walking. Assigned in the scene/prefab Inspector.
    public AnimationClip walkingClip;
    // runningClip: AnimationClip sampled during pursuit. Assigned in the scene/prefab Inspector.
    public AnimationClip runningClip;
    // standClip: AnimationClip sampled backward to crouch and forward to stand. Assigned in the scene/prefab
    // Inspector.
    public AnimationClip standClip;
    // lookClip: AnimationClip layered onto the upper body while searching. Assigned in the scene/prefab Inspector.
    public AnimationClip lookClip;
    // IsWalking can be read by other scripts; only this class can write its backing state.
    public bool IsWalking { get; private set; }
    // StandDuration is computed when read: return standClip clip length in seconds when standClip exists;
    // otherwise 3.
    public float StandDuration => standClip != null ? standClip.length : 3f;

    // graph: PlayableGraph connecting clips, mixers and Animator output. Start retrieves it from the result of
    // PlayableGraph.Create using "Renato existing Mixamo clips".
    private PlayableGraph graph;
    // baseMixer: mixer combining Renato walk, run and stand clips. Start retrieves it from the result of
    // AnimationMixerPlayable.Create using graph, 3.
    private AnimationMixerPlayable baseMixer;
    // layers: mixer combining base locomotion with the masked search animation. Start retrieves it from the result
    // of AnimationLayerMixerPlayable.Create using graph, 2.
    private AnimationLayerMixerPlayable layers;
    // Clips: 0=walk, 1=run, 2=stand/crouch, 3=look
    // clips: animation clips loaded or assigned for this component animation graph. Initialized from a new
    // array/object initialized with the entries specified here.
    private readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[4];
    // Weights for the three base-mixer inputs: 0=walk, 1=run, 2=stand
    // weights: current blend weights for the animation inputs. Initialized from a new array/object initialized
    // with the entries specified here.
    private readonly float[] weights = new float[3];
    // upperBody: AvatarMask restricting the search animation to the upper body. Updated by Start.
    private AvatarMask upperBody;
    // body: CharacterController used for solid movement. Start retrieves it from the first CharacterController
    // component on this GameObject.
    private CharacterController body;
    // state: current behavior requested by the enemy. SetState sets it from next.
    private RenatoEnemy.BehaviourState state;
    // stateTime: time value used by this component behavior logic. LateUpdate sets it from Unity gameplay frame
    // duration in seconds.
    private float stateTime;
    // motionTime: elapsed moving time used to sample walk/run clips. Updated by LateUpdate.
    private float motionTime;

    /// <summary>Validate the avatar and clips, build animation mixers and set the initial standing pose.</summary>
    private void Start()
    {
        // Store the first CharacterController component on this GameObject in body.
        body = GetComponent<CharacterController>();
        // If animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid
        // || walkingClip == null || runningClip == null || standClip == null || lookClip == null ||
        // walkingClip.length <= 0f || runningClip.length <= 0f || standClip.length <= 0f || lookClip.length <= 0f
        // || !walkingClip.isHumanMotion or !runningClip.isHumanMotion or not (standClip.isHumanMotion) or not
        // (lookClip is a Humanoid animation clip), run the following block.
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid ||
            walkingClip == null || runningClip == null || standClip == null || lookClip == null ||
            walkingClip.length <= 0f || runningClip.length <= 0f || standClip.length <= 0f || lookClip.length <= 0f ||
            !walkingClip.isHumanMotion || !runningClip.isHumanMotion || !standClip.isHumanMotion || !lookClip.isHumanMotion)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[Renato] Assign the existing Humanoid avatar and all four Renato clips.", this);
            // Set enabled to false.
            enabled = false;
            // Return now; skip the rest of this method.
            return;
        }
        // Set animator.applyRootMotion to false.
        animator.applyRootMotion = false;
        // Set animator.cullingMode to AnimatorCullingMode.AlwaysAnimate.
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Store the result of PlayableGraph.Create using "Renato existing Mixamo clips" in graph.
        graph = PlayableGraph.Create("Renato existing Mixamo clips");
        // Call graph.SetTimeUpdateMode using DirectorUpdateMode.Manual.
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        // Store the result of AnimationMixerPlayable.Create using graph, 3 in baseMixer.
        baseMixer = AnimationMixerPlayable.Create(graph, 3);
        // Blend walk, run and stand in the base layer; use a second layer for upper-body searching.
        // Store the result of AnimationLayerMixerPlayable.Create using graph, 2 in layers.
        layers = AnimationLayerMixerPlayable.Create(graph, 2);
        // Store a new array/object initialized with the entries specified here in local assets.
        AnimationClip[] assets = { walkingClip, runningClip, standClip, lookClip };
        // Repeat this block while i is below length of assets; start from int i = 0.
        for (int i = 0; i < assets.Length; i++)
        {
            // Store the result of AnimationClipPlayable.Create using graph, assets[i] (entry read from assets) in
            // clips[i].
            clips[i] = AnimationClipPlayable.Create(graph, assets[i]);
            // Call clips[i].SetApplyFootIK using false.
            clips[i].SetApplyFootIK(false);
            // Set clips[i] playback rate to 0 (0 means time is supplied manually).
            clips[i].SetSpeed(0);
            // If i is below 3, call graph.Connect using clips[i] (entry read from clips), 0, baseMixer, i.
            if (i < 3) graph.Connect(clips[i], 0, baseMixer, i);
        }
        // Call graph.Connect using baseMixer, 0, layers, 0.
        graph.Connect(baseMixer, 0, layers, 0);
        // Call graph.Connect using clips[3] (entry read from clips), 0, layers, 1.
        graph.Connect(clips[3], 0, layers, 1);
        // Set layers input 0 blend weight from 1.
        layers.SetInputWeight(0, 1f);
        // Retain the crouched hips/legs while the supplied look clip animates the upper body.
        // Set upperBody to a new AvatarMask instance.
        upperBody = new AvatarMask();
        // Repeat this block while i is below AvatarMaskBodyPart.LastBodyPart viewed as int; start from int i = 0.
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            // Call upperBody.SetHumanoidBodyPartActive using i viewed as AvatarMaskBodyPart, i ==
            // (int)AvatarMaskBodyPart.Body || i == (int)AvatarMaskBodyPart.Head || i ==
            // (int)AvatarMaskBodyPart.LeftArm || i == (int)AvatarMaskBodyPart.RightArm or i ==
            // (int)AvatarMaskBodyPart.LeftFingers or i equals (int)AvatarMaskBodyPart.RightFingers.
            upperBody.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,
                i == (int)AvatarMaskBodyPart.Body || i == (int)AvatarMaskBodyPart.Head ||
                i == (int)AvatarMaskBodyPart.LeftArm || i == (int)AvatarMaskBodyPart.RightArm ||
                i == (int)AvatarMaskBodyPart.LeftFingers || i == (int)AvatarMaskBodyPart.RightFingers);
        // Call layers.SetLayerMaskFromAvatarMask using 1, upperBody.
        layers.SetLayerMaskFromAvatarMask(1, upperBody);
        // Call AnimationPlayableOutput.Create(graph, "Renato", animator).SetSourcePlayable using layers.
        AnimationPlayableOutput.Create(graph, "Renato", animator).SetSourcePlayable(layers);
        // Start fully in the stand/idle pose (index 2).
        // Set weights[2] to 1.
        weights[2] = 1f;
        // Set baseMixer input 2 blend weight from 1.
        baseMixer.SetInputWeight(2, 1f);
        // Sample clips[2] at animation time StandDuration.
        clips[2].SetTime(StandDuration);
        // Start/resume playback on graph.
        graph.Play();
        // Evaluate the PlayableGraph at clip times already assigned; zero adds no extra automatic animation time.
        graph.Evaluate(0f);
    }

    /// <summary>Receive a new enemy behavior and restart the timer for its animation sequence.</summary>
    // next is a RenatoEnemy.BehaviourState argument supplied by RenatoEnemy.SetState.
    // Copy next (argument from the caller) into state. Set stateTime to 0.
    public void SetState(RenatoEnemy.BehaviourState next) { state = next; stateTime = 0f; }

    /// <summary>After physical movement, blend clips and sample crouch, search or standing poses.</summary>
    private void LateUpdate()
    {
        // Set IsWalking to false.
        IsWalking = false;
        // If not (graph.IsValid()) or Unity gameplay clock multiplier (0 freezes gameplay time) is at most 0f or
        // PauseManager.IsGamePaused, return now; skip the rest of this method.
        if (!graph.IsValid() || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        // Add Unity gameplay frame duration in seconds to stateTime and store the updated value there.
        stateTime += Time.deltaTime;
        // Runs after RenatoEnemy.LateUpdate has moved the solid controller.
        // Store body actual movement velocity when body exists and enabled state of body; otherwise the zero
        // vector (no movement or offset) in local velocity.
        Vector3 velocity = body != null && body.enabled ? body.velocity : Vector3.zero;
        // Update the vertical Y component velocity.y from 0.
        velocity.y = 0f;
        // Store squared length of velocity (avoids a square root) is above 0.02 in local moving.
        bool moving = velocity.sqrMagnitude > 0.02f;
        // Add Unity gameplay frame duration in seconds when moving; otherwise 0 to motionTime and store the
        // updated value there.
        motionTime += moving ? Time.deltaTime : 0f;

        // Store state equals RenatoEnemy.BehaviourState.Crouch or state equals RenatoEnemy.BehaviourState.Search
        // or state equals RenatoEnemy.BehaviourState.Stand in local crouched.
        bool crouched = state == RenatoEnemy.BehaviourState.Crouch ||
                        state == RenatoEnemy.BehaviourState.Search ||
                        state == RenatoEnemy.BehaviourState.Stand;

        // Fix: when stationary and NOT crouched, blend to the stand/idle pose (index 2)
        // rather than forcing the crouch animation. Only use walk (0) or run (1) when
        // actually moving.
        // Declare local desired to hold int data supplied later in this method.
        int desired;
        // If crouched, run the following block.
        if (crouched)
            // Set desired to 2.
            desired = 2;
        else if (!moving)
            // Set desired to 2.
            desired = 2;   // stand/idle pose while stationary in patrol or investigate
        else if (state == RenatoEnemy.BehaviourState.Chase)
            // Set desired to 1.
            desired = 1;   // run clip
        else
            // Set desired to 0.
            desired = 0;   // walk clip

        // Set IsWalking to desired equals 0.
        IsWalking = desired == 0;

        // Ease each base animation toward its target weight so state changes do not snap poses.
        // Repeat this block while i is below 3; start from int i = 0.
        for (int i = 0; i < 3; i++)
        {
            // Store weights[i] (entry read from weights) moved toward 1f when i == desired; otherwise 0f by at
            // most Unity gameplay frame duration in seconds multiplied by 6f this step in weights[i].
            weights[i] = Mathf.MoveTowards(weights[i], i == desired ? 1f : 0f, Time.deltaTime * 6f);
            // Set baseMixer input i blend weight from weights[i] (entry read from weights).
            baseMixer.SetInputWeight(i, weights[i]);
        }
        // Sample clips[0] at animation time motionTime modulo (remainder after dividing by) walkingClip clip
        // length in seconds.
        clips[0].SetTime(motionTime % walkingClip.length);
        // Sample clips[1] at animation time motionTime modulo (remainder after dividing by) runningClip clip
        // length in seconds.
        clips[1].SetTime(motionTime % runningClip.length);

        // Stand clip: sample backwards for crouch-down, hold at 0 for search, play forward for stand-up.
        // Keep StandDuration in local standTime.
        float standTime = StandDuration;
        // If state equals RenatoEnemy.BehaviourState.Crouch, run the following block.
        if (state == RenatoEnemy.BehaviourState.Crouch)
            // Store the largest of 0, StandDuration minus stateTime in standTime.
            standTime = Mathf.Max(0f, StandDuration - stateTime);
        else if (state == RenatoEnemy.BehaviourState.Search)
            // Set standTime to 0.
            standTime = 0f;
        else if (state == RenatoEnemy.BehaviourState.Stand)
            // Store the smallest of stateTime, StandDuration in standTime.
            standTime = Mathf.Min(stateTime, StandDuration);
        // For all other states (patrol, chase, investigate) hold the clip at its end (standing pose).
        // Sample clips[2] at animation time standTime.
        clips[2].SetTime(standTime);

        // Sample clips[3] at animation time stateTime modulo (remainder after dividing by) lookClip clip length in
        // seconds.
        clips[3].SetTime(stateTime % lookClip.length);
        // Store the result of layers.GetInputWeight using 1 moved toward 1f when state ==
        // RenatoEnemy.BehaviourState.Search; otherwise 0f by at most Unity gameplay frame duration in seconds
        // multiplied by 6f this step in local lookWeight.
        float lookWeight = Mathf.MoveTowards(layers.GetInputWeight(1),
            state == RenatoEnemy.BehaviourState.Search ? 1f : 0f, Time.deltaTime * 6f);
        // Set layers input 1 blend weight from lookWeight.
        layers.SetInputWeight(1, lookWeight);
        // Evaluate the PlayableGraph at clip times already assigned; zero adds no extra automatic animation time.
        graph.Evaluate(0f);
    }

    /// <summary>Free the animation graph and upper-body mask when leaving the scene.</summary>
    private void OnDestroy()
    {
        // If graph is a valid initialized Unity handle/object, schedule  for destruction at Unity end-of-frame
        // cleanup.
        if (graph.IsValid()) graph.Destroy();
        // If upperBody exists, schedule upperBody for destruction at Unity end-of-frame cleanup.
        if (upperBody != null) Destroy(upperBody);
    }
}
