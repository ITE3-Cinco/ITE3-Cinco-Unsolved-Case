# Level 2 mechanics reviewer

Use this as study or presentation notes for `Assets/Scenes/Lvl-2.unity`. It explains the mechanics present in Level 2, including the shared systems that this level uses. **Story spoilers follow.**

## 1. Main goal and gameplay flow

The player must talk to the frightened woman, find all **three Notes**, return to her, and listen to the truth behind the case. Collecting the final Note makes the return trip safe. The player then finishes the conversation and interacts again to continue to Level 3.

**Remember:** talk → search and survive → collect all three → return safely → hear the reveal → continue.

## 2. Interacting with the NPC

Approach the woman, aim at her, and press **E**. `PlayerInteraction` casts a ray from the camera, finds the target's `Interactable` component, and calls its `Interact` method. The shared interaction range defaults to three units. Simply standing near her does not start a conversation.

`Level2NoteNpc` inherits from `Interactable` and overrides `Interact`. This lets the same E input handle a conversation rather than treating the woman as an inventory pickup. `StationaryNervousIdle.FacePlayer` turns her toward the speaker while keeping her position fixed.

The first conversation tells the player to find all the Notes and come back. It also changes the objective to **Find all the clues and return to her.** The next interactions finish any pages of that line, show the player's reply, and close the box.

The NPC has two roles:

- **Guide:** explain the task, report how many Notes are missing, and tell the player when to return.
- **Story source:** reveal what happened once the player has collected the evidence.

## 3. How dialogue advances

`Level2NoteNpc.Interact` handles an E press in this order:

1. Check that gameplay is active: no pause, loading, transition, or disabled player movement.
2. Turn toward the speaker and remember their position.
3. Show another text page if the current line does not fit in the panel.
4. Show a pending player reply, or close that reply when requested.
5. Advance the current story step.

`DialogueBox` shows the speaker name, dialogue text, and E hint. Text stays until the conversation closes; NPC dialogue is not a timed pickup notification. Long lines use additional pages, so one E press does not always mean one new story entry.

The panel hides during pause/loading or when the player walks more than four units away. Hiding preserves the conversation text and progress. Movement is still possible while talking, and the search remains dangerous until all Notes are collected.

**Input detail:** if the player carries an agimat and selects it, or a hostile ghost is nearby, E gives agimat use priority. That press can consume protection instead of advancing dialogue.

## 4. The Note quest and its state machine

A **state** is the story step the game remembers. The next interaction depends on that step.

| State | Meaning | What happens next |
| --- | --- | --- |
| `NotStarted` | The player has not spoken to the woman. | Show her request first. Start the search, or recognize Notes collected early. |
| `FindingClues` | The player has heard the request and Notes are missing. | The ghost can hunt. A return visit reports the missing count. |
| `CluesCollected` | All Notes are found; the player has not completed the return conversation. | The ghost is harmless. Returning shows the reassurance. |
| `Returned` | The reassurance has been shown. | Begin the lore entries. |
| `Revealing` | The player is reading the lore. | Show the next entry after the current line's pages are finished. |
| `ReadyToLeave` | The final lore entry has been shown. | Finish any remaining pages, then interact again to load Level 3. |

The enum's declaration order differs from this gameplay order. The branches in `Interact` and the pickup event determine the actual flow.

### Following one Note through the code

1. The player aims at a Note and presses E.
2. `Interactable.Interact` tries to add its `ItemData` to `Inventory`. A full inventory cancels the pickup.
3. Because the Note is marked `isArtifact`, the pickup calls `ArtifactManager.CollectArtifact`.
4. `ArtifactManager` records unique pickups, updates the counter, and raises `ArtifactCountChanged`.
5. The NPC's `OnClueCollected` receives the event and calls `RefreshClueState`.
6. `AllCluesCollected` compares the current count with the required total. In this level, the total is three.
7. At completion, the NPC makes the White Lady non-hostile and updates the return objective.

An **event** is a notification between scripts: the Note counter tells the NPC that progress changed. The NPC does not maintain another Note counter. Duplicate collection of the same object cannot increase the manager's count twice.

Notes found before meeting the woman still count. The first conversation always plays before the reassurance or reveal. Returning does not consume the Notes; the NPC checks the collection counter.

## 5. White Lady: danger during the search, safety after completion

`GhostEnemy.IsHostile` uses the NPC's story state and Note completion:

- Before the first conversation, the White Lady is dormant.
- After the request, she can hunt while the state is `FindingClues` and Notes remain missing. Talking unlocks hostility; detection, sight, navigation, and protection still control her actual pursuit.
- Collecting the final Note immediately ends hostility, before another AI update or conversation is needed.

`BecomeNonHostile` stops pursuit, clears movement, disables the navigation agent and ghost colliders, and hides her renderers. The object remains in the scene. Her humming fades through the existing audio system. The catch checks also reject a non-hostile ghost, protecting the return trip and lore conversation.

This safety lasts for the current scene run, including an in-scene fall respawn or component re-enable. Reloading Level 2 for a fresh retry recreates the quest and ghost state.

The later line **“The White Lady wasn't trying to kill you”** describes her story motivation. During the incomplete search, the implemented ghost can still catch the player and trigger the lose screen.

## 6. Lore revealed by the NPC

After all Notes are collected, the woman first reassures the player that the ghost will no longer hurt them. Further interactions reveal seven story entries, in order:

1. Renato, the coworker, is the serial killer behind the cold case; the Notes are evidence.
2. Knowing the truth makes the player a target.
3. The woman in white followed the player because she needed them to listen.
4. The White Lady was not trying to kill the player, according to the woman's account.
5. She wanted to warn the player about Renato and seek justice.
6. The frightened woman reveals that **she is the White Lady**.
7. She identifies herself as one of Renato's victims and tells the player to carry the evidence onward and survive.

`revealPage` is the index of the next lore entry. `Say(revealDialogue[revealPage++])` shows the current entry and then advances the index. Reaching the final entry sets `ReadyToLeave`; it does not immediately load another level.

`onCluesReturned` fires once when the completed return is accepted. Later interactions use the reveal states, so they do not repeat that event.

## 7. Supporting mechanics present in Level 2

| Mechanic | How it works in this level |
| --- | --- |
| Agimat | One pickup in the north passage. E collects it; selecting it or receiving the nearby hostile-ghost warning lets E consume it for five seconds of protection and ghost retreat. It does not count as a Note. Active protection prevents repeated consumption. |
| Flashlight and hotbar | The player starts with a flashlight. F toggles its beam. Selecting another slot hides the beam; selecting the flashlight again restores its saved on/off state. |
| Lighting and flicker | Five corridor lights and visible fluorescent panels dim together in short bursts. The flashlight remains steady. Scaled time makes flicker stop during pause. |
| NPC idle | The nervous Humanoid clip loops through a manually controlled animation graph. Root motion is disabled, so the animation changes her pose without making her walk. Startup aligns the model's feet to the map floor. |
| Loss and retry | A hostile catch uses the existing lose UI. Level 2 retry reloads the scene, restoring its Notes and agimat and restarting the conversation. |
| In-scene fall respawn | The existing respawn system moves the player back while keeping current inventory, Note progress, and NPC story state. Final-Note safety remains active. |
| Final transition | `ContinueStory` checks that Level 3 is loadable, closes dialogue, blocks duplicate requests, freezes gameplay, and calls the shared fade controller with a 1.5-second fade. |

These systems appear in Level 2; some are shared with other scenes. This reviewer covers their Level 2 use.

## 8. Where to read the logic

| Question | Script and method |
| --- | --- |
| How does E find the NPC? | `PlayerInteraction.Update` |
| What happens when the player talks? | `Level2NoteNpc.Interact` |
| How does the final Note make the return safe? | `ArtifactManager.CollectArtifact` → `OnClueCollected` → `RefreshClueState` |
| How are tasks and interaction hints chosen? | `Level2NoteNpc.UpdateObjective` |
| How do text pages work? | `DialogueBox.Show` and `AdvancePage` |
| How does the ghost become harmless? | `GhostEnemy.IsHostile` and `BecomeNonHostile` |
| Why does the NPC stay in place? | `StationaryNervousIdle.Start`, `LateUpdate`, and `FacePlayer` |
| How does the player leave? | `Level2NoteNpc.ContinueStory` |

## 9. Quick oral review

**Why can we talk to the NPC with the pickup key?** She inherits `Interactable` and overrides its interaction behavior.

**What prevents an early lore reveal?** The return branch checks `AllCluesCollected` before entering the reveal states.

**When does the return trip become safe?** During the final successful Note pickup event.

**Does the last Note load Level 3?** No. The player must return, read the reassurance and lore, then interact again.

**What does retry reset?** The scene's quest, conversation, Notes, agimat, and ghost state. An in-scene fall respawn preserves current progress.

### Short presentation script

“Level 2 combines a Note search with an NPC conversation. Aiming at the frightened woman and pressing E calls her interaction method. She explains that we need three Notes, and the search allows the White Lady to hunt. Each successful Note pickup updates one shared counter. Its event tells the NPC when all three are found, immediately making the ghost harmless. Returning then unlocks the ordered lore reveal: Renato is the killer, and the woman is the White Lady, one of his victims. After reading the final line, another interaction fades into Level 3.”
