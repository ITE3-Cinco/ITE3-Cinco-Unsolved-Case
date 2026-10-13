# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Level 2 navigation and lose-screen asset references; no Unity required."""
# Import Path from Python pathlib to build paths relative to this script and read asset files.
from pathlib import Path
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Reuse field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import field, reference

# Resolve this Python file path, then move up two directory levels to locate the repository root in ROOT.
ROOT = Path(__file__).resolve().parents[1]


# Split a Unity asset into documents keyed by fileID and reject duplicate IDs.
# path is the scene/prefab asset path supplied by the test setup.
def documents(path):
    # Store all regex matches extracted from text read from file path using the pattern written here in blocks.
    blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)',
                        path.read_text(), re.MULTILINE | re.DOTALL)
    # Store dictionary mapping key to (int(kind), body) from blocks in result.
    result = {key: (int(kind), body) for kind, key, body in blocks}
    # Run this branch only if length of result differs from length of blocks.
    if len(result) != len(blocks):
        # Raise AssertionError(f'Duplicate file IDs in {path}') because the loaded asset data violates this
        # required structure.
        raise AssertionError(f'Duplicate file IDs in {path}')
    # Return result to the helper caller.
    return result


class Level2GhostTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Scenes/Lvl-2.unity' by
        # test_level2_ghost.documents in cls.scene.
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Assets/Prefabs/=== UI ===.prefab'
        # by test_level2_ghost.documents in cls.ui.
        cls.ui = documents(ROOT / 'Assets/Assets/Prefabs/=== UI ===.prefab')

    # Check that navigation uses imported map and generates its missing colliders.
    def test_navigation_uses_imported_map_and_generates_its_missing_colliders(self):
        # Store self.scene['8800000000000000032'][1] (read from self.scene['8800000000000000032']) in nav.
        nav = self.scene['8800000000000000032'][1]
        # Store self.scene[reference(nav, 'environmentRoot')] (read from self.scene) in root.
        root = self.scene[reference(nav, 'environmentRoot')]
        # Expect root[0] (read from root) to equal 4.
        self.assertEqual(root[0], 4)
        # Expect fileID referenced by field 'm_PrefabInstance' in root[1] (read from root) via
        # test_collision_assets.reference to equal '978205674'.
        self.assertEqual(reference(root[1], 'm_PrefabInstance'), '978205674')
        # Store serialized field 'm_CorrespondingSourceObject' read from root[1] (read from root) by
        # test_collision_assets.field in source.
        source = field(root[1], 'm_CorrespondingSourceObject')
        # Expect 'guid: aa1af663a18182e45bdf2fb698e3c43e' to appear in parsed metadata from the original unrigged
        # glTF.
        self.assertIn('guid: aa1af663a18182e45bdf2fb698e3c43e', source)
        # Expect 'fileID: 2774220344278030320' to appear in parsed metadata from the original unrigged glTF.
        self.assertIn('fileID: 2774220344278030320', source)
        # Expect serialized field 'addMissingMeshColliders' read from nav by test_collision_assets.field to equal
        # '1'.
        self.assertEqual(field(nav, 'addMissingMeshColliders'), '1')
        # Expect serialized field 'voxelSize' read from nav by test_collision_assets.field converted to a number
        # with decimals to be at most 0.1.
        self.assertLessEqual(float(field(nav, 'voxelSize')), .1)
        # Store self.scene['1186399122'][1] (read from self.scene['1186399122']) in agent.
        agent = self.scene['1186399122'][1]
        # Expect serialized field 'agentTypeID' read from nav by test_collision_assets.field to equal serialized
        # field 'm_AgentTypeID' read from agent by test_collision_assets.field.
        self.assertEqual(field(nav, 'agentTypeID'), field(agent, 'm_AgentTypeID'))
        # Expect serialized field 'm_Enabled' read from agent by test_collision_assets.field to equal '0'.
        self.assertEqual(field(agent, 'm_Enabled'), '0')
        # Expect fileID referenced by field 'm_GameObject' in nav via test_collision_assets.reference to equal
        # fileID referenced by field 'm_GameObject' in agent via test_collision_assets.reference.
        self.assertEqual(reference(nav, 'm_GameObject'), reference(agent, 'm_GameObject'))

    # Check that catch uses existing lose canvas and retries level2.
    def test_catch_uses_existing_lose_canvas_and_retries_level2(self):
        # Store self.scene['1186399119'][1] (read from self.scene['1186399119']) in ghost.
        ghost = self.scene['1186399119'][1]
        # Store self.scene[reference(ghost, 'loseSceneController')][1] (read from self.scene[reference(ghost,
        # 'loseSceneController')]) in lose_ref.
        lose_ref = self.scene[reference(ghost, 'loseSceneController')][1]
        # Store self.ui[reference(lose_ref, 'm_CorrespondingSourceObject')][1] (read from
        # self.ui[reference(lose_ref, 'm_CorrespondingSourceObject')]) in lose.
        lose = self.ui[reference(lose_ref, 'm_CorrespondingSourceObject')][1]
        # Store self.ui[reference(lose, 'loseCanvas')][1] (read from self.ui[reference(lose, 'loseCanvas')]) in
        # canvas.
        canvas = self.ui[reference(lose, 'loseCanvas')][1]
        # Expect serialized field 'm_Name' read from canvas by test_collision_assets.field to equal 'LoseCanvas'.
        self.assertEqual(field(canvas, 'm_Name'), 'LoseCanvas')
        # Expect serialized field 'm_IsActive' read from canvas by test_collision_assets.field to equal '0'.
        self.assertEqual(field(canvas, 'm_IsActive'), '0')
        # Process each button from ['tryAgainButton', 'exitButton'].
        for button in ['tryAgainButton', 'exitButton']:
            # Expect self.ui[reference(lose, button)][0] (read from self.ui[reference(lose, button)]) to equal
            # 114.
            self.assertEqual(self.ui[reference(lose, button)][0], 114)
        # Store self.scene[reference(lose_ref, 'm_PrefabInstance')][1] (read from self.scene[reference(lose_ref,
        # 'm_PrefabInstance')]) in ui_instance.
        ui_instance = self.scene[reference(lose_ref, 'm_PrefabInstance')][1]
        # Expect ui_instance to match the regex written here.
        self.assertRegex(ui_instance, r'propertyPath: retrySceneName\n      value: Lvl-2\n')
        # Expect serialized field 'requireNavigation' read from ghost by test_collision_assets.field to equal '1'.
        self.assertEqual(field(ghost, 'requireNavigation'), '1')
        # Expect serialized field 'catchRadius' read from ghost by test_collision_assets.field converted to a
        # number with decimals to be greater than 0.36568844 + 0.5 + 0.08 (calculated from ).
        self.assertGreater(float(field(ghost, 'catchRadius')), .36568844 + .5 + .08)


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
