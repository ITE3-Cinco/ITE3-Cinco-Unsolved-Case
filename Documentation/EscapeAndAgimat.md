# PoliceHQ escape, agimat and checkpoint patrols

## Escape

Collecting all three artifacts adds **Escape via Cold Case Files in Archive A.** to the permanent objective list and pickup notification. A screen overlay names **ARCHIVE A / COLD CASE FILES**, shows distance and a camera-relative bearing toward the visible `SIGN_COLD_CASE_FILES` mesh. It stays readable through walls; the bearing points toward the destination, not a computed route through rooms.

The player can escape only within **1.8 units of the collider surface on `ARCH_Wall_Plaster.085`**. `ARCH_Wall_Plaster.084 (1)` is no longer an exit. Both automatic proximity checks and `EscapeDoor` use the same guarded manager method. All artifacts, proximity, unpaused gameplay and a loadable destination are required before gameplay freezes.

With `Level2` enabled in Build Settings, gameplay freezes during a 1.5-second unscaled fade. A dedicated black overlay renders above the wake-up canvas and other UI, including when a legacy assigned fade image is hidden. A fully black frame is rendered before the next scene loads and time resumes.

## Wake-up

WakeUpCanvas starts fully black using two half-screen black images, then opens slightly, closes, opens wider, closes, and slowly opens completely. The sequence lasts about eight seconds. Normalized canvas anchors follow resolution/aspect changes and CanvasScaler. Scaled time freezes the sequence while paused, and the eyelid canvas temporarily hides so the pause menu is readable. It no longer modifies a randomly selected scene Volume's depth-of-field settings. The canvas disables itself after the final opening; its sort order stays below the escape fade.

**Level2 is deliberately deferred:** the user is preparing it on another unpublished branch. Until it is merged and included in Build Settings, the exit logs one configuration error and leaves gameplay running. Set `EscapeManager.nextSceneName` if the final scene has another name.

## Agimat

`Item_Consumable` is explicitly marked `isAgimat`. Press **E** to consume one carried agimat for five seconds of ghost immunity and retreat. A selected agimat or a nearby ghost gives defense priority over interaction. Otherwise, E interacts with the targeted item/checkpoint first and uses an agimat when no interactable is targeted. Further E presses during protection do not consume another agimat.

Within 16 units of the real ghost, carried agimat icons and slot backgrounds pulse gold and the interaction prompt offers **Press [E] to use Agimat**. The warning stops during protection and when no agimat is held. The scene ghost moves at 4.5 units/second; the 0.9-unit catch radius gives a stationary player roughly 3.4 seconds from warning range to contact. Moving toward the ghost reduces this interval. Inventory selection and the held flashlight refresh after consumption.

## Checkpoints

Saving a map checkpoint replaces the original waypoint patrol with an automatic patrol centered at its spawn location, including when retrying from that checkpoint. The default patrol radius is 18 units. Candidate points must be at least 12 units from both the checkpoint and player. For six seconds, the ghost cannot chase or catch, and retreats if the player approaches within the safety distance.

PoliceHQ now builds its NavMesh at startup, and the ghost uses reachable paths for patrol and agimat retreat. It stops if navigation is unavailable. See [Navigation and animation](GhostNavigationAndAnimation.md) for geometry collection, the humanoid rig and required Unity checks.

## Scares and sound

The ghost patrols without hunting until the first artifact. Generic jump-scare triggers also require at least one artifact. There are two independent once-per-run corner encounters under `=== ENEMY ===`:

- **Artifact 1 Corner Scare:** armed after one artifact; appears at (-56, 2, 44), exits at (-61, 2, 44), through `ARCH_Wall_Plaster.089`.
- **Artifact 2 Corner Scare:** existing encounter, armed after two artifacts, through `ARCH_Wall_Plaster.054`.

Both retain distance, camera visibility and line-of-sight gating. Both play `WhiteLady-Scream` for its full duration, even when the apparition disappears earlier. The ghost catch sound uses the same clip. Generic `JumpscareTrigger` objects with no assigned clip use the live ghost's scream reference; a playing guard prevents overlapping retriggers.

## Verification

Run serialized regressions:

```sh
python3 -m unittest discover -s Tests -v
```

Unity 6000.5.4f1 Play Mode checks (not runnable in this sandbox):

1. Import and compile. Collect 0–2 artifacts and visit the exit: no marker/transition. Collect the third: named Archive A escape prompt and persistent objective. Check the overlay direction from both approaches and with the sign behind a wall.
2. Until Level2 is merged, approach the .085 exit wall: one configuration error, no frozen gameplay. After merging and enabling Level2, test .085 in a fresh run, and confirm .084 (1) alone cannot trigger a transition: one full black fade and correct scene load, without a catch during the fade.
3. Pick up multiple agimat items. Approach the ghost with a different slot selected: gold warning and E prompt. Press E just before contact: exactly one item removed, no catch for five seconds, ghost moves away. Repeat E during protection: no additional consumption. Test protection expiry and pause/resume.
4. Test E on pickups and checkpoint maps with no threat; test defense priority while looking at a map with the ghost nearby. Verify slot selection, flashlight and full-inventory behavior after consuming.
5. Activate each map: patrol moves to that checkpoint's area, away from the player, with a six-second grace period. Retry after a catch: patrol uses the saved checkpoint. Restart from the pause menu: checkpoint state resets.
6. Trigger each corner after its required artifact count, also collecting all three before visiting either. Confirm each plays once, visibility gating works, the full scream is audible, and the visual copy cannot catch. Check pause/resume and retry resets.

7. At 16:9, 4:3 and an ultrawide aspect, verify the first frame is entirely black, both blinks close completely and the final opening clears the screen. Pause during each phase and resume. Check the scene lighting/post-processing remains unchanged.
