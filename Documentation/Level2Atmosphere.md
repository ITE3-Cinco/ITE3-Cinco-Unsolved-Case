# Level Two atmosphere and agimat

Level Two uses dim blue ambient light, subdued reflections and five weak real-time corridor spotlights near the starting passage, north/south passages, central passage and east room. The existing flashlight stays at intensity 200, and a global SceneBrightness volume applies the saved brightness setting relative to the authored lighting.

The player starts with one flashlight in the hotbar. `PlayerMovement.flashlight` points to the scene-added camera light; the inherited flashlight Light component is disabled by a Level Two prefab override. This prevents an uncontrolled beam from staying on when **F** switches the carried flashlight off. Selecting another slot hides the controlled beam; selecting the flashlight again restores its saved on/off state. The inherited world pickup remains inactive in Level Two.

The imported map originally used unlit materials with brightness baked into its textures. Its wall, floor and trim materials now use lit shading, retaining their original textures and mesh/hierarchy data. The fluorescent panel material stays unlit so the panels remain visible in the dark. This map is referenced only by Level Two.

CorridorLightFlicker dims the five corridor lights and the imported panel material together. Every 2.5–6 seconds it produces a short burst of 2–5 dips, then returns to normal. Panel brightness is reduced to 35% even between bursts. Flicker uses scaled time and freezes during pause/loading; disabling the controller restores the authored intensities and original renderer property blocks. It does not modify shared materials.

Exactly one agimat sits on the floor of the north passage at **(-89.8, -1.69, 170.5)**. It reuses Level One's pickup mesh/material, Item_Consumable, Interactable and shared player defense. Aim and press **E** to collect it; select it or approach the hostile White Lady and press **E** to consume it for five seconds of protection and ghost retreat. The existing 16-unit gold hotbar warning, full-inventory guard and repeated-use protection still apply. It is not a Note and cannot advance the quest. Retry recreates it with the scene; a fall respawn preserves the existing inventory state.

## Verification

```sh
python3 -m unittest discover -s Tests -v
```

In Unity 6000.5.4f1, run Edit Mode CorridorLightFlickerTests and the existing Level2NpcTests / GhostGameplayTests, then verify in Play Mode:

1. Import/compile; verify the flashlight starts off, **F** toggles a single beam, and switching to an empty slot hides it. Switch back and check the saved on/off state, then pause/resume and retry. Walk all three Note routes with flashlight on/off and check brightness slider extremes. Check lamp placement, wall shadows, visibility of the NPC/Notes/agimat, and performance.
2. Watch multiple bursts: panel glow and corridor illumination should dip together, with the flashlight remaining steady. Pause during a dip, resume, and retry.
3. Collect the single agimat. Start the hunt, approach the ghost, check the gold warning, consume with E, and confirm one item removed, retreat and five seconds of immunity. Repeated E must not consume again.
4. Collect all three Notes with the agimat carried. Return to the NPC; confirm story completion stays safe, dialogue works and Level3 loads. Retry restores the agimat.

Unity Editor is unavailable in this cloud sandbox; visual tuning and gameplay/import validation require these editor checks.
