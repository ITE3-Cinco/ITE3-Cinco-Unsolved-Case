# Script guide

## Five-minute code presentation

Use the comments in your assigned scripts as speaking notes:

1. **First minute:** read the `Purpose` and `Use` notes. Show the object or UI that uses the script and its Inspector references.
2. **Next two minutes:** follow the main methods and block comments. Explain the input or event, the decision the code makes, and the result it produces.
3. **Fourth minute:** demonstrate the behavior in the game, such as a pickup, sprint, checkpoint, enemy encounter or menu action.
4. **Last minute:** explain one important condition, such as a full inventory, a wall blocking sight, pausing or retrying.

Explain related lines as a group. For example: “We read the movement keys, normalize the direction so diagonal movement is not faster, and move through the CharacterController so walls block the player.”

### Following a value through the code

The comments name the source, the destination and the purpose of important values:

- **Inspector setting:** the scene, prefab or ItemData asset supplies the value. A field initializer is the code default; Unity can load a different saved value.
- **Component lookup:** `GetComponent` reads a component on the same GameObject. `GetComponentInParent` searches that object and its ancestors; `GetComponentInChildren` searches that object and its descendants.
- **Method argument:** the caller supplies a value. For example, `Inventory.SelectSlot` passes the selected ItemData to `PlayerMovement.UpdateHeldItem`, which controls the held flashlight.
- **Assignment:** `=` stores the value on the right in the destination on the left. Copying a component or asset reference points to the same object; creating a new list copies its entries into a separate container.
- **Returned value:** the caller receives a result, such as an item, a component reference or a success flag. An `out` argument also receives data; the interaction raycast writes collision details into `hit`.
- **Event argument:** the event sender supplies data to subscribed methods. `ArtifactManager` sends the artifact count; `RespawnPoint` sends the checkpoint position.
- **Asset checks:** Python tests read Unity's saved scene/prefab text. A `fileID` identifies an object inside that serialized file; a `guid` identifies an asset through its `.meta` file. These are different from runtime component searches.

For the presentation, choose one input and follow it to an output: “The E key triggers a raycast, the hit collider leads to an Interactable, the pickup adds its ItemData to Inventory, and HotbarUI reads that list to display its icon.” Use the nearby statement comments to explain each step.

### Reading Unity scripts

- **Fields and Inspector references** hold settings, linked objects and the current state. An index usually starts at zero, so inventory slot 1 uses index 0.
- **Awake** prepares a component early. **OnEnable/OnDisable** connect and disconnect events. **Start** finishes startup before the first update.
- **Update** runs each frame. **LateUpdate** runs after normal updates, useful for animation and final position checks.
- **Events** tell another system that something happened, such as collecting a Note or saving a checkpoint.
- **Coroutines** spread a sequence across frames. `yield return null` waits for the next frame; a timed wait delays the next step.
- **Time.deltaTime** measures gameplay seconds since the last frame. Setting `Time.timeScale` to zero freezes that clock. **Time.unscaledDeltaTime** uses real time, allowing fades and ending UI to continue.
- **Raycasts** test along a line, such as the player's view or an enemy's sight. **NavMesh** provides walkable routes; **CharacterController** handles solid movement and collision.
- A **return** exits a method early when its conditions are not met. A **null** check avoids using a missing object reference.

Game scripts are in `Assets/Scripts/`, with menu/cutscene components under `Assets/Scenes/`. The tables cover the scripts kept after checking serialized GUID references, code calls, runtime creation and Editor/test entry points.

## Gameplay (`Assets/Scripts/`)

| Script | Purpose / use |
| --- | --- |
| ArtifactManager | Tracks required artifacts; used by PoliceHQ and the item prefab. |
| ArtifactCornerScare | Both artifact-triggered corner encounters in PoliceHQ. |
| CollectibleGroup | Groups evidence and notifies the escape manager. |
| CollectionProgress | Saves collected-item IDs and inventory snapshots for checkpoints and retries. |
| CorridorLightFlicker | Dims corridor lights and their visible panels together. |
| DialogueBox | Builds the NPC dialogue panel, handles long-text pages and tracks its visibility. |
| EscapeManager | Unlocks Archive A escape and checks arrival at the exit wall. |
| GhostEnemy | Patrol, pursuit, catches, checkpoint patrol and agimat retreat. |
| GhostNavigation | Builds navigation from environment colliders in PoliceHQ and Level3; optional clearance overrides apply only when configured. |
| GhostAnimation | Builds the ghost rig and plays idle, walk and scream; also created for apparitions. |
| GhostAudio | Ghost humming and scare audio. |
| HotbarUI | Displays inventory slots and agimat warning. |
| Interactable | Item pickups, artifact progress and map interactions. |
| Inventory | Item storage, selection and consumption. |
| ItemData | ScriptableObject type used by the inventory assets. |
| InvisibilityConsumable | Agimat protection and nearby-ghost warning. |
| LoseSceneController | Loss UI, retry and return to menu. |
| Level3Flow | Coordinates mutually exclusive catch/escape outcomes using the existing lose and fade controllers. |
| Level3Exit | Routes the copied player's main-door trigger entry to Level3Flow. |
| Level3Key | Fits a trigger to the key model and collects it through the E-key interaction. |
| Level2NoteNpc | Manages the Note quest, safe return, story reveal and transition to Level3. |
| MouseLook | Player camera input. |
| NotificationManager | Shows timed pickup and checkpoint notifications. |
| ObjectiveItem | Objective text and completion state. |
| PauseManager | Pause UI and game pause state. |
| PlayerAudio | Footsteps and caught audio. |
| PlayerInteraction | Interaction targeting and input. |
| PlayerMovement | Movement, stamina and held flashlight. |
| PlayerRespawn | Applies saved checkpoint on retry. |
| RespawnPoint | Map checkpoint activation and saved spawn state. |
| RenatoEnemy | Level3 patrol, room searches, visual pursuit and solid CharacterController movement. |
| RenatoAnimation | Plays the supplied Renato Humanoid clips through the existing Animator. |
| RenatoAudio | Crossfades supplied patrol whistling/chase audio, ducks ambience and silences outcomes. |
| SceneBrightness | Saved exposure adjustment; attached in PoliceHQ and added by the menu controller in other scenes. |
| SceneTransition | Creates a persistent loading overlay and fades around asynchronous scene loading. |
| ScreenFadeController | Scene transition fade; created by EscapeManager when needed. |
| StationaryNervousIdle | Loops the frightened woman's animation in place and turns her toward the player. |

## Menu and scene presentation

| Script | Location / use |
| --- | --- |
| MainMenuController | `Assets/Scenes/Menu/`; menu, settings and scene-loading callbacks in MainMenu/OpeningCutscene. |
| TransitionManager | `Assets/Scenes/`; MainMenu splash/video transition. |
| TitleGreenGlowEffect | `Assets/Scenes/Text Effects/`; pulses the title image between green tints. |
| ButtonColorStateEffect | `Assets/Scenes/Text Effects/`; hover, press, float and flicker effects for button images. |
| VideoSplashManager | `Assets/Scenes/SplashScreen/`; prepares the splash video and fades its overlay after playback. |
| WakeUpBlinker | `Assets/Scenes/`; PoliceHQ wake-up animation and the UI prefab. |

## Editor and validation

- `Assets/Editor/FixAllSceneColliders.cs` is the **Tools → Fix All Scene Colliders** Editor command. It has no scene attachment because the menu invokes it.
- `Assets/Tests/Editor/GhostGameplayTests.cs` runs through Unity's Edit Mode Test Runner.
- `Assets/Tests/Editor/Level3GameplayTests.cs` checks Level3 references, navigation, collision, audio and outcomes through the same runner.
- The other Unity test scripts check checkpoint collection snapshots, corridor flicker and the Level 2 NPC quest and animation.
- `Tests/` contains Python checks for serialized assets and scene wiring.
- Run Python checks from the repository root with `python3 -m unittest discover -s Tests`.
- `Tools/build_white_lady_rig.py` is an asset-building tool, run with Python 3 and NumPy. It rebuilds `Assets/Resources/WhiteLadyRig.glb`; it does not run during gameplay.

## Bundled text examples

`Assets/Others/TextMesh Pro/Examples & Extras/Scripts/` contains the supplied text examples. Their comments explain counters, text animation, pointer selection, input validation and sample camera controls. Use their `Context` notes to identify them as bundled examples when choosing code for the presentation.

## Level 3 reuse and cleanup

Level3 retains the copied player, inventory/hotbar, flashlight, objectives, HUD and Lose Canvas. It reuses `GhostNavigation`, `ScreenFadeController` and `LoseSceneController`; Renato's scene-specific scripts supply physical movement and room searching without the earlier ghost's artifact/agimat rules. See [Level 3 configuration](../Docs/Level3.md) for assignments and pending Unity checks.

The earlier ghost, artifact, checkpoint and agimat scripts remain required by PoliceHQ or shared prefabs. Editor menu commands, tests and runtime-loaded Resources also remain in use. No gameplay script was removed during this audit.

The unused duplicate `ITE3 Cinco-Unsolved-Case.slnx` was removed. VS Code already selects `ITE3-Cinco-Unsolved-Case.slnx`, which includes gameplay, Editor and test projects.

## Removed scripts

`MenuBackgroundManager`, `FlashlightPickup`, `JumpscareTrigger`, `EscapeDoor` and `SaveActiveScene` had no serialized uses or executable callers. `InputBridge` was an empty compatibility shell; its sole PoliceHQ component was also removed. Their `.meta` files were removed with them. The active corner scares, Interactable pickup flow, escape manager and menu transition remain listed above.
