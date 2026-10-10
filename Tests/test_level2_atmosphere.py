"""Level Two's lighting and single agimat pickup wiring."""
import json
import re
import unittest
from test_level2_ghost import documents, ROOT
from test_collision_assets import field, reference


class Level2AtmosphereTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')
        cls.player = documents(ROOT / 'Assets/Assets/Prefabs/===PLAYER===.prefab')

    def test_map_surfaces_respond_to_lighting_and_panels_remain_visible(self):
        model = json.loads((ROOT / 'Assets/Assets/free assets/backrooms_another_level/scene.gltf').read_text())
        for material in model['materials']:
            with self.subTest(material=material['name']):
                unlit = 'KHR_materials_unlit' in material.get('extensions', {})
                self.assertEqual(unlit, material['name'] == 'light')
        settings = self.scene['2'][1]
        self.assertEqual(field(settings, 'm_AmbientMode'), '1')
        self.assertEqual(reference(settings, 'm_SkyboxMaterial'), '0')
        self.assertLessEqual(float(field(settings, 'm_ReflectionIntensity')), .1)
        for name in ['m_AmbientSkyColor', 'm_AmbientEquatorColor', 'm_AmbientGroundColor']:
            rgb = [float(v) for v in re.findall(r'[rgb]: ([\d.]+)', field(settings, name))]
            self.assertEqual(len(rgb), 3)
            self.assertLessEqual(max(rgb), .065)

    def test_saved_brightness_has_a_global_volume_and_enabled_camera(self):
        volume = self.scene['226003003'][1]
        brightness = self.scene['226003004'][1]
        self.assertEqual(reference(volume, 'm_GameObject'), reference(brightness, 'm_GameObject'))
        self.assertEqual(field(volume, 'm_IsGlobal'), '1')
        self.assertEqual(field(volume, 'weight'), '1')
        self.assertEqual(field(brightness, 'm_Enabled'), '1')
        camera = self.player['4159511427452579016'][1]
        self.assertEqual(field(camera, 'm_RenderPostProcessing'), '1')
        self.assertIn('m_Bits: 1', camera)

    def test_flicker_is_wired_to_only_corridor_lights(self):
        controller = self.scene['226003002'][1]
        self.assertEqual(field(controller, 'm_Enabled'), '1')
        self.assertEqual(reference(controller, 'environmentRoot'), '8800000000000000033')
        guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Scripts/CorridorLightFlicker.cs.meta').read_text())[1]
        self.assertIn(guid, field(controller, 'm_Script'))
        targets = re.findall(r'fileID: (\d+)', controller.split('  lights:\n')[1].split('  burstInterval:')[0])
        self.assertEqual(len(targets), 5)
        self.assertNotIn('1948734481', targets)
        for target in targets:
            kind, light = self.scene[target]
            self.assertEqual(kind, 108)
            self.assertEqual(field(light, 'm_Enabled'), '1')
            self.assertEqual(field(light, 'm_Lightmapping'), '4')
            self.assertLessEqual(float(field(light, 'm_Intensity')), 4)
            go = reference(light, 'm_GameObject')
            self.assertIn('component: {fileID: ' + target + '}', self.scene[go][1])
        self.assertEqual(field(self.scene['1948734481'][1], 'm_Intensity'), '200')

    def test_exactly_one_pickup_uses_level_one_defense_and_fits_inventory(self):
        pickups = [b for kind, b in self.scene.values() if kind == 114 and
                   'guid: cc2e3a7d10b2fe240a5e74494f52a35d' in b]
        self.assertEqual(len(pickups), 1)
        pickup = pickups[0]
        self.assertEqual(field(pickup, 'isArtifact'), '0')
        self.assertEqual(field(pickup, 'isCheckpoint'), '0')
        self.assertEqual(field(pickup, 'promptMessage'), 'Press [E] to pick up Agimat.')
        go = reference(pickup, 'm_GameObject')
        colliders = [b for k, b in self.scene.values() if k == 136 and reference(b, 'm_GameObject') == go]
        self.assertEqual(len(colliders), 1)
        self.assertEqual(field(colliders[0], 'm_Enabled'), '1')
        self.assertIn('226001001', self.scene['9223372036854775807'][1])
        consumable = self.player['8969556251856540235'][1]
        self.assertEqual(field(consumable, 'duration'), '5')
        self.assertEqual(field(consumable, 'warningDistance'), '16')
        # Three Notes, one agimat and one flashlight all fit without blocking progress.
        self.assertGreaterEqual(int(field(self.player['5252281844144999244'][1], 'maxSlots')), 5)
        self.assertIn('isAgimat: 1', (ROOT / 'Assets/Assets/Inventory/Item_Consumable.asset').read_text())
        # No scene overrides may disable shared defense or change its duration.
        player = self.scene['1487628291'][1]
        self.assertNotRegex(player, r'target: \{fileID: 8969556251856540235[^\n]*\n      propertyPath: (?:duration|m_Enabled)')


if __name__ == '__main__':
    unittest.main()
