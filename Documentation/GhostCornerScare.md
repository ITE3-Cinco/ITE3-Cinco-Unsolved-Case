# Second-artifact White Lady encounter

PoliceHQ has **=== ENEMY === > Artifact 2 Corner Scare**, with editable **Corner Spawn** and **Wall Exit** children. A second encounter is described in [Escape and agimat](EscapeAndAgimat.md).

## Behaviour

1. Collecting the second of the three required artifacts arms the encounter. Flashlights, consumables, and map checkpoints do not count. Duplicate collection notifications do not increment progress.
2. It waits until the player is within 12 metres and the corner appears within the middle 80% of the camera view, with no solid collider obstructing the sight line. The event stays armed if the player looks away or collects artifact three before reaching the corner.
3. A visual copy of the White Lady appears facing the player, plays WhiteLady-Scream, and holds for 1.2 seconds.
4. It moves at 2.5 units/second to the exit point beyond the wall, then disappears. This copy has no active patrol, catch, navigation, or collision behaviour. The original ghost keeps patrolling/chasing normally.
5. It plays once per level run. Retry reloads the level and resets artifact progress and the encounter. Pausing freezes the visual sequence; disabling its component removes the apparition.

The default corner is in the central corridor beside the interview room: spawn **(-92, 2, 83)**, exit **(-97, 2, 83)**. The route crosses `ARCH_Wall_Plaster.054`. These coordinates refer to the current PoliceHQ layout.

The White Lady now has a locally generated humanoid rig and animation driver. See [Navigation and animation](GhostNavigationAndAnimation.md) for the walking setup, missing GhostScream clip, and Unity validation steps. Root motion stays disabled so the apparition route controls movement.

## Inspector controls

Select **Artifact 2 Corner Scare** to change the required artifact count, activation distance, hold duration, exit speed, visibility mask, sound, and volume. Move its two child transforms to change the corner and route. Keep Corner Spawn at the model's center height and outside solid geometry, and put Wall Exit far enough behind the wall to hide the entire model. Gizmos display the route and activation radius.

## Verification

Serialized checks:

```sh
python3 -m unittest discover -s Tests -p 'test_ghost_corner_scare.py' -v
```

Unity Play Mode checks (Editor unavailable in the sandbox):

- Confirm scripts import and compile. At 0/3 and 1/3, approach and look at the corner: no apparition.
- Collect two distinct required artifacts, then approach the corner. Looking away or standing behind a wall should keep the scare pending. Looking at the unobstructed corner should play it once.
- Check the appearance, model orientation, sound, pause/resume, hold, and exit through the wall. Verify there is no invisible blocker or catch event from the apparition. The original ghost can still catch the player normally.
- Collect the third artifact before approaching the corner in a fresh run; the pending encounter should still play. Revisit afterward: it should not repeat.
- Disable the scare component during its hold or exit: the visual should be removed. Reload through Try Again and repeat the 2/3 flow.
- Check the chosen corner from both corridor approaches. Adjust the two scene handles if needed after viewing the imported model in Unity.
