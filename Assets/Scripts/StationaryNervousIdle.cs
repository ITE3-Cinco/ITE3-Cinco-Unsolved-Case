using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

/// <summary>Plays the existing nervous clip with the same Playables approach as
/// GhostAnimation/RenatoAnimation. No agent, velocity, patrol or root motion.</summary>
public class StationaryNervousIdle : MonoBehaviour
{
    public Transform environmentRoot;
    public string characterResource = "Level2NPC/Ch07_nonPBR";
    public string animationResource = "Level2NPC/Nervously Look Around";
    public Animator Animator { get; private set; }
    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private AnimationClip clip;
    private float animationTime;

    private void Start()
    {
        GameObject model = Resources.Load<GameObject>(characterResource);
        if (model != null) Animator = Instantiate(model, transform, false).GetComponentInChildren<Animator>();
        foreach (AnimationClip candidate in Resources.LoadAll<AnimationClip>(animationResource))
            if (candidate.isHumanMotion && !candidate.name.StartsWith("__preview__"))
            {
                clip = candidate;
                break;
            }
        if (Animator == null || Animator.avatar == null || !Animator.avatar.isValid ||
            !Animator.avatar.isHuman || clip == null || clip.length <= 0f)
        {
            Debug.LogError("[StationaryNervousIdle] A Humanoid female model and nervous clip are required.", this);
            return;
        }
        Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        graph = PlayableGraph.Create("Stationary nervous idle");
        graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        playable = AnimationClipPlayable.Create(graph, clip);
        playable.SetApplyFootIK(true);
        playable.SetSpeed(0);
        AnimationPlayableOutput.Create(graph, "Nervous idle", Animator).SetSourcePlayable(playable);
        graph.Play();
        graph.Evaluate(0f);
        AlignFeetToFloor();
    }

    private void AlignFeetToFloor()
    {
        // GhostNavigation has already created the current environment's colliders
        // in Awake. Query only that hierarchy so Notes, NPC and player cannot be hit.
        if (environmentRoot == null) return;
        Ray ray = new Ray(transform.position + Vector3.up * 2f, Vector3.down);
        float nearest = float.PositiveInfinity;
        float floorY = transform.position.y;
        foreach (Collider surface in environmentRoot.GetComponentsInChildren<Collider>())
        {
            if (!surface.enabled || surface.isTrigger || !surface.Raycast(ray, out RaycastHit hit, 4f) ||
                hit.normal.y < 0.7f || hit.distance >= nearest) continue;
            nearest = hit.distance;
            floorY = hit.point.y;
        }
        if (float.IsPositiveInfinity(nearest))
        {
            Debug.LogError("[StationaryNervousIdle] No current Level 2 floor beneath the NPC.", this);
            return;
        }
        transform.position = new Vector3(transform.position.x, floorY, transform.position.z);
        // Sample the posed skinned mesh once; renderer bounds may include unused
        // animation poses and do not reliably locate the soles of the feet.
        float soleY = float.PositiveInfinity;
        Mesh mesh = new Mesh();
        foreach (SkinnedMeshRenderer skin in Animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            skin.BakeMesh(mesh);
            foreach (Vector3 vertex in mesh.vertices)
                soleY = Mathf.Min(soleY, skin.transform.TransformPoint(vertex).y);
        }
        Destroy(mesh);
        if (!float.IsPositiveInfinity(soleY)) Animator.transform.position += Vector3.up * (floorY - soleY);
    }

    public void FacePlayer(Transform player)
    {
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        if (direction.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(direction);
    }

    private void LateUpdate()
    {
        if (!graph.IsValid() || Time.timeScale <= 0f) return;
        animationTime = (animationTime + Time.deltaTime) % clip.length;
        playable.SetTime(animationTime);
        graph.Evaluate(0f);
    }

    private void OnDestroy()
    {
        if (graph.IsValid()) graph.Destroy();
    }
}
