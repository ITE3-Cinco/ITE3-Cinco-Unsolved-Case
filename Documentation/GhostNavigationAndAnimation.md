# White Lady navigation and animation

## Navigation

PoliceHQ's `=== ENEMY ===` container now has `GhostNavigation`, referencing the **Environment** hierarchy. It builds a NavMesh from enabled environment colliders in Awake, before the enemy starts. The agent is saved **disabled** in the scene and is enabled only after a valid spawn point is found on its agent-type NavMesh. This addresses the original attempt to create an agent before a NavMesh exists.

The bake uses the existing Humanoid agent settings and a 0.25-unit voxel size. Non-readable MeshColliders use their local bounds as conservative boxes, including solid door leaves; no mesh Read/Write import changes are required. The player, enemy and scare copies are outside the bake hierarchy. The generated data is removed when the scene unloads.

`GhostEnemy.requireNavigation` is enabled for PoliceHQ. The enemy stops with a descriptive error if its spawn cannot bind to the surface. It does not use the old wall-crossing patrol fallback in this scene. Agimat retreat destinations must be reachable and farther from the player. Checkpoint patrols still use the saved checkpoint, minimum player spacing and grace period.

The agent and generated rig use a feet origin: base offset and visual offset are both zero. The capsule center is one metre above that origin with a two-metre height. This removes the legacy center-pivot offsets that submerged the replacement rig. Catch distance uses horizontal separation with a same-floor height check; the 1.2-metre range includes the ghost radius, player radius and controller skin. Sight lines target the player's controller center.

## Locally built rig

`Assets/Resources/WhiteLadyRig.glb` contains the existing White Lady geometry and texture, fitted to a **21-joint humanoid skeleton** with four normalized weights per vertex. The original A-pose was converted to a T-pose for retargeting. It has 168,979 vertices across three skinned meshes. The original unrigged model remains in place as a fallback if Avatar creation fails.

This is a basic local rig, not an export from Mixamo. The gown follows the hips and thighs gently, and the hair follows the head. Individual fingers, facial animation and cloth simulation are not rigged. Inspect shoulder, hair and dress deformation in Unity and refine weights if needed. The generator is `Tools/build_white_lady_rig.py` (Python 3 + numpy); it preserves UVs and embeds the original texture. Fetch the original texture with Git LFS before regenerating.

At runtime, `GhostAnimation` instantiates the rig, builds a Humanoid Avatar, and drives idle/walk/scream through Unity's Animation Playables. Root motion is disabled so the NavMeshAgent owns movement. Walking is selected from actual position changes, so an enemy stopped by a wall is not intentionally told to walk. Blend duration defaults to 0.15 seconds.

`GhostIdle`, `GhostWalk` and `GhostScream` live in **Assets/Resources/GhostAnimations** and import as Humanoid with Create From This Model. The added GhostScream FBX was moved with its GUID intact. Invalid/non-Humanoid Inspector overrides fall back to these resources. PoliceHQ deliberately uses the GhostWalk resource for patrols; its old Generic GhostWalking override was removed. The alternative GhostWalking asset also imports as Humanoid now.

## Initial patrol and artifact gating

The old spawn at (-63, 2, 112) and first waypoint at (-47.74, .2, 95.68) were outside the corridor floor colliders. The initial spawn is now (-63, .32, 143), along the continuous northern corridor. The three existing waypoint objects retain their x coordinates and now sit at floor height (.32) along z=143. Patrol destinations are sampled onto the agent's surface and require complete paths. Unreachable/null waypoints are skipped; if all fail, the ghost retries once per second. Chase and retreat invalidate the patrol path so it resumes correctly afterward.

Patrol starts immediately at zero artifacts. Hunting unlocks after the first artifact (`artifactsBeforeHunt`); physical contact can catch the player at any artifact count, even if navigation could not initialize. Checkpoint grace, invisibility and repulsion still prevent catches; corner encounters retain their separate one- and two-artifact gates. Line-of-sight tests ignore the ghost/player hierarchies without excluding walls that share the Default layer.

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
- At zero artifacts, approach the ghost and verify it continues its route without chasing. Contact must still show the Lose Canvas, stop player controls, play the scream and pause gameplay. Test Retry and Exit. Collect the first artifact and verify hunting begins only with line of sight. Observe idle and patrol walking, floor alignment, model facing and dress/shoulder deformation. Pause/resume should freeze/resume both navigation and animation.
- Chase, use an agimat, save checkpoints and retry. Confirm wall collisions/pathfinding, retreat movement, safety spacing and checkpoint patrol regions.
- With GhostScream imported, trigger both corner scares and a catch. Check full scream playback with the sound, transition to walking on wall exit, no active agent on visual copies, and continued scream animation over the paused lose screen.
- Run a player build: runtime baking must work without readable model meshes. Bake time and navigation around closed doors still need an Editor/player measurement.

## Model attribution

This work is based on **A HORROR GIRL CHARACTER CODE99/type101** by **yongtun**, licensed under **CC BY 4.0**. This derivative adds a humanoid skeleton, skin weights and a T-pose; original model and texture attribution remains applicable.

- https://sketchfab.com/3d-models/a-horror-girl-character-code99type101-e4c599eff4c5449b83606da562c76afb
- https://sketchfab.com/yongtun
- https://creativecommons.org/licenses/by/4.0/

## Regression checks added for gameplay fixes

`Assets/Tests/Editor/GhostGameplayTests.cs` uses Unity's Edit Mode Test Runner to check the inactive-canvas lifecycle, close-range catches before hunting/navigation, checkpoint protection and exposure adjustments without accumulation. These tests require Unity and were not executed in the cloud sandbox. The Python suite checks both scare markers for corridor support, body clearance, capsule/catch spacing and lighting wiring.

The lose controller records the show request before activating its canvas, so a delayed Awake cannot hide a requested loss screen. PoliceHQ currently uses a separate active controller; this also supports placing it on an inactive canvas.

## Lighting and brightness

PoliceHQ uses dim trilight ambient colors, reflection intensity 0.2, directional intensity 0.08 and corridor fixture intensity 4. The flashlight retains intensity 200 and its existing range. Camera post-processing is enabled. `SceneBrightness` is attached to the global volume so saved brightness applies when PoliceHQ is opened directly as well as through the menu.

Brightness 5 preserves the authored exposure (-0.7 EV); 0 and 10 add -2 and +2 EV. The slider modifies the runtime volume profile, preserving the asset and avoiding cumulative offsets. Scene-local menu callbacks forward to the persistent settings controller.

In Play Mode, compare flashlight off/on in a corridor at brightness 0, 5 and 10. Check menu → opening scene → PoliceHQ, direct PoliceHQ play, retry and returning to the menu. Confirm repeated slider changes do not accumulate. The selected light levels still require visual tuning in Unity; asset checks cannot establish final rendered visibility.
