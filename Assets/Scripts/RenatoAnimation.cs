using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Reuses GhostAnimation's manual Playables approach with Renato's existing
/// Humanoid Animator/avatar. The stand clip is sampled backwards for crouching.</summary>
[DefaultExecutionOrder(100)]
public class RenatoAnimation : MonoBehaviour
{
    public Animator animator;
    public AnimationClip walkingClip;
    public AnimationClip runningClip;
    public AnimationClip standClip;
    public AnimationClip lookClip;
    public bool IsWalking { get; private set; }
    public float StandDuration => standClip != null ? standClip.length : 3f;

    private PlayableGraph graph;
    private AnimationMixerPlayable baseMixer;
    private AnimationLayerMixerPlayable layers;
    // Clips: 0=walk, 1=run, 2=stand/crouch, 3=look
    private readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[4];
    // Weights for the three base-mixer inputs: 0=walk, 1=run, 2=stand
    private readonly float[] weights = new float[3];
    private AvatarMask upperBody;
    private CharacterController body;
    private RenatoEnemy.BehaviourState state;
    private float stateTime;
    private float motionTime;

    private void Start()
    {
        body = GetComponent<CharacterController>();
        if (animator == null || animator.avatar == null || !animator.avatar.isHuman || !animator.avatar.isValid ||
            walkingClip == null || runningClip == null || standClip == null || lookClip == null ||
            walkingClip.length <= 0f || runningClip.length <= 0f || standClip.length <= 0f || lookClip.length <= 0f ||
            !walkingClip.isHumanMotion || !runningClip.isHumanMotion || !standClip.isHumanMotion || !lookClip.isHumanMotion)
        {
            Debug.LogError("[Renato] Assign the existing Humanoid avatar and all four Renato clips.", this);
            enabled = false;
            return;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Renato existing Mixamo clips");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        baseMixer = AnimationMixerPlayable.Create(graph, 3);
        layers = AnimationLayerMixerPlayable.Create(graph, 2);
        AnimationClip[] assets = { walkingClip, runningClip, standClip, lookClip };
        for (int i = 0; i < assets.Length; i++)
        {
            clips[i] = AnimationClipPlayable.Create(graph, assets[i]);
            clips[i].SetApplyFootIK(false);
            clips[i].SetSpeed(0);
            if (i < 3) graph.Connect(clips[i], 0, baseMixer, i);
        }
        graph.Connect(baseMixer, 0, layers, 0);
        graph.Connect(clips[3], 0, layers, 1);
        layers.SetInputWeight(0, 1f);
        // Retain the crouched hips/legs while the supplied look clip animates the upper body.
        upperBody = new AvatarMask();
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            upperBody.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i,
                i == (int)AvatarMaskBodyPart.Body || i == (int)AvatarMaskBodyPart.Head ||
                i == (int)AvatarMaskBodyPart.LeftArm || i == (int)AvatarMaskBodyPart.RightArm ||
                i == (int)AvatarMaskBodyPart.LeftFingers || i == (int)AvatarMaskBodyPart.RightFingers);
        layers.SetLayerMaskFromAvatarMask(1, upperBody);
        AnimationPlayableOutput.Create(graph, "Renato", animator).SetSourcePlayable(layers);
        // Start fully in the stand/idle pose (index 2).
        weights[2] = 1f;
        baseMixer.SetInputWeight(2, 1f);
        clips[2].SetTime(StandDuration);
        graph.Play();
        graph.Evaluate(0f);
    }

    public void SetState(RenatoEnemy.BehaviourState next) { state = next; stateTime = 0f; }

    private void LateUpdate()
    {
        IsWalking = false;
        if (!graph.IsValid() || Time.timeScale <= 0f || PauseManager.IsGamePaused) return;
        stateTime += Time.deltaTime;
        // Runs after RenatoEnemy.LateUpdate has moved the solid controller.
        Vector3 velocity = body != null && body.enabled ? body.velocity : Vector3.zero;
        velocity.y = 0f;
        bool moving = velocity.sqrMagnitude > 0.02f;
        motionTime += moving ? Time.deltaTime : 0f;

        bool crouched = state == RenatoEnemy.BehaviourState.Crouch ||
                        state == RenatoEnemy.BehaviourState.Search ||
                        state == RenatoEnemy.BehaviourState.Stand;

        // Fix: when stationary and NOT crouched, blend to the stand/idle pose (index 2)
        // rather than forcing the crouch animation. Only use walk (0) or run (1) when
        // actually moving.
        int desired;
        if (crouched)
            desired = 2;
        else if (!moving)
            desired = 2;   // stand/idle pose while stationary in patrol or investigate
        else if (state == RenatoEnemy.BehaviourState.Chase)
            desired = 1;   // run clip
        else
            desired = 0;   // walk clip

        IsWalking = desired == 0;

        for (int i = 0; i < 3; i++)
        {
            weights[i] = Mathf.MoveTowards(weights[i], i == desired ? 1f : 0f, Time.deltaTime * 6f);
            baseMixer.SetInputWeight(i, weights[i]);
        }
        clips[0].SetTime(motionTime % walkingClip.length);
        clips[1].SetTime(motionTime % runningClip.length);

        // Stand clip: sample backwards for crouch-down, hold at 0 for search, play forward for stand-up.
        float standTime = StandDuration;
        if (state == RenatoEnemy.BehaviourState.Crouch)
            standTime = Mathf.Max(0f, StandDuration - stateTime);
        else if (state == RenatoEnemy.BehaviourState.Search)
            standTime = 0f;
        else if (state == RenatoEnemy.BehaviourState.Stand)
            standTime = Mathf.Min(stateTime, StandDuration);
        // For all other states (patrol, chase, investigate) hold the clip at its end (standing pose).
        clips[2].SetTime(standTime);

        clips[3].SetTime(stateTime % lookClip.length);
        float lookWeight = Mathf.MoveTowards(layers.GetInputWeight(1),
            state == RenatoEnemy.BehaviourState.Search ? 1f : 0f, Time.deltaTime * 6f);
        layers.SetInputWeight(1, lookWeight);
        graph.Evaluate(0f);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (upperBody != null) Destroy(upperBody);
    }
}
