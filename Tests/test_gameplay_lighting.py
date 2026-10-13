# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Scene lighting and brightness wiring; exposure behavior also has Unity tests."""
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Import test_collision_assets as assets for the helpers used below.
import test_collision_assets as assets
# Reuse ASSETS, ROOT, field, reference from Tests/test_collision_assets.py; that module defines repository paths
# and Unity field/fileID parsing.
from test_collision_assets import ASSETS, ROOT, field, reference


class GameplayLightingTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Initialize the shared fixture data in assets.CollisionAssetsTests.
        assets.CollisionAssetsTests.setUpClass()
        # Store assets.CollisionAssetsTests.assets[ASSETS[0]] (read from assets.CollisionAssetsTests.assets) in
        # (cls.docs, cls.components).
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[ASSETS[0]]

    # Check that gameplay camera processes global brightness volume.
    def test_gameplay_camera_processes_global_brightness_volume(self):
        # Expect serialized field 'm_RenderPostProcessing' read from self.docs['13884630'][1] (read from
        # self.docs['13884630']) by test_collision_assets.field to equal '1'.
        self.assertEqual(field(self.docs['13884630'][1], 'm_RenderPostProcessing'), '1')
        # Store self.docs['8800000000000000023'][1] (read from self.docs['8800000000000000023']) in brightness.
        brightness = self.docs['8800000000000000023'][1]
        # Expect serialized field 'm_Enabled' read from brightness by test_collision_assets.field to equal '1'.
        self.assertEqual(field(brightness, 'm_Enabled'), '1')
        # Store fileID referenced by field 'm_GameObject' in brightness via test_collision_assets.reference in go.
        go = reference(brightness, 'm_GameObject')
        # Store the first matching entry from sequence of b using (_, k, b) from self.components[go] (read from
        # self.components) filtered by k == 114 and 'sharedProfile:' in b in volume.
        volume = next(b for _, k, b in self.components[go] if k == 114 and 'sharedProfile:' in b)
        # Expect serialized field 'm_IsGlobal' read from volume by test_collision_assets.field to equal '1'.
        self.assertEqual(field(volume, 'm_IsGlobal'), '1')
        # Expect serialized field 'weight' read from volume by test_collision_assets.field to equal '1'.
        self.assertEqual(field(volume, 'weight'), '1')
        # Expect '8800000000000000023' to appear in self.docs[go][1] (read from self.docs[go]).
        self.assertIn('8800000000000000023', self.docs[go][1])
        # Store text read from file ROOT / 'Assets/Scripts/SceneBrightness.cs.meta' in meta.
        meta = (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text()
        # Expect re.search('guid: (\\w+)', meta)[1] (read from re.search('guid: (\\w+)', meta)) to appear in
        # serialized field 'm_Script' read from brightness by test_collision_assets.field.
        self.assertIn(re.search(r'guid: (\w+)', meta)[1], field(brightness, 'm_Script'))

    # Check that flashlight exceeds corridor fixtures and ambient is dim.
    def test_flashlight_exceeds_corridor_fixtures_and_ambient_is_dim(self):
        # Store serialized field 'm_Intensity' read from self.docs['687620204'][1] (read from
        # self.docs['687620204']) by test_collision_assets.field converted to a number with decimals in
        # flashlight.
        flashlight = float(field(self.docs['687620204'][1], 'm_Intensity'))
        # Expect flashlight to equal 200.
        self.assertEqual(flashlight, 200)
        # Store list of b using (k, b) from values stored in self.docs filtered by k == 108 and field(b, 'm_Type')
        # == '0' and (reference(b, 'm_GameObject') != reference(self.docs['687620204'][1], 'm_GameObject')) in
        # fixtures.
        fixtures = [b for k, b in self.docs.values() if k == 108 and field(b, 'm_Type') == '0'
                    and reference(b, 'm_GameObject') != reference(self.docs['687620204'][1], 'm_GameObject')]
        # Expect fixtures to be true.
        self.assertTrue(fixtures)
        # Process each light from fixtures.
        for light in fixtures:
            # Expect serialized field 'm_Intensity' read from light by test_collision_assets.field converted to a
            # number with decimals to be at most flashlight / 25 (calculated from flashlight).
            self.assertLessEqual(float(field(light, 'm_Intensity')), flashlight / 25)
        # Store the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 104 in settings.
        settings = next(b for k, b in self.docs.values() if k == 104)
        # Expect serialized field 'm_AmbientMode' read from settings by test_collision_assets.field to equal '1'.
        self.assertEqual(field(settings, 'm_AmbientMode'), '1')
        # Expect serialized field 'm_ReflectionIntensity' read from settings by test_collision_assets.field
        # converted to a number with decimals to be at most 0.2.
        self.assertLessEqual(float(field(settings, 'm_ReflectionIntensity')), .2)
        # Expect serialized field 'm_Intensity' read from self.docs['1749609168'][1] (read from
        # self.docs['1749609168']) by test_collision_assets.field converted to a number with decimals to be at
        # most 0.1.
        self.assertLessEqual(float(field(self.docs['1749609168'][1], 'm_Intensity')), .1)

    # Check that every build scene applies saved brightness to its camera.
    def test_every_build_scene_applies_saved_brightness_to_its_camera(self):
        # Store text read from file ROOT / 'ProjectSettings/EditorBuildSettings.asset' in build.
        build = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
        # Store all regex matches extracted from build using the pattern written here in scenes.
        scenes = re.findall(r'enabled: 1\n    path: (.+)', build)
        # Store re.search('guid: (\\w+)', (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text())[1] (read
        # from re.search('guid: (\\w+)', (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text())) in
        # brightness_guid.
        brightness_guid = re.search(r'guid: (\w+)', (ROOT / 'Assets/Scripts/SceneBrightness.cs.meta').read_text())[1]
        # Expect scenes to be true.
        self.assertTrue(scenes)
        # Process each scene from scenes.
        for scene in scenes:
            # Label this asset/object combination as a separate unittest subtest so a failure identifies the exact
            # fixture.
            with self.subTest(scene=scene):
                # Store text read from file ROOT / scene in text.
                text = (ROOT / scene).read_text()
                # Store all regex matches extracted from text using the pattern written here in blocks.
                blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
                # Store dictionary mapping key to (int(kind), body) from blocks in docs.
                docs = {key: (int(kind), body) for kind, key, body in blocks}
                # Expect length of docs to equal length of blocks.
                self.assertEqual(len(docs), len(blocks))
                # Store list of (key, b) using (key, (k, b)) from key/value pairs stored in docs filtered by k ==
                # 114 and brightness_guid in b in brightness.
                brightness = [(key, b) for key, (k, b) in docs.items() if k == 114 and brightness_guid in b]
                # Expect length of brightness to equal 1.
                self.assertEqual(len(brightness), 1)
                # Store brightness[0] (read from brightness) in (key, body).
                key, body = brightness[0]
                # Expect serialized field 'm_Enabled' read from body by test_collision_assets.field to equal '1'.
                self.assertEqual(field(body, 'm_Enabled'), '1')
                # Store fileID referenced by field 'm_GameObject' in body via test_collision_assets.reference in
                # go.
                go = reference(body, 'm_GameObject')
                # Expect 'component: {fileID: ' + key + '}' (calculated from key) to appear in docs[go][1] (read
                # from docs[go]).
                self.assertIn('component: {fileID: ' + key + '}', docs[go][1])
                # Expect serialized field 'm_IsActive' read from docs[go][1] (read from docs[go]) by
                # test_collision_assets.field to equal '1'.
                self.assertEqual(field(docs[go][1], 'm_IsActive'), '1')
                # Store serialized field 'm_Layer' read from docs[go][1] (read from docs[go]) by
                # test_collision_assets.field converted to an integer in layer.
                layer = int(field(docs[go][1], 'm_Layer'))
                # Store list of b using (k, b) from values stored in docs filtered by k == 114 and reference(b,
                # 'm_GameObject') == go and ('sharedProfile:' in b) in volumes.
                volumes = [b for k, b in docs.values() if k == 114 and reference(b, 'm_GameObject') == go and 'sharedProfile:' in b]
                # Expect length of volumes to equal 1.
                self.assertEqual(len(volumes), 1)
                # Expect serialized field 'm_IsGlobal' read from volumes[0] (read from volumes) by
                # test_collision_assets.field to equal '1'.
                self.assertEqual(field(volumes[0], 'm_IsGlobal'), '1')
                # Expect serialized field 'weight' read from volumes[0] (read from volumes) by
                # test_collision_assets.field to equal '1'.
                self.assertEqual(field(volumes[0], 'weight'), '1')
                # Store list of b using (k, b) from values stored in docs filtered by k == 114 and
                # 'm_RenderPostProcessing:' in b in cameras.
                cameras = [b for k, b in docs.values() if k == 114 and 'm_RenderPostProcessing:' in b]
                # Expect cameras to be true.
                self.assertTrue(cameras)
                # Process each camera from cameras.
                for camera in cameras:
                    # Expect serialized field 'm_RenderPostProcessing' read from camera by
                    # test_collision_assets.field to equal '1'.
                    self.assertEqual(field(camera, 'm_RenderPostProcessing'), '1')
                    # Store re.search('m_VolumeLayerMask:\\n    serializedVersion: \\d+\\n    m_Bits: (\\d+)',
                    # camera)[1] (read from re.search('m_VolumeLayerMask:\\n    serializedVersion: \\d+\\n
                    # m_Bits: (\\d+)', camera)) converted to an integer in mask.
                    mask = int(re.search(r'm_VolumeLayerMask:\n    serializedVersion: \d+\n    m_Bits: (\d+)', camera)[1])
                    # Expect mask & 1 << layer (calculated from mask, layer) to be true.
                    self.assertTrue(mask & (1 << layer))
