# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Level Two's lighting, single flashlight beam and agimat pickup wiring."""
# Import json for encoding and decoding glTF/GLB JSON metadata.
import json
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Reuse documents, ROOT from Tests/test_level2_ghost.py; documents loads Unity YAML into a fileID lookup.
from test_level2_ghost import documents, ROOT
# Reuse field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import field, reference


class Level2AtmosphereTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Scenes/Lvl-2.unity' by
        # test_level2_ghost.documents in cls.scene.
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Assets/Prefabs/===PLAYER===.prefab'
        # by test_level2_ghost.documents in cls.player.
        cls.player = documents(ROOT / 'Assets/Assets/Prefabs/===PLAYER===.prefab')

    # Check that map surfaces respond to lighting and panels remain visible.
    def test_map_surfaces_respond_to_lighting_and_panels_remain_visible(self):
        # Store metadata decoded from JSON in text read from file ROOT / 'Assets/Assets/free
        # assets/backrooms_another_level/scene.gltf' in model.
        model = json.loads((ROOT / 'Assets/Assets/free assets/backrooms_another_level/scene.gltf').read_text())
        # Process each material from model['materials'] (read from model).
        for material in model['materials']:
            # Label this asset/object combination as a separate unittest subtest so a failure identifies the exact
            # fixture.
            with self.subTest(material=material['name']):
                # Store 'KHR_materials_unlit' appears in dictionary value for key 'extensions' in material
                # (default an empty dictionary) in unlit.
                unlit = 'KHR_materials_unlit' in material.get('extensions', {})
                # Expect unlit to equal material['name'] (read from material) equals 'light'.
                self.assertEqual(unlit, material['name'] == 'light')
        # Store self.scene['2'][1] (read from self.scene['2']) in settings.
        settings = self.scene['2'][1]
        # Expect serialized field 'm_AmbientMode' read from settings by test_collision_assets.field to equal '1'.
        self.assertEqual(field(settings, 'm_AmbientMode'), '1')
        # Expect fileID referenced by field 'm_SkyboxMaterial' in settings via test_collision_assets.reference to
        # equal '0'.
        self.assertEqual(reference(settings, 'm_SkyboxMaterial'), '0')
        # Expect serialized field 'm_ReflectionIntensity' read from settings by test_collision_assets.field
        # converted to a number with decimals to be at most 0.1.
        self.assertLessEqual(float(field(settings, 'm_ReflectionIntensity')), .1)
        # Process each name from ['m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor'].
        for name in ['m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor']:
            # Store list of float(v) using v from all regex matches extracted from serialized field name read from
            # settings by test_collision_assets.field using the pattern written here in rgb.
            rgb = [float(v) for v in re.findall(r'[rgb]: ([\d.]+)', field(settings, name))]
            # Expect length of rgb to equal 3.
            self.assertEqual(len(rgb), 3)
            # Expect largest of rgb to be at most 0.065.
            self.assertLessEqual(max(rgb), .065)

    # Check that saved brightness has a global volume and enabled camera.
    def test_saved_brightness_has_a_global_volume_and_enabled_camera(self):
        # Store self.scene['226003003'][1] (read from self.scene['226003003']) in volume.
        volume = self.scene['226003003'][1]
        # Store self.scene['226003004'][1] (read from self.scene['226003004']) in brightness.
        brightness = self.scene['226003004'][1]
        # Expect fileID referenced by field 'm_GameObject' in volume via test_collision_assets.reference to equal
        # fileID referenced by field 'm_GameObject' in brightness via test_collision_assets.reference.
        self.assertEqual(reference(volume, 'm_GameObject'), reference(brightness, 'm_GameObject'))
        # Expect serialized field 'm_IsGlobal' read from volume by test_collision_assets.field to equal '1'.
        self.assertEqual(field(volume, 'm_IsGlobal'), '1')
        # Expect serialized field 'weight' read from volume by test_collision_assets.field to equal '1'.
        self.assertEqual(field(volume, 'weight'), '1')
        # Expect serialized field 'm_Enabled' read from brightness by test_collision_assets.field to equal '1'.
        self.assertEqual(field(brightness, 'm_Enabled'), '1')
        # Store self.player['4159511427452579016'][1] (read from self.player['4159511427452579016']) in camera.
        camera = self.player['4159511427452579016'][1]
        # Expect serialized field 'm_RenderPostProcessing' read from camera by test_collision_assets.field to
        # equal '1'.
        self.assertEqual(field(camera, 'm_RenderPostProcessing'), '1')
        # Expect 'm_Bits: 1' to appear in camera.
        self.assertIn('m_Bits: 1', camera)

    # Check that flicker is wired to only corridor lights.
    def test_flicker_is_wired_to_only_corridor_lights(self):
        # Store self.scene['226003002'][1] (read from self.scene['226003002']) in controller.
        controller = self.scene['226003002'][1]
        # Expect serialized field 'm_Enabled' read from controller by test_collision_assets.field to equal '1'.
        self.assertEqual(field(controller, 'm_Enabled'), '1')
        # Expect fileID referenced by field 'environmentRoot' in controller via test_collision_assets.reference to
        # equal '8800000000000000033'.
        self.assertEqual(reference(controller, 'environmentRoot'), '8800000000000000033')
        # Store re.search('guid: (\\w+)', (ROOT / 'Assets/Scripts/CorridorLightFlicker.cs.meta').read_text())[1]
        # (read from re.search('guid: (\\w+)', (ROOT /
        # 'Assets/Scripts/CorridorLightFlicker.cs.meta').read_text())) in guid.
        guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Scripts/CorridorLightFlicker.cs.meta').read_text())[1]
        # Expect guid to appear in serialized field 'm_Script' read from controller by
        # test_collision_assets.field.
        self.assertIn(guid, field(controller, 'm_Script'))
        # Store all regex matches extracted from controller.split('  lights:\n')[1].split('  burstInterval:')[0]
        # (read from controller.split('  lights:\n')[1].split('  burstInterval:')) using the pattern written here
        # in targets.
        targets = re.findall(r'fileID: (\d+)', controller.split('  lights:\n')[1].split('  burstInterval:')[0])
        # Expect length of targets to equal 5.
        self.assertEqual(len(targets), 5)
        # Expect '1948734481' to be absent from targets.
        self.assertNotIn('1948734481', targets)
        # Process each target from targets.
        for target in targets:
            # Store self.scene[target] (read from self.scene) in (kind, light).
            kind, light = self.scene[target]
            # Expect kind to equal 108.
            self.assertEqual(kind, 108)
            # Expect serialized field 'm_Enabled' read from light by test_collision_assets.field to equal '1'.
            self.assertEqual(field(light, 'm_Enabled'), '1')
            # Expect serialized field 'm_Lightmapping' read from light by test_collision_assets.field to equal
            # '4'.
            self.assertEqual(field(light, 'm_Lightmapping'), '4')
            # Expect serialized field 'm_Intensity' read from light by test_collision_assets.field converted to a
            # number with decimals to be at most 4.
            self.assertLessEqual(float(field(light, 'm_Intensity')), 4)
            # Store fileID referenced by field 'm_GameObject' in light via test_collision_assets.reference in go.
            go = reference(light, 'm_GameObject')
            # Expect 'component: {fileID: ' + target + '}' (calculated from target) to appear in self.scene[go][1]
            # (read from self.scene[go]).
            self.assertIn('component: {fileID: ' + target + '}', self.scene[go][1])
        # Expect serialized field 'm_Intensity' read from self.scene['1948734481'][1] (read from
        # self.scene['1948734481']) by test_collision_assets.field to equal '200'.
        self.assertEqual(field(self.scene['1948734481'][1], 'm_Intensity'), '200')

    # The player starts with one flashlight; its scene light must be the only enabled player beam.
    def test_player_has_one_controlled_flashlight_beam(self):
        # Read the overrides on Level Two's shared player prefab instance.
        instance = self.scene['1487628291'][1]
        # Map each source component/property pair to its scene value and object reference.
        overrides = {(target, prop.strip("'")): (value.strip(), ref) for target, prop, value, ref in
                     re.findall(r'- target: \{fileID: (\d+),[^\n]*\n'
                                r'      propertyPath: ([^\n]+)\n'
                                r'      value:([^\n]*)\n'
                                r'      objectReference: (\{[^\n]+\})', instance)}
        # Locate PlayerMovement in the source prefab rather than relying on its serialized component ID.
        movement_id, movement = next((key, body) for key, (kind, body) in self.player.items()
                                     if kind == 114 and 'Assembly-CSharp::PlayerMovement' in body)
        # Resolve the Light assigned to PlayerMovement after applying the scene override.
        light_ref = overrides[(movement_id, 'flashlight')][1]
        # Extract the scene light ID so we can compare it with all effective player lights below.
        controlled_id = re.search(r'fileID: (\d+)', light_ref)[1]
        # Level Two grants ownership before any pickup; a second beam must not act as a free flashlight.
        self.assertEqual(overrides[(movement_id, 'hasFlashlight')][0], '1')
        # Ensure the starting hotbar has exactly one entry containing the same flashlight asset as the player.
        inventory_id = next(key for key, (kind, body) in self.player.items()
                            if kind == 114 and 'Assembly-CSharp::Inventory' in body)
        # A one-entry inventory leaves four slots for the three Notes and agimat.
        self.assertEqual(overrides[(inventory_id, 'items.Array.size')][0], '1')
        # The carried item and the toggled light must belong to the same flashlight definition.
        self.assertEqual(overrides[(inventory_id, 'items.Array.data[0]')][1],
                         field(movement, 'flashlightItemData'))
        # Gather enabled lights inherited from the player prefab, applying scene component overrides.
        enabled = [key for key, (kind, body) in self.player.items() if kind == 108 and
                   overrides.get((key, 'm_Enabled'), (field(body, 'm_Enabled'), None))[0] == '1']
        # Identify scene-added objects whose parent is a Transform from this player instance.
        player_parents = {key for key, (kind, body) in self.scene.items() if kind == 4 and
                          reference(body, 'm_PrefabInstance') == '1487628291'}
        # Gather their owning GameObject IDs so corridor lights are excluded from the beam check.
        added_objects = {reference(body, 'm_GameObject') for kind, body in self.scene.values()
                         if kind == 4 and reference(body, 'm_Father') in player_parents}
        # Include each enabled scene-added player light in the effective beam list.
        enabled.extend(key for key, (kind, body) in self.scene.items() if kind == 108 and
                       reference(body, 'm_GameObject') in added_objects and field(body, 'm_Enabled') == '1')
        # Before the fix, the inherited light and scene light both appeared here; only the controlled one should.
        self.assertEqual(enabled, [controlled_id])

    # Check that exactly one pickup uses level one defense and fits inventory.
    def test_exactly_one_pickup_uses_level_one_defense_and_fits_inventory(self):
        # Store list of b using (kind, b) from values stored in self.scene filtered by kind == 114 and 'guid:
        # cc2e3a7d10b2fe240a5e74494f52a35d' in b in pickups.
        pickups = [b for kind, b in self.scene.values() if kind == 114 and
                   'guid: cc2e3a7d10b2fe240a5e74494f52a35d' in b]
        # Expect length of pickups to equal 1.
        self.assertEqual(len(pickups), 1)
        # Store pickups[0] (read from pickups) in pickup.
        pickup = pickups[0]
        # Expect serialized field 'isArtifact' read from pickup by test_collision_assets.field to equal '0'.
        self.assertEqual(field(pickup, 'isArtifact'), '0')
        # Expect serialized field 'isCheckpoint' read from pickup by test_collision_assets.field to equal '0'.
        self.assertEqual(field(pickup, 'isCheckpoint'), '0')
        # Expect serialized field 'promptMessage' read from pickup by test_collision_assets.field to equal 'Press
        # [E] to pick up Agimat.'.
        self.assertEqual(field(pickup, 'promptMessage'), 'Press [E] to pick up Agimat.')
        # Store fileID referenced by field 'm_GameObject' in pickup via test_collision_assets.reference in go.
        go = reference(pickup, 'm_GameObject')
        # Store list of b using (k, b) from values stored in self.scene filtered by k == 136 and reference(b,
        # 'm_GameObject') == go in colliders.
        colliders = [b for k, b in self.scene.values() if k == 136 and reference(b, 'm_GameObject') == go]
        # Expect length of colliders to equal 1.
        self.assertEqual(len(colliders), 1)
        # Expect serialized field 'm_Enabled' read from colliders[0] (read from colliders) by
        # test_collision_assets.field to equal '1'.
        self.assertEqual(field(colliders[0], 'm_Enabled'), '1')
        # Expect '226001001' to appear in self.scene['9223372036854775807'][1] (read from
        # self.scene['9223372036854775807']).
        self.assertIn('226001001', self.scene['9223372036854775807'][1])
        # Store self.player['8969556251856540235'][1] (read from self.player['8969556251856540235']) in
        # consumable.
        consumable = self.player['8969556251856540235'][1]
        # Expect serialized field 'duration' read from consumable by test_collision_assets.field to equal '5'.
        self.assertEqual(field(consumable, 'duration'), '5')
        # Expect serialized field 'warningDistance' read from consumable by test_collision_assets.field to equal
        # '16'.
        self.assertEqual(field(consumable, 'warningDistance'), '16')
        # Three Notes, one agimat and one flashlight all fit without blocking progress.
        # Expect serialized field 'maxSlots' read from self.player['5252281844144999244'][1] (read from
        # self.player['5252281844144999244']) by test_collision_assets.field converted to an integer to be at
        # least 5.
        self.assertGreaterEqual(int(field(self.player['5252281844144999244'][1], 'maxSlots')), 5)
        # Expect 'isAgimat: 1' to appear in text read from file ROOT /
        # 'Assets/Assets/Inventory/Item_Consumable.asset'.
        self.assertIn('isAgimat: 1', (ROOT / 'Assets/Assets/Inventory/Item_Consumable.asset').read_text())
        # No scene overrides may disable shared defense or change its duration.
        # Store self.scene['1487628291'][1] (read from self.scene['1487628291']) in player.
        player = self.scene['1487628291'][1]
        # Expect player not to match the regex written here.
        self.assertNotRegex(player, r'target: \{fileID: 8969556251856540235[^\n]*\n      propertyPath: (?:duration|m_Enabled)')


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
