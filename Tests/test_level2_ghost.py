"""Level 2 navigation and lose-screen asset references; no Unity required."""
from pathlib import Path
import re
import unittest
from test_collision_assets import field, reference

ROOT = Path(__file__).resolve().parents[1]


def documents(path):
    blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',
                        path.read_text(), re.MULTILINE | re.DOTALL)
    result = {key: (int(kind), body) for kind, key, body in blocks}
    if len(result) != len(blocks):
        raise AssertionError(f'Duplicate file IDs in {path}')
    return result


class Level2GhostTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')
        cls.ui = documents(ROOT / 'Assets/Assets/Prefabs/=== UI ===.prefab')

    def test_navigation_uses_imported_map_and_generates_its_missing_colliders(self):
        nav = self.scene['8800000000000000032'][1]
        root = self.scene[reference(nav, 'environmentRoot')]
        self.assertEqual(root[0], 4)
        self.assertEqual(reference(root[1], 'm_PrefabInstance'), '978205674')
        source = field(root[1], 'm_CorrespondingSourceObject')
        self.assertIn('guid: aa1af663a18182e45bdf2fb698e3c43e', source)
        self.assertIn('fileID: 2774220344278030320', source)
        self.assertEqual(field(nav, 'addMissingMeshColliders'), '1')
        self.assertLessEqual(float(field(nav, 'voxelSize')), .1)
        agent = self.scene['1186399122'][1]
        self.assertEqual(field(nav, 'agentTypeID'), field(agent, 'm_AgentTypeID'))
        self.assertEqual(field(agent, 'm_Enabled'), '0')
        self.assertEqual(reference(nav, 'm_GameObject'), reference(agent, 'm_GameObject'))

    def test_catch_uses_existing_lose_canvas_and_retries_level2(self):
        ghost = self.scene['1186399119'][1]
        lose_ref = self.scene[reference(ghost, 'loseSceneController')][1]
        lose = self.ui[reference(lose_ref, 'm_CorrespondingSourceObject')][1]
        canvas = self.ui[reference(lose, 'loseCanvas')][1]
        self.assertEqual(field(canvas, 'm_Name'), 'LoseCanvas')
        self.assertEqual(field(canvas, 'm_IsActive'), '0')
        for button in ['tryAgainButton', 'exitButton']:
            self.assertEqual(self.ui[reference(lose, button)][0], 114)
        ui_instance = self.scene[reference(lose_ref, 'm_PrefabInstance')][1]
        self.assertRegex(ui_instance, r'propertyPath: retrySceneName\n      value: Lvl-2\n')
        self.assertEqual(field(ghost, 'requireNavigation'), '1')
        self.assertGreater(float(field(ghost, 'catchRadius')), .36568844 + .5 + .08)


if __name__ == '__main__':
    unittest.main()
