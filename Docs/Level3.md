# Level 3 asylum configuration

## Existing assets reused

- Scene: `Assets/Scenes/Level3.unity` (retains its GUID/name; no replacement scene).
- Environment: existing `Assets/Abandoned_Asylum/Prefabs/Asylum.prefab` instance. Its 925 mesh colliders and readable `Models/Asylum.fbx` supply runtime navigation geometry.
- Renato: the scene's existing `RenatoEnemy` model instance, sourced from `Assets/Assets/ENEMY-ANIM/char_david/Ch28_nonPBR.fbx`. Its imported Humanoid Animator, avatar and body/hair materials are retained under a navigation parent. No duplicate character is instantiated.
- Animation: existing `RenatoWalk`, `RenatoChase`, `Looking Around`, and `Crouch To Stand` FBX files. The original FBX content and GUIDs are unchanged. Import settings bake root motion into the pose and loop locomotion/look clips.
- Player, camera, audio, interaction, hotbar, HUD, objective controller, EventSystem and Lose Canvas are reused. The extra scene camera/listener is disabled. The flashlight light follows the existing player camera; the existing item starts in slot 1 and uses the existing F-key behavior. The redundant world pickup and artifact counter/icon are inactive.

## Scene assignments

| Component | Assignments/configuration |
| --- | --- |
| Player | Upstairs bedroom spawn `(-21, 4.75, -28)`; existing CharacterController radius 0.3, height 1.8; existing PlayerRespawn uses Dwight Spawn marker |
| ObjectiveItem | Existing text reference; `primaryObjective = Escape the asylum.` |
| Inventory | Existing flashlight ItemData in first slot; existing HotbarUI and all five icon/background pairs |
| LoseSceneController | Existing LoseCanvas, Retry/Exit buttons; Retry `Level3`, Exit `MainMenu` |
| Level 3 Flow | Existing player, lose controller, PauseManager/PauseButton, Renato, main exit, ScreenFadeController and existing TMP font |
| GhostNavigation | Existing asylum root only; voxel 0.1, radius 0.3, height 1.8. Other scenes keep their project defaults. Bake happens before agent startup. |
| Renato Navigation | NavMeshAgent and solid CharacterController at ground-level feet; existing model/Animator as child; RenatoEnemy and RenatoAnimation |
| RenatoAnimation | Existing Humanoid Animator and four imported Mixamo clips; no replacement Animator Controller |
| RenatoEnemy | Four hall patrol points; Office A, Guard Room, Stock and Kitchen A entrance/search markers; player and flow references |
| Main Door Escape Trigger | Trigger box and kinematic Rigidbody inside the existing main entrance, centered `(0, 1, -12.7)`; Level3Exit routes only the copied player to flow |
| Build Settings | Existing entries preserved; Level3 appended for Retry/build loading |

The bedroom’s right door leaf and glass plus the four search-room door leaves are opened wider as scene overrides. Existing rooms, halls, stairs and furniture remain. Walls and furniture provide sight occlusion; no guaranteed-detection hiding/search system is added.

## Behavior

Renato follows complete NavMesh paths. After two patrol stops he chooses a reachable search room, travels to its interior marker, crouches, looks around, stands, then returns through its entrance to patrol. Search animations start only on arrival. There is no separate crouch-down or idle asset in the Renato folder: the existing stand clip is sampled backwards for crouching and its final standing pose provides idle. The look clip is masked to the upper body to retain crouched legs. Navigation drives the root, and seeing Dwight interrupts every search phase immediately.

Detection uses range, field of view, and solid-collider line of sight. Sound is never queried. Losing sight preserves the last **seen** position, investigates it, then searches a reachable room or resumes patrol. The AI never updates the hidden player's location by sound or through a wall. Contact also requires a clear sight ray.

Catch and escape share one outcome guard. Catch stops navigation/controls and shows the existing Lose Canvas. Escape stops navigation/controls, uses the existing unscaled `ScreenFadeController.FadeToBlack`, waits for black, then uses that same black panel for **“You escaped with the truth...”** and a Main Menu button. No evidence/inventory/objective condition gates escape. No additional Lose Canvas, scene, EventSystem, inventory, or cinematic is created.

## Material repair

139 asylum materials referenced absent shader GUID `4e90a8289da1f3943885176e4f83e35c`. They now use the URP Lit shader already used by five asylum materials, retaining existing textures and mapping base color, smoothness, normal/metallic/emission keywords and cutout/transparent modes. These materials have no direct references from the other game scenes/prefabs. A dangling unused legacy specular-map reference in Tap was cleared; its existing metallic map remains. Visual appearance still requires Unity verification.

## Validation and remaining editor checks

Static scene/reference checks and C# syntax checks passed in the coding sandbox. **This is not a Unity compilation or Play Mode pass.** Unity Editor 6000.5.4f1 is unavailable in the sandbox.

With that editor and all Git LFS assets retrieved (`git lfs pull`), open the project and run the EditMode tests, including `Level3GameplayTests` and the existing `GhostGameplayTests`. The Level 3 tests check imported Animator/clip references, copied UI references, actual runtime NavMesh connectivity from the spawn to every patrol/search marker and exit, wall occlusion, and catch locking. Do not treat the layout as verified until the connectivity test passes.

In Play Mode, verify:

1. Spawn clearance and navigation from the upstairs bedroom through its doorway and down the stairs, all four search rooms, and the main-door vestibule; check furniture and alternate hall/stair routes.
2. One player view/audio listener; existing HUD and objective; flashlight in slot 1, F toggles, switching slots hides it.
3. Renato's foot alignment, scale, walk/run and crouch/look/stand blend; search starts inside rooms, can miss Dwight, and is interrupted by sight.
4. Walls block detection; moving unseen does not update last-known position; footsteps do not attract Renato.
5. Catch opens the existing Lose Canvas once. Pause/hotbar/movement/look cannot resume gameplay behind it. Retry reloads Level3; Exit loads MainMenu.
6. Main-door trigger works while walking and sprinting; escape freezes Renato and the player, fades fully to black, shows the exact ending text, and Main Menu works. Catch and escape cannot both finish the run.
7. Asylum materials display correctly, including cutout/transparent props. Check Console for import/shader/missing-reference/NavMesh errors.

No original MainMenu, OpeningCutscene or PoliceHQ scene was edited. Shared ObjectiveItem and GhostNavigation additions default to their original behavior in other scenes. LoseSceneController, player controllers and ScreenFadeController source are unchanged.

## Night lighting and final-level balance

Level 3 retains its existing MoonBurst night sky. Flat, dim gray ambient light and moonlight replace the demo lighting contribution. The scene no longer uses the asset showcase's baked lighting data/settings, and its 40 demo reflection probes are disabled. All 96 existing asylum lamps use scene overrides for realtime lighting, 45% of their original intensity (capped at 0.65) and range capped at 3.5 m. This leaves pools of lamplight with dark gaps. The existing flashlight retains its intensity/range, with a narrower 50-degree beam and lower shadow normal bias. These overrides do not alter shared lights or global project settings. Visual calibration in Unity remains required.

Dwight now starts in the upstairs bedroom, farther from the main entrance. Level 3's existing movement component is configured for 3 m/s walking and 5.5 m/s sprinting; the existing 100 stamina / 25 per second drain gives four seconds of sprint, with 10 per second recovery. Renato patrols at 2.6 m/s, chases at 6 m/s, and sees within 16 m / 120 degrees with the existing solid-wall sight check. He starts in the central hall and visits the main entrance approach on patrol. Breaking sight and choosing when to move matters more than straight-line sprinting. Escape still has no collectible gate, and Renato still ignores sounds. These are initial tuning values, not a claim of playtested difficulty.

Renato's trigger capsule is replaced by a solid CharacterController (radius 0.3, height 1.8). The NavMesh supplies desired velocity; CharacterController.Move resolves actual displacement against colliders. Automatic agent position updates are disabled, and its simulation is synchronized to the position allowed by physics without warping through obstacles. Gravity and a 0.3 m step offset support floors/stairs. A blocked patrol retries after 25 seconds. Added editor tests attempt to move Renato through a solid wall at ordinary and long frame durations, then verify that a trigger remains passable.

Additional Play Mode checks: compare flashlight off/on in corridors and rooms at the default brightness; check lamp shadows and dark gaps; test Renato against wall corners, door frames, furniture and stairs; confirm controller/agent synchronization and foot alignment; then play the full bedroom-to-exit route to tune difficulty without unavoidable spawn catches.

## Renato audio and animation follow-up

The supplied `Assets/Audio/Renato Audio/RenatoWhistling.mp3` and `ChaseAudio.mp3` are now assigned to a scene-local RenatoAudio component with two distinct sources. Whistling is a looping 3D cue (2–18 m), routed through the existing SFXVolume mixer group. Pursuit uses looping 2D chase music through MusicVolume. These crossfade over two seconds when AI enters/leaves Chase. After sight is lost, the AI's existing two-second pursuit memory expires before the music fades; simply going behind cover for one frame does not abruptly silence the cue. Whistling returns during patrol/search/investigation. Audio never drives enemy detection.

The existing LVL3BGM source is retained at volume 0.2, ducked to 20% of that level during pursuit, then restored. Pause suspends all three sources; resume continues them. Catch/escape fades all three to silence with unscaled time, including while the outcome has frozen gameplay. No replacement audio is imported.

Animation uses the existing Humanoid model/avatar and walk/run/look/stand clips described above. RenatoAnimation now runs after physics movement and reads the solid CharacterController's horizontal velocity, so an obstructed character blends to standing rather than animating according to NavMesh simulation alone. Root motion remains disabled.

Static references/syntax can be checked in the sandbox, but Unity audio playback, imported clip/avatar compatibility, exact foot alignment, looping/blending quality and perceived mix still require Play Mode. The added EditMode mix test covers patrol-to-chase crossfade, investigate fade-back, mixer routes, and outcome fading at timeScale zero. Also verify pause/resume, rapidly losing/reacquiring sight, music/SFX settings, spatial distance falloff and audio silence on the ending screen.

## Bedroom spawn, caught audio and wall balance

Level 3 assigns the existing `Player/player-scream.mp3` to PlayerAudio’s caught clip. The catch flow still plays it once before showing the Lose Canvas.

Dwight and the fallback spawn marker are both at `(-21, 4.75, -28)`, in the upstairs bedroom’s central aisle, facing its doorway. The right door leaf and its glass are opened 90 degrees together around the outer hinge as scene overrides. The route to the ground-floor main exit now includes the upstairs hall and stairs. Imported geometry places the bedroom floor at about Y=3.82; the existing centered 1.8 m controller starts just above it. Runtime clearance and stair connectivity still require the existing NavMesh test in Unity.

The six tiled wall materials now use URP Lit with their existing albedo, normal and metallic textures, a muted gray tint `(0.5, 0.51, 0.52)` and smoothness 0.2. These nested materials still referenced the missing legacy shader after the earlier material repair. Flat ambient color is lifted to `(0.025, 0.027, 0.03)` to soften black gaps toward dark gray; lamp, moon and flashlight strengths are unchanged. Check the final perceived balance with the flashlight both off and on in Unity.
