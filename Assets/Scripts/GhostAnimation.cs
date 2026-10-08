using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Humanoid playback for the locally rigged White Lady and Mixamo clips.</summary>
[DefaultExecutionOrder(100)]
public class GhostAnimation : MonoBehaviour
{
    public AnimationClip idleClip;
    public AnimationClip walkingClip;
    public AnimationClip screamClip;
    public float visualFootOffset = -1.8f;
    public float blendDuration = 0.15f;
    public Animator Animator { get; private set; }

    private PlayableGraph graph;
    private AnimationMixerPlayable mixer;
    private readonly AnimationClipPlayable[] playables = new AnimationClipPlayable[3];
    private readonly float[] weights = new float[3];
    private Avatar avatar;
    private bool moving;
    private float screamTime = -1f;
    private float locomotionTime;
    private readonly AnimationClip[] clips = new AnimationClip[3];

    private void Awake()
    {
        if (idleClip == null || !idleClip.isHumanMotion) idleClip = LoadClip("GhostIdle");
        if (walkingClip == null || !walkingClip.isHumanMotion) walkingClip = LoadClip("GhostWalk");
        if (screamClip == null || !screamClip.isHumanMotion) screamClip = LoadClip("GhostScream");

        Transform existing = transform.Find("White Lady Animated Visual");
        GameObject visual;
        if (existing != null)
            visual = existing.gameObject;
        else
        {
            GameObject model = Resources.Load<GameObject>("WhiteLadyRig");
            if (model == null)
            {
                Debug.LogError("[GhostAnimation] WhiteLadyRig is missing from Resources.", this);
                return;
            }
            visual = Instantiate(model, transform, false);
            visual.name = "White Lady Animated Visual";
        }
        visual.transform.localPosition = Vector3.up * visualFootOffset;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        Animator = visual.GetComponent<Animator>();
        if (Animator == null) Animator = visual.AddComponent<Animator>();
        avatar = BuildAvatar(visual);
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            visual.SetActive(false);
            Debug.LogError("[GhostAnimation] White Lady humanoid Avatar could not be created.", this);
            return;
        }
        Animator.avatar = avatar;
        Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Keep the original mesh as a fallback until the new rig validates.
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            if (!renderer.transform.IsChildOf(visual.transform)) renderer.enabled = false;

        graph = PlayableGraph.Create("White Lady animations");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        mixer = AnimationMixerPlayable.Create(graph, 3);
        clips[0] = idleClip;
        clips[1] = walkingClip;
        clips[2] = screamClip;
        for (int i = 0; i < clips.Length; i++)
        {
            if (clips[i] == null || !clips[i].isHumanMotion) continue;
            playables[i] = AnimationClipPlayable.Create(graph, clips[i]);
            playables[i].SetApplyFootIK(false);
            graph.Connect(playables[i], 0, mixer, i);
        }
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Humanoid", Animator);
        output.SetSourcePlayable(mixer);
        weights[0] = 1f;
        mixer.SetInputWeight(0, 1f);
        graph.Play();
        if (!playables[1].IsValid())
            Debug.LogWarning("[GhostAnimation] Assign a Humanoid GhostWalk clip for patrols.", this);
        if (!playables[2].IsValid())
            Debug.LogWarning("[GhostAnimation] GhostScream is not assigned; scare audio remains available.", this);
    }

    private static AnimationClip LoadClip(string name)
    {
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("GhostAnimations/" + name))
            if (clip.isHumanMotion && !clip.name.StartsWith("__preview__")) return clip;
        return null;
    }

    private static Avatar BuildAvatar(GameObject visual)
    {
        List<HumanBone> human = new List<HumanBone>();
        List<SkeletonBone> skeleton = new List<SkeletonBone>();
        foreach (Transform bone in visual.GetComponentsInChildren<Transform>(true))
        {
            skeleton.Add(new SkeletonBone { name = bone.name, position = bone.localPosition,
                rotation = bone.localRotation, scale = bone.localScale });
            if (System.Enum.TryParse(bone.name, out HumanBodyBones humanBone) && (int)humanBone < (int)HumanBodyBones.LastBone)
                human.Add(new HumanBone { boneName = bone.name, humanName = HumanTrait.BoneName[(int)humanBone],
                    limit = new HumanLimit { useDefaultValues = true } });
        }
        HumanDescription description = new HumanDescription
        {
            human = human.ToArray(), skeleton = skeleton.ToArray(),
            upperArmTwist = 0.5f, lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
            armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f
        };
        return AvatarBuilder.BuildHumanAvatar(visual, description);
    }

    private void LateUpdate()
    {
        if (!graph.IsValid()) return;
        float delta = Animator.updateMode == AnimatorUpdateMode.UnscaledTime
            ? Time.unscaledDeltaTime : Time.deltaTime;
        if (delta <= 0f) return;
        locomotionTime += delta;
        int target = moving && playables[1].IsValid() ? 1 : 0;
        if (screamTime >= 0f && screamTime < screamClip.length && playables[2].IsValid())
        {
            screamTime += delta;
            target = 2;
            screamTime = Mathf.Min(screamTime, screamClip.length);
        }
        for (int i = 0; i < playables.Length; i++)
        {
            weights[i] = Mathf.MoveTowards(weights[i], i == target ? 1f : 0f,
                delta / Mathf.Max(0.01f, blendDuration));
            mixer.SetInputWeight(i, weights[i]);
            if (!playables[i].IsValid()) continue;
            float time = i == 2 ? Mathf.Max(0f, screamTime) : locomotionTime;
            playables[i].SetTime(i == 2 ? Mathf.Min(time, clips[i].length) : time % Mathf.Max(.001f, clips[i].length));
        }
        graph.Evaluate(0f);
    }

    public static void SetMoving(Animator animator, string parameter, bool value)
    {
        if (animator == null) return;
        GhostAnimation driver = animator.GetComponentInParent<GhostAnimation>();
        if (driver != null) { driver.moving = value; return; }
        if (animator.runtimeAnimatorController == null) return;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == parameter)
                animator.SetBool(p.nameHash, value);
    }

    public static float PlayScream(Animator animator, string parameter)
    {
        if (animator == null) return 0f;
        GhostAnimation driver = animator.GetComponentInParent<GhostAnimation>();
        if (driver != null)
        {
            if (driver.screamClip == null || !driver.playables[2].IsValid()) return 0f;
            driver.screamTime = 0f;
            return driver.screamClip.length;
        }
        if (animator.runtimeAnimatorController == null) return 0f;
        foreach (AnimatorControllerParameter p in animator.parameters)
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == parameter)
                animator.SetTrigger(p.nameHash);
        return 0f;
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
        if (avatar != null) Destroy(avatar);
    }
}
