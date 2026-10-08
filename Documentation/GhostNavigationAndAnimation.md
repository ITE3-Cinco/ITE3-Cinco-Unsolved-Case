# White Lady navigation and animation

## Navigation

PoliceHQ's `=== ENEMY ===` container now has `GhostNavigation`, referencing the **Environment** hierarchy. It builds a NavMesh from enabled environment colliders in Awake, before the enemy starts. The agent is saved **disabled** in the scene and is enabled only after a valid spawn point is found on its agent-type NavMesh. This addresses the original attempt to create an agent before a NavMesh exists.

The bake uses the existing Humanoid agent settings and a 0.25-unit voxel size. Non-readable MeshColliders use their local bounds as conservative boxes, including solid door leaves; no mesh Read/Write import changes are required. The player, enemy and scare copies are outside the bake hierarchy. The generated data is removed when the scene unloads.

`GhostEnemy.requireNavigation` is enabled for PoliceHQ. The enemy stops with a descriptive error if its spawn cannot bind to the surface. It does not use the old wall-crossing patrol fallback in this scene. Agimat retreat destinations must be reachable and farther from the player. Checkpoint patrols still use the saved checkpoint, minimum player spacing and grace period.

The agent's base offset is 1.8; its visual uses a matching -1.8 foot offset. Catch distance now uses horizontal separation with a same-floor height check because the agent root and player feet have different heights.

## Locally built rig

`Assets/Resources/WhiteLadyRig.glb` contains the existing White Lady geometry and texture, fitted to a **21-joint humanoid skeleton** with four normalized weights per vertex. The original A-pose was converted to a T-pose for retargeting. It has 168,979 vertices across three skinned meshes. The original unrigged model remains in place as a fallback if Avatar creation fails.

This is a basic local rig, not an export from Mixamo. The gown follows the hips and thighs gently, and the hair follows the head. Individual fingers, facial animation and cloth simulation are not rigged. Inspect shoulder, hair and dress deformation in Unity and refine weights if needed. The generator is `Tools/build_white_lady_rig.py` (Python 3 + numpy); it preserves UVs and embeds the original texture. Fetch the original texture with Git LFS before regenerating.

At runtime, `GhostAnimation` instantiates the rig, builds a Humanoid Avatar, and drives idle/walk/scream through Unity's Animation Playables. Root motion is disabled so the NavMeshAgent owns movement. Walking is selected from actual position changes, so an enemy stopped by a wall is not intentionally told to walk. Blend duration defaults to 0.15 seconds.

`GhostIdle`, `GhostWalk` and `GhostScream` live in **Assets/Resources/GhostAnimations** and import as Humanoid with Create From This Model. The added GhostScream FBX was moved with its GUID intact. Invalid/non-Humanoid Inspector overrides fall back to these resources. PoliceHQ deliberately uses the GhostWalk resource for patrols; its old Generic GhostWalking override was removed. The alternative GhostWalking asset also imports as Humanoid now.

## Initial patrol and artifact gating

The old spawn at (-63, 2, 112) and first waypoint at (-47.74, .2, 95.68) were outside the corridor floor colliders. The initial spawn is now (-63, 2, 143), along the continuous northern corridor. The three existing waypoint objects retain their x coordinates and now sit at floor height (.32) along z=143. Patrol destinations are sampled onto the agent's surface and require complete paths. Unreachable/null waypoints are skipped; if all fail, the ghost retries once per second. Chase and retreat invalidate the patrol path so it resumes correctly afterward.

Patrol starts immediately at zero artifacts. Hunting and physical catches unlock after the first artifact (`artifactsBeforeHunt`); corner encounters retain their separate one- and two-artifact gates. Generic jump-scare triggers also require an artifact, including when the player collects it while already inside the trigger. Line-of-sight tests ignore the ghost/player hierarchies without excluding walls that share the Default layer.

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
- At zero artifacts, approach the ghost and verify it continues its route without chasing/catching. Collect the first artifact and verify hunting begins only with line of sight. Observe idle and patrol walking, floor alignment, model facing and dress/shoulder deformation. Pause/resume should freeze/resume both navigation and animation.
- Chase, use an agimat, save checkpoints and retry. Confirm wall collisions/pathfinding, retreat movement, safety spacing and checkpoint patrol regions.
- With GhostScream imported, trigger both corner scares and a catch. Check full scream playback with the sound, transition to walking on wall exit, no active agent on visual copies, and continued scream animation over the paused lose screen.
- Run a player build: runtime baking must work without readable model meshes. Bake time and navigation around closed doors still need an Editor/player measurement.

## Model attribution

This work is based on **A HORROR GIRL CHARACTER CODE99/type101** by **yongtun**, licensed under **CC BY 4.0**. This derivative adds a humanoid skeleton, skin weights and a T-pose; original model and texture attribution remains applicable.

- https://sketchfab.com/3d-models/a-horror-girl-character-code99type101-e4c599eff4c5449b83606da562c76afb
- https://sketchfab.com/yongtun
- https://creativecommons.org/licenses/by/4.0/
