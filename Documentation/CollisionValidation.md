# PoliceHQ collision fixes

## Confirmed asset defects

- `DOOR_Hinge_X` had an enabled unit BoxCollider on a transform scaled to 100. Its world bounds were approximately X [-139.20, -39.20], Y [-49.68, 50.32], Z [119.30, 219.30]. This invisible volume overlaps the player's starting position and a large part of the map. It is now disabled, matching the other 21 hinge boxes already disabled in PoliceHQ.
- The meshless `PoliceHeadquarters` object had an enabled unit box near (-91.17, 0.32, 94.30). It is now disabled in the scene and environment prefab.
- All 22 oversized hinge boxes were still enabled in `Environment.prefab`. They are now disabled. Eighteen door leaves without colliders now use their visible meshes as non-convex MeshColliders; the two leaves with existing boxes retain them. The prefab's six marker boxes are triggers, matching the scene.

Map transforms, floor/ceiling colliders, walls, and existing door leaf colliders are preserved. The doors are static environment geometry; moving doors with dynamic Rigidbodies would need compound or convex colliders.

`Tools > Fix All Scene Colliders` repairs the known hierarchy boxes and marker/pickup colliders in the active scene. It includes inactive children, supports Undo, and leaves changes unsaved for inspection. It no longer snaps walls to hardcoded heights, removes negative scales, or refits every architectural mesh to a box.

PlayerMovement now checks grounding before moving, combines horizontal and vertical displacement into one CharacterController.Move call, and clears upward velocity on ceiling contact. Movement is suspended while paused or while the controller is disabled.

## Automated asset checks

From the repository root:

```sh
python3 -m unittest discover -s Tests -v
```

These validate saved collider states, door leaf mesh references, and component links. They do not simulate Unity physics.

## Unity Editor verification

Use the declared Unity Editor version in `ProjectSettings/ProjectVersion.txt` and fetch Git LFS assets before importing the project.

1. Open `Assets/Scenes/PoliceHQ.unity`. Check the Console for compile/import errors, then enable **Window > Analysis > Physics Debugger** and collision geometry visualization.
2. Select `DOOR_Hinge_X` and `PoliceHeadquarters`: their parent boxes should be disabled. Door leaves, walls, and floors should still have solid collision geometry.
3. Enter Play Mode. From the starting position, walk and sprint through the main lobby and connecting corridors in both directions. Cross floor tile seams diagonally and approach walls at shallow angles. Verify that real walls and closed door leaves still stop the player.
4. Jump from standing and while moving. Under a low ceiling, verify that upward movement stops and the player falls immediately. Check landing, stamina, flashlight controls, and pause/resume.
5. Place `Environment.prefab` in a temporary scene. Verify that marker volumes and hinge parents do not block passage and that door leaves remain solid.
6. In a disposable copy of PoliceHQ, enable the `DOOR_Hinge_X` box, run the repair command, and inspect the result. Undo should restore the box. Running the command twice should make no further changes on the second run. Confirm other loaded scenes and map transforms are untouched.

Editor compilation, mesh cooking, and Play Mode traversal require Unity and were not executable in the cloud sandbox.
