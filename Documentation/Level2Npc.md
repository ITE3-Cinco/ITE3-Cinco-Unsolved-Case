# Level 2 frightened woman and Notes

Base: current `level-2-merge`, commit `5ef1ec3719f8bb43e3f4a9a5712a70f960857653`, including the placed `char_npc` update. The task checkout started on main without Level 2, so it was fast-forwarded to this branch first. This implementation preserves that branch's current `Assets/Scenes/Lvl-2.unity` environment, lighting, three Notes, White Lady navigation/chase and loss/retry configuration. It does not use the earlier note-quest branch's environment.

## Gameplay

- A stationary woman waits near the starting passage at `(-83.489, -1.745, 164.93)`, preserving the placed `char_npc` position and facing.
- Approach, aim at her and press **E**, using `PlayerInteraction` and her `Interactable` collider. She faces the player on interaction.
- Her first conversation asks for **all three Notes**, without identifying Renato or herself. The existing objective holder becomes **Find all the clues and return to her.**
- Notes still use the existing Note Artifact prefab, `Item_Note`, inventory and `ArtifactManager`. No second collection counter exists. Notes found before speaking to her still count; the initial conversation is always shown first.
- An early return reports missing Notes. Collecting the last Note changes the objective to return; collection alone cannot complete the story gate.
- Interacting after all Notes acknowledges the evidence and invokes `onCluesReturned` once. Further E interactions deliver the lore in three short pages. Renato is identified as the coworker and true serial killer; she identifies herself as the White Lady and his victim, and explains her warning and search for justice.
- A final E interaction uses `ScreenFadeController` / `SceneTransition` to continue to the existing Level3 scene. The White Lady stays active until that transition begins. Talking does not pause or protect the player.

There was no dialogue controller in the confirmed branch. The NPC extends `Interactable` and displays its lines in the existing `NotificationManager` subtitle. The existing notification script is named `NotificationManager.cs` to match its MonoBehaviour class; its GUID is preserved so all scene/prefab references continue to resolve. Each line is shown for 18 seconds; aim at her and press E to advance the return/lore conversation. Only the Level 2 notification layout and player prompt reference are overridden. Artifact pickup messages are configurable with the original Level 1 defaults retained.

## Stationary animation and retry

`StationaryNervousIdle` reuses the project's Playables animation approach. The existing scene instance of `Assets/Assets/free assets/char_npc/char_npc.fbx` is now a child of the quest/interaction root, with the collider at the same location. It is visible in Edit Mode and reused in Play Mode; no additional woman is instantiated from Resources. The old Resources assets are retained but no longer used by this NPC.

The sibling `Nervously Look Around.fbx` is assigned directly through `nervousClip`, imports as Humanoid, and uses the full 228-frame Mixamo take at 30 fps (7.6 seconds). Looping and root rotation/Y/XZ baking are enabled in the importer; runtime root motion is disabled. Playback wraps continuously. The NPC has no Rigidbody or NavMeshAgent. It queries only the current environment's colliders once, after existing navigation initialization, and aligns the sampled mesh soles to the floor. Interaction turns only the quest root toward the player; its position stays fixed. The current environment, Notes, UI and White Lady/retry serialized objects are unchanged by this asset-wiring fix.

Story state is local to the NPC. The existing Lose Canvas retries `Lvl-2`, recreating the NPC, tracker, inventory and Notes together. Checkpoint spawn handling remains in `RespawnPoint`/`PlayerRespawn`. In-scene fall respawns preserve collected items and story state, matching their existing behavior.

## Validation

Static checks:

```sh
python3 -m unittest discover -s Tests -p 'test_level2_*.py' -v
```

Unity 6000.5.4f1: run Edit Mode `Level2NpcTests` and the existing `GhostGameplayTests`. The new tests cover scene references, avatar/clip imports, fixed position across animation samples/loops, first/partial/full/duplicate/precollected Notes, lore gating, frozen gameplay and scene reset. Tests use reflection to access Assembly-CSharp, following the existing test assembly convention.

Play Mode checks still required:

1. Approach her from spawn. Verify her feet, visible idle, fixed position and E prompt; talk and read the complete initial subtitle.
2. Collect one/two Notes; revisit her. Verify progress, no premature reveal and an active White Lady.
3. Get caught. Verify the existing lose UI, retry spawn, all three restored Notes, empty progress and initial NPC conversation. Repeat after all Notes and after the return dialogue.
4. Collect all three Notes through E/inventory. Return, receive acknowledgment, then read each lore page. Confirm no transition occurs merely from collecting the last Note.
5. Press E after the final lore page and verify Level3 loads. Check pause, floor respawn, and keyboard interaction while under threat.

The cloud sandbox has no Unity Editor, so compilation, imported asset validation and Play Mode results are not claimed. Run the scoped Python checks above for serialized wiring and environment preservation. The Unity tests also check that the assigned asset is `char_npc`, the visible model and interaction collider share a root, only one Animator exists before/after startup, and both root and visual remain stationary across animation loops. Compilation, actual retargeting, floor contact and gameplay still need the editor checks above.
