"""Scene lighting and brightness wiring; exposure behavior also has Unity tests."""
import re
import unittest
import test_collision_assets as assets
from test_collision_assets import ASSETS, ROOT, field, reference


class GameplayLightingTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        assets.CollisionAssetsTests.setUpClass()
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[ASSETS[0]]

    def test_gameplay_camera_processes_global_brightness_volume(self):
        self.assertEqual(field(self.docs['13884630'][1], 'm_RenderPostProcessing'), '1')
        brightness = self.docs['8800000000000000023'][1]
        self.assertEqual(field(brightness, 'm_Enabled'), '1')
        go = reference(brightness, 'm_GameObject')
        volume = next(b for _, k, b in self.components[go] if k == 114 and 'sharedProfile:' in b)
        self.assertEqual(field(volume, 'm_IsGlobal'), '1')
        self.assertEqual(field(volume, 'weight'), '1')
        self.assertIn('8800000000000000023', self.docs[go][1])
        meta = (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text()
        self.assertIn(re.search(r'guid: (\w+)', meta)[1], field(brightness, 'm_Script'))

    def test_flashlight_exceeds_corridor_fixtures_and_ambient_is_dim(self):
        flashlight = float(field(self.docs['687620204'][1], 'm_Intensity'))
        self.assertEqual(flashlight, 200)
        fixtures = [b for k, b in self.docs.values() if k == 108 and field(b, 'm_Type') == '0'
                    and reference(b, 'm_GameObject') != reference(self.docs['687620204'][1], 'm_GameObject')]
        self.assertTrue(fixtures)
        for light in fixtures:
            self.assertLessEqual(float(field(light, 'm_Intensity')), flashlight / 25)
        settings = next(b for k, b in self.docs.values() if k == 104)
        self.assertEqual(field(settings, 'm_AmbientMode'), '1')
        self.assertLessEqual(float(field(settings, 'm_ReflectionIntensity')), .2)
        self.assertLessEqual(float(field(self.docs['1749609168'][1], 'm_Intensity')), .1)
