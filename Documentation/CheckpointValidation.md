# Map checkpoints

All four `Map Holder` objects in PoliceHQ and all four in `Environment.prefab` now have:

- An enabled `RespawnPoint` with its own child **Respawn Position**.
- An enabled `Interactable`, with `isCheckpoint` enabled and an explicit reference to that RespawnPoint.
- An enabled BoxCollider trigger fitted to the map's unit cube mesh. The interaction raycast explicitly includes triggers.

Look at a map within 3 metres and press **E**. It displays “Checkpoint updated.” The latest map replaces the previous checkpoint; interacting does not collect or destroy the map. Walking past a map does not activate it.

`Map Holder (3)` in PoliceHQ was embedded in a wall. It has been moved 0.2 metres toward the corridor so interaction rays can reach it.

Spawn positions are on the corridor side (+Z), 1.5 metres from each map, at a world center height of 1.4 metres in the current assets. They have upright rotations and floor support for the current player capsule. They follow the map hierarchy when it is moved. Recheck these positions if the map layout or player capsule changes.

## Respawn and retry

- Falling below `killYDepth` teleports the player to the latest checkpoint and clears movement velocity.
- Before any checkpoint is activated, falling returns the player to their original starting pose (or an explicitly assigned fallback Transform).
- After a ghost catch, **Try Again** reloads PoliceHQ and starts the player at the last map checkpoint. The rest of the level, including pickups and enemies, resets with the scene reload.
- Restarting normally, exiting to the menu, starting another scene, or starting a new Editor Play session clears the checkpoint. Only an explicit Try Again carries it across one reload of the same scene.
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
5. Activate a map, get caught by the ghost, and click Try Again. Verify return to that map with normal movement, working cursor lock, and reset scene objects. Repeat twice to ensure subsequent retries still work.
6. Retry before activating any map; verify the original player start. Use Exit and start a new game; verify the previous checkpoint is cleared. Check normal Restart as well.
7. Confirm the loss panel keeps the game paused when first shown and its buttons are clickable. Check both ordinary Play Mode and Play Mode with domain reload disabled.
8. Instantiate the environment prefab in a test scene with a player and camera. Verify all four maps are interactable and their spawn children are clear of walls and props.

The sandbox checks serialized wiring and analytical box clearance only. Unity compilation, mesh collision checks, and the Play Mode steps remain required because the Editor is unavailable here.
