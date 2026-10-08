# White Lady navigation and animation

## Navigation

PoliceHQ's `=== ENEMY ===` container now has `GhostNavigation`, referencing the **Environment** hierarchy. It builds a NavMesh from enabled environment colliders in Awake, before the enemy starts. The agent is saved **disabled** in the scene and is enabled only after a valid spawn point is found on its agent-type NavMesh. This addresses the original attempt to create an agent before a NavMesh exists.

The bake uses the existing Humanoid agent settings and a 0.1-unit voxel size. Non-readable MeshColliders use their local bounds as conservative boxes, including solid door leaves; no mesh Read/Write import changes are required. The player, enemy and scare copies are outside the bake hierarchy. The generated data is removed when the scene unloads.

`GhostEnemy.requireNavigation` is enabled for PoliceHQ. The enemy stops with a descriptive error if its spawn cannot bind to the surface. It does not use the old wall-crossing patrol fallback in this scene. Agimat retreat destinations must be reachable and farther from the player. Checkpoint patrols still use the saved checkpoint, minimum player spacing and grace period.

The agent's base offset is 1.8; its visual uses a matching -1.8 foot offset. Catch distance now uses horizontal separation with a same-floor height check because the agent root and player feet have different heights.

## Locally built rig

`Assets/Resources/WhiteLadyRig.glb` contains the existing White Lady geometry and texture, fitted to a **21-joint humanoid skeleton** with four normalized weights per vertex. The original A-pose was converted to a T-pose for retargeting. It has 168,979 vertices across three skinned meshes. The original unrigged model remains in place as a fallback if Avatar creation fails.

This is a basic local rig, not an export from Mixamo. The gown follows the hips and thighs gently, and the hair follows the head. Individual fingers, facial animation and cloth simulation are not rigged. Inspect shoulder, hair and dress deformation in Unity and refine weights if needed. The generator is `Tools/build_white_lady_rig.py` (Python 3 + numpy); it preserves UVs and embeds the original texture. Fetch the original texture with Git LFS before regenerating.

At runtime, `GhostAnimation` instantiates the rig, builds a Humanoid Avatar, and drives idle/walk/scream through Unity's Animation Playables. Root motion is disabled so the NavMeshAgent owns movement. Walking is selected from actual position changes, so an enemy stopped by a wall is not intentionally told to walk. Blend duration defaults to 0.15 seconds.

The existing `GhostIdle.fbx` and `GhostWalk.fbx` were moved, preserving their GUIDs, to **Assets/Resources/GhostAnimations**. Both import as Humanoid with Create From This Model. Clips can also be assigned explicitly in the Ghost Animation Inspector component. `GhostWalk` is used for the requested default walking behavior; no separate `GhostWalking` file exists in this checkout.

## GhostScream still required

No Mixamo `GhostScream` animation is present in the repository. To connect it:

1. Import its FBX. In **Rig**, choose **Humanoid → Create From This Model**, then Apply.
2. Expand the FBX in Project and assign its animation clip to **Whitelady → Ghost Animation → Scream Clip**.
3. Alternatively, put the FBX at **Assets/Resources/GhostAnimations/GhostScream.fbx** for automatic discovery.

Corner scares and generic jump scares trigger the clip and remain visible for at least its duration. A catch triggers it too, using unscaled playback while the lose screen pauses gameplay. The existing `WhiteLady-Scream` audio continues to play when the animation slot is empty. Apparitions keep only their animation driver enabled; their navigation, collision and catch scripts remain disabled.

## Validation and Unity checks

Run structural checks:

```sh
python3 -m unittest discover -s Tests -p 'test_ghost_navigation_rig.py' -v
python3 -m unittest discover -s Tests -p 'test_ghost_corner_scare.py' -v
python3 -m unittest discover -s Tests -p 'test_escape_agimat_assets.py' -v
```

The generated GLB passes Khronos glTF validation. These checks do **not** establish Unity Avatar validity, correct retargeting, bake performance or reachable hallway routes. Unity 6000.5.4f1 is unavailable in the sandbox.

In Unity:

- Import the moved FBXs and generated GLB; confirm no compile/import errors and a valid Humanoid Avatar at runtime.
- Enter PoliceHQ. Confirm the original “Failed to create agent” warning is gone, and `Whitelady` has an enabled agent on the generated NavMesh. Inspect the navigation overlay at all three initial patrol waypoints, doors and four checkpoint areas.
- Observe idle and patrol walking, floor alignment, model facing and dress/shoulder deformation. Pause/resume should freeze/resume both navigation and animation.
- Chase, use an agimat, save checkpoints and retry. Confirm wall collisions/pathfinding, retreat movement, safety spacing and checkpoint patrol regions.
- After adding GhostScream, trigger both corner scares and a catch. Check full scream playback with the sound, transition to walking on wall exit, no active agent on visual copies, and continued scream animation over the paused lose screen.
- Run a player build: runtime baking must work without readable model meshes. Bake time and navigation around closed doors still need an Editor/player measurement.

## Model attribution

This work is based on **A HORROR GIRL CHARACTER CODE99/type101** by **yongtun**, licensed under **CC BY 4.0**. This derivative adds a humanoid skeleton, skin weights and a T-pose; original model and texture attribution remains applicable.

- https://sketchfab.com/3d-models/a-horror-girl-character-code99type101-e4c599eff4c5449b83606da562c76afb
- https://sketchfab.com/yongtun
- https://creativecommons.org/licenses/by/4.0/
