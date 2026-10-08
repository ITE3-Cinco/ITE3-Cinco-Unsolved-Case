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
| GhostNavigation | Builds the PoliceHQ navigation surface. |
| GhostAnimation | Builds the ghost rig and plays idle, walk and scream; also created for apparitions. |
| GhostAudio | Ghost humming and scare audio. |
| HotbarUI | Displays inventory slots and agimat warning. |
| Interactable | Item pickups, artifact progress and map interactions. |
| Inventory | Item storage, selection and consumption. |
| ItemData | ScriptableObject type used by the inventory assets. |
| InvisibilityConsumable | Agimat protection and nearby-ghost warning. |
| LoseSceneController | Loss UI, retry and return to menu. |
| MouseLook | Player camera input. |
| NotificationText | Defines **NotificationManager**; shows pickup and checkpoint notifications. |
| ObjectiveItem | Objective text and completion state. |
| PauseManager | Pause UI and game pause state. |
| PlayerAudio | Footsteps and caught audio. |
| PlayerInteraction | Interaction targeting and input. |
| PlayerMovement | Movement, stamina and held flashlight. |
| PlayerRespawn | Applies saved checkpoint on retry. |
| RespawnPoint | Map checkpoint activation and saved spawn state. |
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
- `Tests/` contains Python checks for serialized assets and scene wiring.

## Removed scripts

`MenuBackgroundManager`, `FlashlightPickup`, `JumpscareTrigger`, `EscapeDoor` and `SaveActiveScene` had no serialized uses or executable callers. `InputBridge` was an empty compatibility shell; its sole PoliceHQ component was also removed. Their `.meta` files were removed with them. The active corner scares, Interactable pickup flow, escape manager and menu transition remain listed above.
