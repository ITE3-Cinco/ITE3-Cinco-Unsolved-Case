// Purpose: Builds the White Lady visual and blends its idle, walk and scream animations.
// Use: Attach to the ghost or a visual apparition; clips can load from Resources if unassigned.
// How it works: A PlayableGraph connects animation clips to a mixer; weights select the visible pose.

// System.Collections.Generic supplies typed lists, sets and dictionaries for stored data.
using System.Collections.Generic;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// Unity animation APIs supply clip mixers and outputs that send poses to Animators.
using UnityEngine.Animations;
// Unity Playables supplies graphs that connect and evaluate animation clips.
using UnityEngine.Playables;

/// <summary>Humanoid playback for the locally rigged White Lady and Mixamo clips.</summary>
[DefaultExecutionOrder(100)]
public class GhostAnimation : MonoBehaviour
{
    // idleClip: AnimationClip sampled while the ghost is stationary. Awake retrieves it from the result of
    // GhostAnimation.LoadClip using "GhostIdle". Inspector assignment may supply its initial value.
    public AnimationClip idleClip;
    // walkingClip: AnimationClip sampled during walking. Awake retrieves it from the result of
    // GhostAnimation.LoadClip using "GhostWalk". Inspector assignment may supply its initial value.
    public AnimationClip walkingClip;
    // screamClip: Humanoid scream animation. Awake retrieves it from the result of GhostAnimation.LoadClip using
    // "GhostScream". Inspector assignment may supply its initial value.
    public AnimationClip screamClip;
    // visualFootOffset: vertical offset applied to the generated ghost visual. Assigned in the scene/prefab
    // Inspector; the initializer below is the default.
    public float visualFootOffset = 0f;
    // blendDuration: seconds used to blend animation input weights. Assigned in the scene/prefab Inspector; the
    // initializer below is the default.
    public float blendDuration = 0.15f;
    // Animator can be read by other scripts; only this class can write its backing state.
    public Animator Animator { get; private set; }

    // The graph connects clips to the Animator; weights blend inputs 0=idle, 1=walk and 2=scream.
    // graph: PlayableGraph connecting clips, mixers and Animator output. Awake retrieves it from the result of
    // PlayableGraph.Create using "White Lady animations".
    private PlayableGraph graph;
    // mixer: animation mixer combining the clip inputs. Awake retrieves it from the result of
    // AnimationMixerPlayable.Create using graph, 3.
    private AnimationMixerPlayable mixer;
    // playables: clip nodes connected to the animation mixer. Initialized from a new array/object initialized with
    // the entries specified here.
    private readonly AnimationClipPlayable[] playables = new AnimationClipPlayable[3];
    // weights: current blend weights for the animation inputs. Initialized from a new array/object initialized
    // with the entries specified here.
    private readonly float[] weights = new float[3];
    // avatar: Humanoid bone mapping built for the ghost rig. Awake retrieves it from the result of
    // GhostAnimation.BuildAvatar using visual.
    private Avatar avatar;
    // moving: requested locomotion state. Runtime data filled by the methods below; starts at the type default.
    private bool moving;
    // screamTime: current scream sample time; -1 means no scream has started. LateUpdate sets it from delta.
    private float screamTime = -1f;
    // locomotionTime: elapsed time used to loop locomotion clips. LateUpdate sets it from delta.
    private float locomotionTime;
    // clips: animation clips loaded or assigned for this component animation graph. Initialized from a new
    // array/object initialized with the entries specified here.
    private readonly AnimationClip[] clips = new AnimationClip[3];

    /// <summary>Load clips, create the rig and avatar, then connect three animation inputs to the Animator.</summary>
    private void Awake()
    {
        // If idleClip is missing or not (idleClip is a Humanoid animation clip), store the result of
        // GhostAnimation.LoadClip using "GhostIdle" in idleClip.
        if (idleClip == null || !idleClip.isHumanMotion) idleClip = LoadClip("GhostIdle");
        // If walkingClip is missing or not (walkingClip is a Humanoid animation clip), store the result of
        // GhostAnimation.LoadClip using "GhostWalk" in walkingClip.
        if (walkingClip == null || !walkingClip.isHumanMotion) walkingClip = LoadClip("GhostWalk");
        // If screamClip is missing or not (screamClip is a Humanoid animation clip), store the result of
        // GhostAnimation.LoadClip using "GhostScream" in screamClip.
        if (screamClip == null || !screamClip.isHumanMotion) screamClip = LoadClip("GhostScream");

        // Always build the Avatar from the resource's rest pose. A live ghost's
        // skeleton may already be walking or screaming when a scare is created.
        // Store the GameObject asset from a Resources folder at "WhiteLadyRig" in local model.
        GameObject model = Resources.Load<GameObject>("WhiteLadyRig");
        // If model is missing, run the following block.
        if (model == null)
        {
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[GhostAnimation] WhiteLadyRig is missing from Resources.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Store a runtime copy of model under the Transform on this component GameObject in local visual.
        GameObject visual = Instantiate(model, transform, false);
        // Set visual.name to "White Lady Animated Visual".
        visual.name = "White Lady Animated Visual";
        // Set visual.transform.localPosition to the world-up direction (positive Y) multiplied by
        // visualFootOffset.
        visual.transform.localPosition = Vector3.up * visualFootOffset;
        // Set visual.transform.localRotation to the identity rotation (no rotation).
        visual.transform.localRotation = Quaternion.identity;
        // Set visual.transform.localScale to a vector with all three components equal to 1.
        visual.transform.localScale = Vector3.one;
        // Store the first Animator component on visual in Animator.
        Animator = visual.GetComponent<Animator>();
        // If Animator is missing, store a Animator component attached to visual in Animator.
        if (Animator == null) Animator = visual.AddComponent<Animator>();
        // Store the result of GhostAnimation.BuildAvatar using visual in avatar.
        avatar = BuildAvatar(visual);
        // If avatar is missing or not (avatar.isValid) or not (avatar.isHuman), run the following block.
        if (avatar == null || !avatar.isValid || !avatar.isHuman)
        {
            // Deactivate visual.
            visual.SetActive(false);
            // Report the failed setup or state as a Unity Console error.
            Debug.LogError("[GhostAnimation] White Lady humanoid Avatar could not be created.", this);
            // Return now; skip the rest of this method.
            return;
        }
        // Copy avatar cached by this component into Animator.avatar.
        Animator.avatar = avatar;
        // Set Animator.applyRootMotion to false.
        Animator.applyRootMotion = false;
        // Set Animator.cullingMode to AnimatorCullingMode.AlwaysAnimate.
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        // Keep the original mesh as a fallback until the new rig validates.
        // Process each renderer in all Renderer components on or beneath this GameObject (including inactive
        // children).
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            // If not (renderer.transform belongs to visual.transform Transform hierarchy), disable renderer by
            // changing its component enabled flag.
            if (!renderer.transform.IsChildOf(visual.transform)) renderer.enabled = false;

        // Create one clip input per animation and send their mixed pose to the rig Animator.
        // Store the result of PlayableGraph.Create using "White Lady animations" in graph.
        graph = PlayableGraph.Create("White Lady animations");
        // Call graph.SetTimeUpdateMode using DirectorUpdateMode.Manual.
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        // Store the result of AnimationMixerPlayable.Create using graph, 3 in mixer.
        mixer = AnimationMixerPlayable.Create(graph, 3);
        // Copy idleClip from this component into clips[0].
        clips[0] = idleClip;
        // Copy walkingClip from this component into clips[1].
        clips[1] = walkingClip;
        // Copy screamClip from this component into clips[2].
        clips[2] = screamClip;
        // Repeat this block while i is below length of clips; start from int i = 0.
        for (int i = 0; i < clips.Length; i++)
        {
            // If clips[i] (entry read from clips) is missing or not (clips[i] is a Humanoid animation clip), skip
            // this entry and continue with the next loop iteration.
            if (clips[i] == null || !clips[i].isHumanMotion) continue;
            // Store the result of AnimationClipPlayable.Create using graph, clips[i] (entry read from clips) in
            // playables[i].
            playables[i] = AnimationClipPlayable.Create(graph, clips[i]);
            // Call playables[i].SetApplyFootIK using false.
            playables[i].SetApplyFootIK(false);
            // Call graph.Connect using playables[i] (entry read from playables), 0, mixer, i.
            graph.Connect(playables[i], 0, mixer, i);
        }
        // Store the result of AnimationPlayableOutput.Create using graph, "Humanoid", Animator in local output.
        AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Humanoid", Animator);
        // Call output.SetSourcePlayable using mixer.
        output.SetSourcePlayable(mixer);
        // Set weights[0] to 1.
        weights[0] = 1f;
        // Set mixer input 0 blend weight from 1.
        mixer.SetInputWeight(0, 1f);
        // Start/resume playback on graph.
        graph.Play();
        // If not (playables[1] is a valid initialized Unity handle/object), run the following block.
        if (!playables[1].IsValid())
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[GhostAnimation] Assign a Humanoid GhostWalk clip for patrols.", this);
        // If not (playables[2] is a valid initialized Unity handle/object), run the following block.
        if (!playables[2].IsValid())
            // Report the missing/invalid setup or state as a Unity Console warning.
            Debug.LogWarning("[GhostAnimation] GhostScream is not assigned; scare audio remains available.", this);
    }

    /// <summary>Find a real Humanoid clip in Resources and ignore Unity preview clips.</summary>
    // name is a string argument supplied by GhostAnimation.Awake.
    private static AnimationClip LoadClip(string name)
    {
        // Process each clip in all AnimationClip assets from a Resources folder at "GhostAnimations/" plus name.
        foreach (AnimationClip clip in Resources.LoadAll<AnimationClip>("GhostAnimations/" + name))
            // If clip is a Humanoid animation clip and not (clip.name begins with "__preview__"), return clip to
            // the method caller.
            if (clip.isHumanMotion && !clip.name.StartsWith("__preview__")) return clip;
        // Return no reference (null) to the method caller.
        return null;
    }

    /// <summary>Match named model bones to Unity human bones using the untouched rest pose.</summary>
    // visual is a GameObject argument supplied by GhostAnimation.Awake.
    private static Avatar BuildAvatar(GameObject visual)
    {
        // Store a new list with no entries in local human.
        List<HumanBone> human = new List<HumanBone>();
        // Store a new list with no entries in local skeleton.
        List<SkeletonBone> skeleton = new List<SkeletonBone>();
        // Process each bone in all Transform components on or beneath visual (including inactive children).
        foreach (Transform bone in visual.GetComponentsInChildren<Transform>(true))
        {
            // Add a new SkeletonBone instance to skeleton.
            skeleton.Add(new SkeletonBone { name = bone.name, position = bone.localPosition,
                rotation = bone.localRotation, scale = bone.localScale });
            // If the result of System.Enum.TryParse using bone.name, HumanBodyBones humanBone and humanBone viewed
            // as int is below HumanBodyBones.LastBone viewed as int, run the following block.
            if (System.Enum.TryParse(bone.name, out HumanBodyBones humanBone) && (int)humanBone < (int)HumanBodyBones.LastBone)
                // Add a new HumanBone instance to human.
                human.Add(new HumanBone { boneName = bone.name, humanName = HumanTrait.BoneName[(int)humanBone],
                    limit = new HumanLimit { useDefaultValues = true } });
        }
        // Store a new HumanDescription instance in local description.
        HumanDescription description = new HumanDescription
        {
            human = human.ToArray(), skeleton = skeleton.ToArray(),
            upperArmTwist = 0.5f, lowerArmTwist = 0.5f,
            upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
            armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f
        };
        // Return the result of AvatarBuilder.BuildHumanAvatar using visual, description to the method caller.
        return AvatarBuilder.BuildHumanAvatar(visual, description);
    }

    /// <summary>After movement, advance clip times and smoothly blend toward idle, walk or scream.</summary>
    private void LateUpdate()
    {
        // If not (graph is a valid initialized Unity handle/object), return now; skip the rest of this method.
        if (!graph.IsValid()) return;
        // Store Unity real frame duration in seconds (ignores pause) when Animator.updateMode equals
        // AnimatorUpdateMode.UnscaledTime; otherwise Unity gameplay frame duration in seconds in local delta.
        float delta = Animator.updateMode == AnimatorUpdateMode.UnscaledTime
            ? Time.unscaledDeltaTime : Time.deltaTime;
        // If delta is at most 0, return now; skip the rest of this method.
        if (delta <= 0f) return;
        // Add delta to locomotionTime and store the updated value there.
        locomotionTime += delta;
        // Choose walking or idle unless a valid scream is still playing.
        // Store 1 when moving and whether playables[1] is a valid initialized Unity handle/object; otherwise 0 in
        // local target.
        int target = moving && playables[1].IsValid() ? 1 : 0;
        // If screamTime is at least 0f and screamTime is below screamClip.length and playables[2] is a valid
        // initialized Unity handle/object, run the following block.
        if (screamTime >= 0f && screamTime < screamClip.length && playables[2].IsValid())
        {
            // Add delta to screamTime and store the updated value there.
            screamTime += delta;
            // Set target to 2.
            target = 2;
            // Store the smallest of screamTime, screamClip clip length in seconds in screamTime.
            screamTime = Mathf.Min(screamTime, screamClip.length);
        }
        // Blend input weights gradually, loop movement clips and stop scream sampling at its last frame.
        // Repeat this block while i is below length of playables; start from int i = 0.
        for (int i = 0; i < playables.Length; i++)
        {
            // Store weights[i] (entry read from weights) moved toward 1f when i == target; otherwise 0f by at most
            // delta divided by Mathf.Max(0.01f, blendDuration) this step in weights[i].
            weights[i] = Mathf.MoveTowards(weights[i], i == target ? 1f : 0f,
                delta / Mathf.Max(0.01f, blendDuration));
            // Set mixer input i blend weight from weights[i] (entry read from weights).
            mixer.SetInputWeight(i, weights[i]);
            // If not (playables[i] is a valid initialized Unity handle/object), skip this entry and continue with
            // the next loop iteration.
            if (!playables[i].IsValid()) continue;
            // Store the largest of 0f, screamTime when i equals 2; otherwise locomotionTime in local time.
            float time = i == 2 ? Mathf.Max(0f, screamTime) : locomotionTime;
            // Sample playables[i] at animation time the smallest of time, clips[i].length when i equals 2;
            // otherwise time modulo (remainder after dividing by) Mathf.Max(.001f, clips[i].length).
            playables[i].SetTime(i == 2 ? Mathf.Min(time, clips[i].length) : time % Mathf.Max(.001f, clips[i].length));
        }
        // Evaluate the PlayableGraph at clip times already assigned; zero adds no extra automatic animation time.
        graph.Evaluate(0f);
    }

    /// <summary>Use this custom driver when present, otherwise set a matching Animator bool parameter.</summary>
    // animator is a Animator argument supplied by ArtifactCornerScare.SetMoving, GhostEnemy.ApplyMovingAnimation.
    // parameter is a string argument supplied by ArtifactCornerScare.SetMoving, GhostEnemy.ApplyMovingAnimation.
    // value is a bool argument supplied by ArtifactCornerScare.SetMoving, GhostEnemy.ApplyMovingAnimation.
    public static void SetMoving(Animator animator, string parameter, bool value)
    {
        // If animator is missing, return now; skip the rest of this method.
        if (animator == null) return;
        // Store the first GhostAnimation component on or above animator in local driver.
        GhostAnimation driver = animator.GetComponentInParent<GhostAnimation>();
        // If driver exists, run the following block.
        if (driver != null) { driver.moving = value; return; }
        // If animator.runtimeAnimatorController is missing, return now; skip the rest of this method.
        if (animator.runtimeAnimatorController == null) return;
        // Process each p in animator.parameters.
        foreach (AnimatorControllerParameter p in animator.parameters)
            // If p.type equals AnimatorControllerParameterType.Bool and p.name equals parameter, run the following
            // block.
            if (p.type == AnimatorControllerParameterType.Bool && p.name == parameter)
                // Call animator.SetBool using p.nameHash, value.
                animator.SetBool(p.nameHash, value);
    }

    /// <summary>Restart the custom scream and return its duration, or trigger a regular Animator scream.</summary>
    // animator is a Animator argument supplied by ArtifactCornerScare.PlayScare, GhostEnemy.CatchPlayer.
    // parameter is a string argument supplied by ArtifactCornerScare.PlayScare, GhostEnemy.CatchPlayer.
    public static float PlayScream(Animator animator, string parameter)
    {
        // If animator is missing, return 0 to the method caller.
        if (animator == null) return 0f;
        // Store the first GhostAnimation component on or above animator in local driver.
        GhostAnimation driver = animator.GetComponentInParent<GhostAnimation>();
        // If driver exists, run the following block.
        if (driver != null)
        {
            // If driver.screamClip is missing or not (driver.playables[2] is a valid initialized Unity
            // handle/object), return 0 to the method caller.
            if (driver.screamClip == null || !driver.playables[2].IsValid()) return 0f;
            // Set driver.screamTime to 0.
            driver.screamTime = 0f;
            // Return driver.screamClip clip length in seconds to the method caller.
            return driver.screamClip.length;
        }
        // If animator.runtimeAnimatorController is missing, return 0 to the method caller.
        if (animator.runtimeAnimatorController == null) return 0f;
        // Process each p in animator.parameters.
        foreach (AnimatorControllerParameter p in animator.parameters)
            // If p.type equals AnimatorControllerParameterType.Trigger and p.name equals parameter, run the
            // following block.
            if (p.type == AnimatorControllerParameterType.Trigger && p.name == parameter)
                // Call animator.SetTrigger using p.nameHash.
                animator.SetTrigger(p.nameHash);
        // Return 0 to the method caller.
        return 0f;
    }

    /// <summary>Free the animation graph and generated avatar when the visual is removed.</summary>
    private void OnDestroy()
    {
        // If graph is a valid initialized Unity handle/object, schedule  for destruction at Unity end-of-frame
        // cleanup.
        if (graph.IsValid()) graph.Destroy();
        // If avatar exists, schedule avatar for destruction at Unity end-of-frame cleanup.
        if (avatar != null) Destroy(avatar);
    }
}
