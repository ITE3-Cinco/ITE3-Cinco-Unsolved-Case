# Map checkpoints

All four `Map Holder` objects in PoliceHQ and all four in `Environment.prefab` now have:

- An enabled `RespawnPoint` with its own child **Respawn Position**.
- An enabled `Interactable`, with `isCheckpoint` enabled and an explicit reference to that RespawnPoint.
- An enabled BoxCollider trigger fitted to the map's unit cube mesh. The interaction raycast explicitly includes triggers.

Look at a map within 3 metres and press **E**. It displays “Checkpoint updated” with the total collected-item count. The latest map replaces the previous checkpoint; interacting does not collect or destroy the map. Walking past a map does not activate it.

`Map Holder (3)` in PoliceHQ was embedded in a wall. It has been moved 0.2 metres toward the corridor so interaction rays can reach it.

Spawn positions are on the corridor side (+Z), 1.5 metres from each map, at a world center height of 1.4 metres in the current assets. They have upright rotations and floor support for the current player capsule. They follow the map hierarchy when it is moved. Recheck these positions if the map layout or player capsule changes.

## Respawn and retry

- Falling below `killYDepth` teleports the player to the latest checkpoint and clears movement velocity.
- Before any checkpoint is activated, falling returns the player to their original starting pose (or an explicitly assigned fallback Transform).
- After a ghost catch, **Try Again** reloads PoliceHQ and starts the player at the last map checkpoint. The inventory, selected slot, flashlight ownership, artifact count, and required collectible groups restore to the snapshot taken at that map. Saved pickups stay removed; items collected after saving return to the world. Enemies reset with the scene reload.
- Restarting normally, exiting to the menu, loading an unrelated scene, or starting a new Editor Play session clears the checkpoint. Only an explicit Try Again carries it across one reload of the same scene.
- Checkpoints are held in memory; they are not saved across application restarts.

The lose screen now releases the cursor for its buttons. Its delayed `Start` no longer resumes time after the lose panel is first activated.

## Checks

Run serialized asset regressions from the repository root:

```sh
python3 -m unittest discover -s Tests -v
```

In Unity 6000.5.4f1:

1. Open PoliceHQ. Ensure all scripts compile and there are no missing script references.
2. Enter Play Mode, pass a map without pressing E, and verify it does not activate. Move within 3 metres, aim at the map, and press E. Check the prompt and confirmation for each of the four maps.
3. Set a checkpoint, fall below Y = -20, and verify that the player returns upright on the corridor floor without retaining falling velocity. Repeat after setting a different map.
4. Before touching any map in a fresh run, fall below Y = -20. Verify return to the original player start, not to a map mounted on a wall.
5. Activate a map, get caught by the ghost, and click Try Again. Verify return to that map with normal movement, working cursor lock, and restored inventory/artifact progress. Verify that saved pickups remain absent and pickups collected after saving reappear. Repeat twice to ensure subsequent retries still work.
6. Retry before activating any map; verify the original player start. Use Exit and start a new game; verify the previous checkpoint is cleared. Check normal Restart as well.
7. Confirm the loss panel keeps the game paused when first shown and its buttons are clickable. Check both ordinary Play Mode and Play Mode with domain reload disabled.
8. Instantiate the environment prefab in a test scene with a player and camera. Verify all four maps are interactable and their spawn children are clear of walls and props.

The sandbox checks serialized wiring and analytical box clearance only. Unity compilation, mesh collision checks, and the Play Mode steps remain required because the Editor is unavailable here.

## Level 1 escape and collection history

The Archive A escape now loads the enabled `Lvl-2` build scene using the existing unscaled fade and asynchronous load. It still requires all artifacts and proximity to the final exit. Input is blocked while loading.

The escape carries the session's collected-item history and total into Level 2. Level 2 keeps its authored starting inventory and independent note objective; Level 1 evidence does not occupy its hotbar or satisfy its notes. The Level 2 entrance becomes the retry baseline for that history even before finding a map. `CollectionProgress.TotalCollected` includes successful pickups without ItemData, excludes checkpoint maps and failed pickups, and counts each world pickup once.

Additional Unity checks:

1. Collect a flashlight and some artifacts, save at a map, then collect another item or consume an Agimat. Retry twice: saved inventory and counts must match the map snapshot each time, and restored artifacts must still allow completing the escape objective.
2. Collect all Level 1 artifacts, walk to Archive A, and verify one fade into `Lvl-2`, with normal controls after the fade. Verify `CollectionProgress.TotalCollected` retains the Level 1 total while Level 2 notes start at zero.
3. Lose in Level 2 before activating a map and retry: the carried total must remain. Collect a Level 2 note and verify the total increases by one. A new game or ordinary restart clears prior history.
4. Run the Editor `CollectionProgressTests` alongside existing gameplay tests. These verify snapshot isolation, repeated retries, inventory restoration, scene boundaries, duplicate identity, and fresh-run resets.
