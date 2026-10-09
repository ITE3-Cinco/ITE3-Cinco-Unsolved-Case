# Script guide

Game scripts are in `Assets/Scripts/`, with menu/cutscene components under `Assets/Scenes/`. The tables cover the scripts kept after checking serialized GUID references, code calls, runtime creation and Editor/test entry points.

## Gameplay (`Assets/Scripts/`)

| Script | Purpose / use |
| --- | --- |
| ArtifactManager | Tracks required artifacts; used by PoliceHQ and the item prefab. |
| ArtifactCornerScare | Both artifact-triggered corner encounters in PoliceHQ. |
| CollectibleGroup | Groups evidence and notifies the escape manager. |
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
| MouseLook | Player camera input. |
| NotificationText | Defines **NotificationManager**; shows pickup and checkpoint notifications. |
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
| ScreenFadeController | Scene transition fade; created by EscapeManager when needed. |

## Menu and scene presentation

| Script | Location / use |
| --- | --- |
| MainMenuController | `Assets/Scenes/Menu/`; menu, settings and scene-loading callbacks in MainMenu/OpeningCutscene. |
| TransitionManager | `Assets/Scenes/`; MainMenu splash/video transition. |
| TitleGreenGlowEffect | `Assets/Scenes/Menu/Text Effects/`; title effect in MainMenu/OpeningCutscene. |
| WakeUpBlinker | `Assets/Scenes/`; PoliceHQ wake-up animation and the UI prefab. |

## Editor and validation

- `Assets/Editor/FixAllSceneColliders.cs` is the **Tools → Fix All Scene Colliders** Editor command. It has no scene attachment because the menu invokes it.
- `Assets/Tests/Editor/GhostGameplayTests.cs` runs through Unity's Edit Mode Test Runner.
- `Assets/Tests/Editor/Level3GameplayTests.cs` checks Level3 references, navigation, collision, audio and outcomes through the same runner.
- `Tests/` contains Python checks for serialized assets and scene wiring.

## Level 3 reuse and cleanup

Level3 retains the copied player, inventory/hotbar, flashlight, objectives, HUD and Lose Canvas. It reuses `GhostNavigation`, `ScreenFadeController` and `LoseSceneController`; Renato's scene-specific scripts supply physical movement and room searching without the earlier ghost's artifact/agimat rules. See [Level 3 configuration](../Docs/Level3.md) for assignments and pending Unity checks.

The earlier ghost, artifact, checkpoint and agimat scripts remain required by PoliceHQ or shared prefabs. Editor menu commands, tests and runtime-loaded Resources also remain in use. No gameplay script was removed during this audit.

The unused duplicate `ITE3 Cinco-Unsolved-Case.slnx` was removed. VS Code already selects `ITE3-Cinco-Unsolved-Case.slnx`, which includes gameplay, Editor and test projects.

## Removed scripts

`MenuBackgroundManager`, `FlashlightPickup`, `JumpscareTrigger`, `EscapeDoor` and `SaveActiveScene` had no serialized uses or executable callers. `InputBridge` was an empty compatibility shell; its sole PoliceHQ component was also removed. Their `.meta` files were removed with them. The active corner scares, Interactable pickup flow, escape manager and menu transition remain listed above.
