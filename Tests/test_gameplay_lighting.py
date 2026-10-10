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

    def test_every_build_scene_applies_saved_brightness_to_its_camera(self):
        build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
        scenes = re.findall(r'enabled: 1\n    path: (.+)', build)
        brightness_guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text())[1]
        self.assertTrue(scenes)
        for scene in scenes:
            with self.subTest(scene=scene):
                text = (ROOT / scene).read_text()
                blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
                docs = {key: (int(kind), body) for kind, key, body in blocks}
                self.assertEqual(len(docs), len(blocks))
                brightness = [(key, b) for key, (k, b) in docs.items() if k == 114 and brightness_guid in b]
                self.assertEqual(len(brightness), 1)
                key, body = brightness[0]
                self.assertEqual(field(body, 'm_Enabled'), '1')
                go = reference(body, 'm_GameObject')
                self.assertIn('component: {fileID: ' + key + '}', docs[go][1])
                self.assertEqual(field(docs[go][1], 'm_IsActive'), '1')
                layer = int(field(docs[go][1], 'm_Layer'))
                volumes = [b for k, b in docs.values() if k == 114 and reference(b, 'm_GameObject') == go and 'sharedProfile:' in b]
                self.assertEqual(len(volumes), 1)
                self.assertEqual(field(volumes[0], 'm_IsGlobal'), '1')
                self.assertEqual(field(volumes[0], 'weight'), '1')
                cameras = [b for k, b in docs.values() if k == 114 and 'm_RenderPostProcessing:' in b]
                self.assertTrue(cameras)
                for camera in cameras:
                    self.assertEqual(field(camera, 'm_RenderPostProcessing'), '1')
                    mask = int(re.search(r'm_VolumeLayerMask:\n    serializedVersion: \d+\n    m_Bits: (\d+)', camera)[1])
                    self.assertTrue(mask & (1 << layer))
