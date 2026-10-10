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
| ObjectiveItem | Existing text reference; `primaryObjective = Find the key, then escape through the main door.` |
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

Catch and escape share one outcome guard. Catch stops navigation/controls and shows the existing Lose Canvas. Escape stops navigation/controls, uses the existing unscaled `ScreenFadeController.FadeToBlack`, waits for black, then plays `Assets/Assets/ENDING CLIP/final ending.mp4` over that same black panel. The clip plays once with audio through MusicVolume, fits the screen without stretching, and automatically loads MainMenu on completion. The video uses unscaled time while gameplay remains frozen. Missing clips, decoder errors or startup timeouts show **“NAKALIGTAS KA. NAGSISIMULA NA ANG HUSTISYA.”** and a Main Menu button as a fallback. Escape requires collecting the existing scene key with E; the objective then points to the main door. No additional Lose Canvas, scene, EventSystem, or inventory is created.

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
6. Main-door trigger works while walking and sprinting; escape freezes Renato and the player, fades fully to black, plays the complete final ending with audio while gameplay stays frozen, and automatically returns to MainMenu with normal time restored. Check the movie at 16:9, 4:3 and ultrawide aspect ratios; also check the fallback Main Menu button with the clip temporarily unassigned. Catch and escape cannot both finish the run.
7. Asylum materials display correctly, including cutout/transparent props. Check Console for import/shader/missing-reference/NavMesh errors.

No original MainMenu, OpeningCutscene or PoliceHQ scene was edited. Shared ObjectiveItem and GhostNavigation additions default to their original behavior in other scenes. LoseSceneController and ScreenFadeController source are unchanged. PlayerMovement now has an optional stamina boost enabled only in Level 3.

## Night lighting and final-level balance

Level 3 retains its existing MoonBurst night sky. Flat, dim gray ambient light and moonlight replace the demo lighting contribution. The scene no longer uses the asset showcase's baked lighting data/settings, and its 40 demo reflection probes are disabled. All 96 existing asylum lamps use scene overrides for realtime lighting, 45% of their original intensity (capped at 0.65) and range capped at 3.5 m. This leaves pools of lamplight with dark gaps. The existing flashlight retains its intensity/range, with a narrower 50-degree beam and lower shadow normal bias. These overrides do not alter shared lights or global project settings. Visual calibration in Unity remains required.

Dwight now starts in the upstairs bedroom, farther from the main entrance. Level 3's existing movement component is configured for 3 m/s walking and 5.5 m/s sprinting; the existing 100 stamina / 25 per second drain gives four seconds of sprint, with 10 per second recovery. Renato patrols at 2.6 m/s, chases at 6 m/s, and sees within 16 m / 120 degrees with the existing solid-wall sight check. He starts in the central hall and visits the main entrance approach on patrol. Breaking sight and choosing when to move matters more than straight-line sprinting. Escape now requires the key; Renato still ignores sounds. These are initial tuning values, not a claim of playtested difficulty.

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

## Floor tiles and final ending follow-up

All 13 nested `Materials/Tiled/Tiles` materials now use URP Lit, preserving their albedo tiling, normal maps and metallic maps. They receive shadows, have no emission, and use the same muted gray tint and 0.2 smoothness as the repaired walls. This repairs the remaining missing legacy shader references in the tile set.

Level 3 now explicitly selects **Flat** ambient lighting (`m_AmbientMode: 3`); the checked-in scene had still selected Skybox at intensity 8, so the authored ambient gray was not being applied. Ambient gray is now `(0.055, 0.057, 0.06)`, with intensity 1 and environment reflections 0.1. This is intended to make nearby geometry readable with the flashlight off while keeping the night atmosphere. Level 3 still starts with its flashlight equipped; other scenes' pickup behavior is unchanged. Verify the gray balance and flashlight visibility in both Scene view (scene lighting enabled) and Play Mode at default brightness.

The final clip is assigned directly on Level3Flow so it is included in builds. The existing fade hides the HUD before playback, and the video/audio objects and render texture are released after playback or scene destruction. Run `python3 -m unittest discover -s Tests -v` for serialized regressions, then Unity EditMode tests (including the imported ending reference check). Unity compilation, playback/audio, menu transition and perceived lighting require Unity 6000.5.4f1; static validation alone does not verify them.

## Saved brightness across scenes

All four enabled build scenes (MainMenu, OpeningCutscene, PoliceHQ and Level3) have an enabled SceneBrightness component on a global volume visible to their camera. OpeningCutscene now enables camera post-processing. Level3 has a dedicated volume with a runtime exposure-only profile; its default slider value 5 preserves the authored night lighting. Each component reads PlayerPrefs in OnEnable, including when opening a gameplay scene directly, before a menu controller has run. The slider range 0–10 supplies an exposure offset of -2 to +2 stops relative to each scene's authored exposure; repeated updates do not accumulate or mutate shared profiles.

The menu applies settings on scene load without a scaled-time delay. Slider initialization uses SetValueWithoutNotify so loading settings cannot overwrite another saved value through a callback. Preferences are saved when closing Settings, starting gameplay or leaving the intro. Camera exposure affects rendered scene content; screen-space overlay UI and video retain their original colors.

Validation: the serialized lighting suite checks every enabled build scene for a linked active brightness component, global volume and camera post-processing/layer mask. Unity tests cover saved min/default/max values without a menu, timeScale zero and preserving unrelated grading. In Unity, set brightness to 0, 5 and 10, close Settings, start the game, move through all levels, retry and return to MainMenu; confirm scene brightness and the restored slider value. Also restart the application and open each gameplay scene directly. These runtime checks remain outstanding in the sandbox.


## Exit pressure and shared transition update

Renato now walks at 3.2 m/s with a 120-degree field of view. The main entrance approach is visited between each of the three hall stops, starting with the entrance. Room searches happen after six patrol stops and last two seconds, plus the existing crouch/stand animations. Sprint hearing and chase speed retain the current scene settings. Walls still block sight; the exit requires the scene key. Playtest the bedroom-to-door route to calibrate this tuning.

Successful escape fades to black, then fades in **“NAKALIGTAS KA. NAGSISIMULA NA ANG HUSTISYA.”** in the bundled Apache-licensed Special Elite typewriter font. The large white title is centered on two lines on a resolution-scaled canvas, holds fully visible for three unscaled seconds, then fades out. The final clip plays at **0.85×** speed and fades in after its first decoded frame; all fades use unscaled time while gameplay is frozen. Video failure uses the same title with the existing Main Menu fallback. Verify text legibility and wrapping at 16:9, 4:3 and ultrawide resolutions, clip speed/audio sync, and the transition timing in Unity Play Mode.

All scripted scene loads now use SceneTransition: a persistent input-blocking overlay fades out, asynchronously loads the destination, then fades in using unscaled time. Duplicate load requests are ignored. Menu play, intro completion, retry, pause exit, corridor escape and ending return share this path. Splash/menu fades also use unscaled time. Renato and ghost sight queries reuse bounded hit buffers (overflow blocks sight conservatively), and ghost chase paths refresh at most five times a second. These reduce known allocation/path costs; they are not measured frame-rate improvements. The synchronous runtime NavMesh bake still runs during scene initialization behind black and can delay scene activation.

Required Unity checks: run EditMode tests; exercise every enabled scene transition, including paused retry/exit and repeated clicks; confirm input unlocks and the new scene fades in; verify the success message precedes playback at timeScale zero and fallback still works. Profile CPU/GC and frame time in each gameplay scene on target hardware, including AI pursuit and Level 3's initial NavMesh bake. Unity is unavailable in the coding sandbox.


## Key requirement, stamina boost and caught view

The existing Key model at `(29.636, 4.708, -0.598)` has a Level3Key interactable. Its previous empty MeshCollider is replaced by a trigger BoxCollider fitted to child renderers in Awake. Aim at it within the existing 3 m interaction range and press E. Collection is stored on Level3Flow for the current run, removes the world key and updates the objective to “Escape through the main door.” Collection adds the Key ItemData to the existing inventory and displays the supplied Buttons-Icons/key icon.png sprite in the next free hotbar slot (slot 2 after the flashlight). The selected slot is preserved. If the inventory is full or its configuration is missing, collection leaves the world key available and the exit locked. Retry reloads the scene with the key uncollected and only the flashlight in the hotbar. Both the trigger and overlap exit paths use the same key guard.

Level 3 alone enables the stamina sprint boost: sprint speed is 5.0 m/s at stamina 45 or above and 5.5 m/s below 45. Speed follows the current stamina after that frame's drain/recovery. Walking remains 3 m/s; at zero stamina the existing exhaustion and 20% recovery requirement still apply. Other scenes retain their configured speeds.

Catch freezes controls and Renato, then snaps the existing player camera 0.65 m in front of Renato's Humanoid head, looking toward his face at 40 degrees FOV. A 0.03 m near plane avoids clipping the close view, and an angled spotlight illuminates his face. The existing LoseCanvas Background Image has alpha 0.5 so “CASE: UNSOLVED” is clearer while Renato’s illuminated face remains visible through the overlay; Retry and Exit remain interactive.

Validation: run `python3 -m unittest discover -s Tests -v` and Unity EditMode `Level3GameplayTests`. Added Unity checks cover key raycast geometry, the trigger/overlap gate, collection state, stamina 45/44.99 boundary, reachable key floor, and the caught view. Unity compilation and runtime checks remain outstanding without Unity 6000.5.4f1. In Play Mode, verify the key is visible/reachable at its existing placement, a full key-to-door escape, retry resetting collection, stamina drain/recovery/exhaustion, and Renato's face framing/lighting from front and rear catches. Tune camera distance and opacity visually if needed.
