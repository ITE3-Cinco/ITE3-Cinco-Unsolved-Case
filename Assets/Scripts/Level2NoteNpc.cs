using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Uses the same raycast and E interaction as pickups, while remaining in the scene.
public class Level2NoteNpc : Interactable
{
    public Level2NoteQuest quest;
    [Header("Nervous character")]
    public Animator animator;
    public AnimationClip nervousIdle;
    [Tooltip("Existing character/clip Resources paths used when explicit references are empty.")]
    public string characterResource = "Level2NPC/Ch07_nonPBR";
    public string animationResource = "Level2NPC/Nervously Look Around";

    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private float animationTime;

    private void Start()
    {
        if (animator == null)
        {
            GameObject model = Resources.Load<GameObject>(characterResource);
            if (model != null)
                animator = Instantiate(model, transform, false).GetComponentInChildren<Animator>();
        }
        if (nervousIdle == null)
            foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>(animationResource))
                if (clip.isHumanMotion && !clip.name.StartsWith("__preview__"))
                {
                    nervousIdle = clip;
                    break;
                }
        if (animator == null || animator.avatar == null || !animator.avatar.isValid ||
            !animator.avatar.isHuman || nervousIdle == null || !nervousIdle.isHumanMotion)
        {
            Debug.LogError("[Level2NoteNpc] Assign a Humanoid NPC and nervous idle clip.", this);
            return;
        }
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Nervous NPC idle");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, nervousIdle);
        playable.SetApplyFootIK(false);
        playable.SetSpeed(0);
        AnimationPlayableOutput.Create(graph, "Nervous idle", animator).SetSourcePlayable(playable);
        graph.Play();
    }

    private void LateUpdate()
    {
        if (!graph.IsValid() || nervousIdle.length <= 0f) return;
        animationTime = (animationTime + Time.deltaTime) % nervousIdle.length;
        playable.SetTime(animationTime);
        graph.Evaluate(0f);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }

    public override void Interact(GameObject playerObject)
    {
        if (quest != null) quest.Talk(playerObject);
    }
}
