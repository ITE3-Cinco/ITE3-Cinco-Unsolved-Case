# Level 2: recovered Notes

Open `Assets/Scenes/Lvl-2.unity` in Unity **6000.5.4f1**. Fetch Git LFS assets before importing (`git lfs install --local`, then `git lfs pull`). No additional Inspector assignments are required for the checked-in configuration.

## Gameplay

1. Dwight starts with the existing flashlight in the northern part of the backrooms. Approach the frightened woman and press **E** while looking at her.
2. Her request appears in the existing notification label, and the objective changes to finding three Notes. Attempting a pickup beforehand explains that the player must speak to her first.
3. Find the Notes near the northern column, in the central recess, and at the southern dead end. Each uses the existing Note ItemData, scanned Note material, mesh and Interactable pickup path. The same Note icon occupies one hotbar slot; the existing artifact counter and objective show the collected/remaining total. Only Level 2's inventory opts into stacking this item.
4. The White Lady patrols from the western corridor. The first Note unlocks her existing artifact-based hunting rule. Detection, line of sight, chase, catch, animations, humming and scream use the unchanged Level 1 scripts/assets.
5. After the last pickup, the objective says to return to the woman. Talking returns the Notes, plays her thanks, fires `onCompleted` once, and changes the quest to Completed. The NPC stays in the scene. One further **E** interaction continues to Level3 using the existing SceneTransition, allowing the completion dialogue to be read first.

Quest states: `NotStarted → FindingNotes → AllNotesCollected → Completed`. A retry restarts this level and the quest. There is no new save system.

## Reused systems and changes

- Existing `PlayerInteraction`, `Interactable`, `Inventory`, `ArtifactManager`, `ObjectiveItem`, `NotificationManager`, player, HUD/hotbar, pause, lose/retry and scene transition systems.
- `Interactable.Interact` is now virtual, with an optional quest gate and a same-frame duplicate guard. Other scenes leave the quest reference empty.
- `Inventory.stackableItems` is an opt-in list configured with Item_Note only in Level 2. Other inventories retain their original slot behavior.
- `GhostEnemy`, `GhostAnimation`, `GhostAudio` and `GhostNavigation` are reused without code changes. `WhiteLady.prefab` packages Level 1's existing enemy components/audio/rig references for reuse. Level 1 retains its existing scene enemy.
- New scripts: `Level2NoteQuest` coordinates existing managers; `Level2NoteNpc` subclasses Interactable and plays the supplied nervous clip using the project's Playables approach. There was no existing conversation/quest state controller to extend.
- New prefabs: `Level2Note`, `Level2NoteNpc`, `WhiteLady`.
- `Lvl-2.unity`: removed the sample item group, placed three Notes/NPC/enemy/patrol points, assigned quest and existing UI references, corrected player placement and the interaction prompt, initialized the flashlight inventory, disabled duplicate flashlight pickup behavior and the redundant light, configured retry and a brightness volume.
- The Level 2 player radius is 0.4 m (plus the existing 0.08 m skin) to fit the central recess doorway. Navigation uses 0.4 m radius, 1.8 m height and 0.1 m voxels. Player movement code and Level 1 settings are unchanged.
- `PoliceHQ.unity`: only its exit destination changes from the nonexistent `Level2` to `Lvl-2`. Build Settings now enables `Lvl-2` before Level3. Level3's ending is unchanged.

## Asset repair and NPC presentation

Level 2 referenced a deleted `backrooms_with_baked_textures.glb`. The exact asset and its original GUID were restored from the parent of `21dfcdfac4c1dbe59945d943ea7a1dba9c02bce7`. This preserves the existing scene's model hierarchy, transform and 23 mesh colliders. The restored folder uses a fresh GUID because its old GUID was reused by another folder.

The existing `Ch07_nonPBR.fbx` and `Nervously Look Around.fbx` moved, with their GUIDs intact, into `Assets/Resources/Level2NPC`. Textures and materials stay in their original location and remain referenced by GUID. The nervous clip now imports as Humanoid to retarget to the existing NPC avatar.

The NPC prefab places her interaction collider in the scene. Her existing visual model and animation load at runtime, like the existing White Lady visual. Therefore the model is visible in Play Mode; Edit Mode shows her prefab root/collider. `animator` and `nervousIdle` can instead be explicitly assigned on the component. With them empty, the configured Resources paths load the shipped assets. Root motion is off, and the nervous clip repeats without moving her interaction point.

## Inspector customization

- On **Level 2 Note Quest**, edit request/completion dialogue and duration, `nextSceneName`, or `onCompleted`.
- To change the Note count, add/remove `Level2Note` prefab instances, assign their `noteQuest`, and update the quest's `notes` array. Total count is derived from valid distinct references; all must use the assigned Note ItemData and `isArtifact`. No other artifact types are configured.
- Move Notes/NPC/patrol points within the walkable floor. Keep enough room to approach and aim at each Note. Re-run the Unity navigation test after geometry changes.
- The White Lady prefab exposes the existing AI/audio settings. Its scene instance references the player, lose UI and five patrol transforms. Runtime navigation bakes only the existing environment hierarchy before her agent starts; no manual bake is required.

## Verification

Sandbox checks:

- Five Level 2 serialized tests pass: local references, Note-only configuration, restored environment, reused enemy wiring, NPC resources and scene progression.
- Serialized YAML and GUID checks pass for the modified Level 2 scene/new prefabs/build settings; no duplicate asset GUIDs were introduced.
- A geometry check using the restored model's actual triangle buffers connected all placement/approach locations on a 0.1 m grid at ghost and player clearances. This caught and corrected a patrol point overlapping a column and the original player radius being too large for the recess doorway. It is not a Unity NavMesh bake.
- The full Python suite has 29 tests: 21 pass, 2 fail and 6 error. All eight remaining issues also occur at the starting revision: old Level 1 assertions look for player/UI/manager document IDs that have moved to prefabs. The baseline has an additional brightness failure now resolved by following inherited cameras in the shared test.

Unity is not installed in the sandbox, so compilation, FBX imports, Play Mode and visual/audio verification remain unexecuted. Run the Unity Test Runner's **EditMode** suite, including `Level2NoteQuestTests` (which enters Play Mode). It checks imported NPC/ghost animations, runtime navigation to Notes/NPC/patrol points, early pickup rejection, first/repeat dialogue, full inventory rejection, duplicate pickup, stacking, final return state and enabled Level3 destination.

Then manually verify:

- Play Level 1: original artifact types, hotbar, flashlight, checkpoint, corner scares, White Lady catch/retry and escape into Level 2.
- In Level 2, aim at each Note/NPC: **E** prompt, collider/raycast reach, Note visibility, readable request/progress/return/completion text and nervous animation without root drift.
- Observe the White Lady patrolling, then collect one Note: line of sight through doorways, walls blocking detection, chase/catch, humming/scream, Lose Canvas, retry to Level 2, pause/resume and controls.
- Collect all Notes and return: completion only on NPC interaction, Notes removed from inventory, no Level 1 escape objective appearing, further interaction fading to Level3, and Level3's existing ending still available.
- Check the Console for missing references/import errors, and inspect narrow doorway traversal, lighting and animation foot alignment. Runtime visuals, timing and difficulty still need this playtest.
