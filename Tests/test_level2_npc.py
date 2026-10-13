# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Serialized Level 2 integration checks; runtime behavior uses Level2NpcTests."""
# Import Path from Python pathlib to build paths relative to this script and read asset files.
from pathlib import Path
# Import hashlib for computing SHA-256 checksums for fixed serialized assets.
import hashlib
# Import json for encoding and decoding glTF/GLB JSON metadata.
import json
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Reuse documents from Tests/test_level2_ghost.py; documents loads Unity YAML into a fileID lookup.
from test_level2_ghost import documents
# Reuse field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import field, reference

# Resolve this Python file path, then move up two directory levels to locate the repository root in ROOT.
ROOT = Path(__file__).resolve().parents[1]


class Level2NpcAssets(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Scenes/Lvl-2.unity' by
        # test_level2_ghost.documents in cls.scene.
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')

    # Check that npc wiring and fixed root.
    def test_npc_wiring_and_fixed_root(self):
        # Store self.scene['226000003'][1] (read from self.scene['226000003']) in npc.
        npc = self.scene['226000003'][1]
        # Store self.scene[reference(npc, 'whiteLady')][1] (read from self.scene[reference(npc, 'whiteLady')]) in
        # ghost.
        ghost = self.scene[reference(npc, 'whiteLady')][1]
        # Expect fileID referenced by field 'clueStory' in ghost via test_collision_assets.reference to equal
        # '226000003'.
        self.assertEqual(reference(ghost, 'clueStory'), '226000003')
        # Store self.scene[reference(npc, 'nervousIdle')][1] (read from self.scene[reference(npc, 'nervousIdle')])
        # in idle.
        idle = self.scene[reference(npc, 'nervousIdle')][1]
        # Expect fileID referenced by field 'm_GameObject' in npc via test_collision_assets.reference to equal
        # fileID referenced by field 'm_GameObject' in idle via test_collision_assets.reference.
        self.assertEqual(reference(npc, 'm_GameObject'), reference(idle, 'm_GameObject'))
        # Expect fileID referenced by field 'environmentRoot' in idle via test_collision_assets.reference to equal
        # '8800000000000000033'.
        self.assertEqual(reference(idle, 'environmentRoot'), '8800000000000000033')
        # Expect serialized field 'isArtifact' read from npc by test_collision_assets.field to equal '0'.
        self.assertEqual(field(npc, 'isArtifact'), '0')
        # Expect serialized field 'nextSceneName' read from npc by test_collision_assets.field to equal 'Level3'.
        self.assertEqual(field(npc, 'nextSceneName'), 'Level3')
        # Process each (name, instance) from [('artifacts', '557273661'), ('objective', '867314172'),
        # ('notifications', '867314172')].
        for name, instance in [('artifacts', '557273661'), ('objective', '867314172'), ('notifications', '867314172')]:
            # Store self.scene[reference(npc, name)][1] (read from self.scene[reference(npc, name)]) in target.
            target = self.scene[reference(npc, name)][1]
            # Expect fileID referenced by field 'm_PrefabInstance' in target via test_collision_assets.reference
            # to equal instance.
            self.assertEqual(reference(target, 'm_PrefabInstance'), instance)
        # Store list of kind using (kind, body) from values stored in self.scene filtered by re.search('^
        # m_GameObject: \\{fileID: 226000000\\}$', body, re.M) in components.
        components = [kind for kind, body in self.scene.values()
                      if re.search(r'^  m_GameObject: \{fileID: 226000000\}$', body, re.M)]
        # Expect components to contain the same entries/counts as [4, 136, 114, 114].
        self.assertCountEqual(components, [4, 136, 114, 114])
        # Expect serialized field 'm_LocalPosition' read from self.scene['226000001'][1] (read from
        # self.scene['226000001']) by test_collision_assets.field to equal '{x: -83.489, y: -1.745, z: 164.93}'.
        self.assertEqual(field(self.scene['226000001'][1], 'm_LocalPosition'), '{x: -83.489, y: -1.745, z: 164.93}')
        # Store self.scene['1487628291'][1] (read from self.scene['1487628291']) in player.
        player = self.scene['1487628291'][1]
        # Expect player to match the regex written here.
        self.assertRegex(player, r'propertyPath: interactionText\n      value: *\n      objectReference: \{fileID: 226000012\}')

    # Check that existing dialogue component has a resolvable script name.
    def test_existing_dialogue_component_has_a_resolvable_script_name(self):
        # Store path ROOT joined with 'Assets/Scripts/NotificationManager.cs' in script.
        script = ROOT / 'Assets/Scripts/NotificationManager.cs'
        # Expect 'public class NotificationManager : MonoBehaviour' to appear in text read from file script.
        self.assertIn('public class NotificationManager : MonoBehaviour', script.read_text())
        # Store text read from file Path(str(script) + '.meta') in meta.
        meta = Path(str(script) + '.meta').read_text()
        # Store re.search('^guid: (\\w+)', meta, re.M)[1] (read from re.search('^guid: (\\w+)', meta, re.M)) in
        # guid.
        guid = re.search(r'^guid: (\w+)', meta, re.M)[1]
        # Expect guid to equal '86ee917182946ef4aae4199a25f194c4'.
        self.assertEqual(guid, '86ee917182946ef4aae4199a25f194c4')
        # Store text read from file ROOT / 'Assets/Assets/Prefabs/=== UI ===.prefab' in ui.
        ui = (ROOT / 'Assets/Assets/Prefabs/=== UI ===.prefab').read_text()
        # Expect 'm_Script: {fileID: 11500000, guid: ' + guid + ', type: 3}' (calculated from guid) to appear in
        # ui.
        self.assertIn('m_Script: {fileID: 11500000, guid: ' + guid + ', type: 3}', ui)
        # Expect the result of (ROOT / 'Assets/Scripts/NotificationText.cs').exists to be false.
        self.assertFalse((ROOT / 'Assets/Scripts/NotificationText.cs').exists())

    # Check that all three existing notes and inventory capacity.
    def test_all_three_existing_notes_and_inventory_capacity(self):
        # Store list of b using (k, b) from values stored in self.scene filtered by k == 1001 and 'm_SourcePrefab:
        # {fileID: 100100000, guid: 400c42ff1cb449eeb4d11d96009213c4' in b in notes.
        notes = [b for k, b in self.scene.values() if k == 1001 and
                 'm_SourcePrefab: {fileID: 100100000, guid: 400c42ff1cb449eeb4d11d96009213c4' in b]
        # Expect length of notes to equal 3.
        self.assertEqual(len(notes), 3)
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Assets/Prefabs/Note
        # Artifact.prefab' by test_level2_ghost.documents in prefab.
        prefab = documents(ROOT / 'Assets/Assets/Prefabs/Note Artifact.prefab')
        # Expect serialized field 'isArtifact' read from prefab['1005'][1] (read from prefab['1005']) by
        # test_collision_assets.field to equal '1'.
        self.assertEqual(field(prefab['1005'][1], 'isArtifact'), '1')
        # Expect '9c7916d32634eb748a23e544f1a118a0' to appear in serialized field 'itemData' read from
        # prefab['1005'][1] (read from prefab['1005']) by test_collision_assets.field.
        self.assertIn('9c7916d32634eb748a23e544f1a118a0', field(prefab['1005'][1], 'itemData'))
        # Store Unity YAML documents loaded from path ROOT joined with
        # 'Assets/Assets/Prefabs/ArtifactManager.prefab' by test_level2_ghost.documents in manager.
        manager = documents(ROOT / 'Assets/Assets/Prefabs/ArtifactManager.prefab')
        # Expect serialized field 'totalArtifacts' read from manager['3433380000644894917'][1] (read from
        # manager['3433380000644894917']) by test_collision_assets.field converted to an integer to equal length
        # of notes.
        self.assertEqual(int(field(manager['3433380000644894917'][1], 'totalArtifacts')), len(notes))
        # Store Unity YAML documents loaded from path ROOT joined with 'Assets/Assets/Prefabs/===PLAYER===.prefab'
        # by test_level2_ghost.documents in player.
        player = documents(ROOT / 'Assets/Assets/Prefabs/===PLAYER===.prefab')
        # Expect serialized field 'maxSlots' read from player['5252281844144999244'][1] (read from
        # player['5252281844144999244']) by test_collision_assets.field converted to an integer to be at least
        # len(notes) + 2 (calculated from notes).
        self.assertGreaterEqual(int(field(player['5252281844144999244'][1], 'maxSlots')), len(notes) + 2)
        # Expect 'value: All clues found. Return to the frightened woman.' to appear in self.scene['557273661'][1]
        # (read from self.scene['557273661']).
        self.assertIn('value: All clues found. Return to the frightened woman.', self.scene['557273661'][1])

    # Check that placed character and nervous clip are connected to interaction root.
    def test_placed_character_and_nervous_clip_are_connected_to_interaction_root(self):
        # Store self.scene['226000004'][1] (read from self.scene['226000004']) in idle.
        idle = self.scene['226000004'][1]
        # Store self.scene[reference(idle, 'character')][1] (read from self.scene[reference(idle, 'character')])
        # in visual.
        visual = self.scene[reference(idle, 'character')][1]
        # Store fileID referenced by field 'm_PrefabInstance' in visual via test_collision_assets.reference in
        # instance_id.
        instance_id = reference(visual, 'm_PrefabInstance')
        # Store self.scene[instance_id][1] (read from self.scene[instance_id]) in model.
        model = self.scene[instance_id][1]
        # Expect '    m_TransformParent: {fileID: 226000001}' to appear in model.
        self.assertIn('    m_TransformParent: {fileID: 226000001}', model)
        # Expect 'guid: f6cc5544201dc094291bc1ad60c1112f' to appear in model.
        self.assertIn('guid: f6cc5544201dc094291bc1ad60c1112f', model)
        # Expect '  m_Children:\n  - {fileID: 226000005}' to appear in self.scene['226000001'][1] (read from
        # self.scene['226000001']).
        self.assertIn('  m_Children:\n  - {fileID: 226000005}', self.scene['226000001'][1])
        # Expect '{fileID: ' + instance_id + '}' (calculated from instance_id) to be absent from
        # self.scene['9223372036854775807'][1] (read from self.scene['9223372036854775807']).
        self.assertNotIn('{fileID: ' + instance_id + '}', self.scene['9223372036854775807'][1])
        # Expect length of [b for (k, b) in self.scene.values() if k == 1001 and 'guid:
        # f6cc5544201dc094291bc1ad60c1112f' in b] to equal 1.
        self.assertEqual(len([b for k, b in self.scene.values() if k == 1001 and
                             'guid: f6cc5544201dc094291bc1ad60c1112f' in b]), 1)
        # Process each axis from 'xyz'.
        for axis in 'xyz':
            # Expect model to match the regex written here.
            self.assertRegex(model, rf'propertyPath: m_LocalPosition\.{axis}\n      value: 0\n')
        # Store serialized field 'nervousClip' read from idle by test_collision_assets.field in clip.
        clip = field(idle, 'nervousClip')
        # Expect 'fileID: 7400000' to appear in clip.
        self.assertIn('fileID: 7400000', clip)
        # Expect 'guid: ba763fd43a5b3374aa7d14e330518b9e' to appear in clip.
        self.assertIn('guid: ba763fd43a5b3374aa7d14e330518b9e', clip)
        # Expect 'characterResource:' to be absent from idle.
        self.assertNotIn('characterResource:', idle)
        # Process each name from ['char_npc', 'Nervously Look Around'].
        for name in ['char_npc', 'Nervously Look Around']:
            # Store path ROOT joined with formatted text inserting name in path.
            path = ROOT / f'Assets/Assets/free assets/char_npc/{name}.fbx'
            # Expect whether path exists as a file to be true.
            self.assertTrue(path.is_file())
            # Store text read from file Path(str(path) + '.meta') in meta.
            meta = Path(str(path) + '.meta').read_text()
            # Expect 'animationType: 3' to appear in meta.
            self.assertIn('animationType: 3', meta)
            # Expect 'avatarSetup: 1' to appear in meta.
            self.assertIn('avatarSetup: 1', meta)
        # Expect 'internalID: 7400000' to appear in meta.
        self.assertIn('internalID: 7400000', meta)
        # Expect 'loopTime: 1' to appear in meta.
        self.assertIn('loopTime: 1', meta)
        # Process each axis from ['Orientation', 'PositionY', 'PositionXZ'].
        for axis in ['Orientation', 'PositionY', 'PositionXZ']:
            # Expect formatted text inserting axis to appear in meta.
            self.assertIn(f'loopBlend{axis}: 1', meta)

    # Check that current environment ghost and notes unchanged.
    def test_current_environment_ghost_and_notes_unchanged(self):
        # Hashes of serialized documents from the current level-2-merge
        # 5ef1ec3719f8bb43e3f4a9a5712a70f960857653 base. Geometry and quest wiring stay fixed;
        # RenderSettings are covered by test_level2_atmosphere after intentional darkening.
        # Store a dictionary containing '1', '1186399119', '3', '4', '437939276', '8800000000000000032',
        # '8800000000000000033', '8800000000000000110', '8800000000000000111', '8800000000000000112', '978205674'
        # as keys in expected.
        expected = {'1': '72d265b7b2a916d26a2cd17e7d7ea22fc44de6a8aa817e91cb32ef478644a190',
         '1186399119': 'cdbfb0d458c983f25a7f5a385d4d8cd8678761c07e02a0f6ac58a9b0e4b63838',
         '3': '4a8983ae894d61f1925364ad9a576a6b946ea2726a6f6e154e52d28db624a558',
         '4': '8c75158c3eb85863fa14829ba849f7a2e8b87f391e64eeabb1b04361ef8d5d9d',
         '437939276': '1ccc12e32dc5908a59431284dd8a14cbc70d18fb724a1d54ed85cc8a8218f883',
         '8800000000000000032': '3d8117068f44873ac2817516ce10d25abb9302ef038a0d316a00739483370b7e',
         '8800000000000000033': 'a47e9738325d2e0f3394bc22942a6bb9e4745c6bc490fea994d2943362efec57',
         '8800000000000000110': 'b4b626e7319e60d3e27de7a7e2c0f6628927b0b658bf408d86d5613f503618ea',
         '8800000000000000111': '0cda5b8c95d12930c817bfe07fee9a0f64386be33bb5e9f64f5cc1c76c93eca5',
         '8800000000000000112': 'dac8f6207e7668b1556eb1384a4339f793085b2c21b74deb29b6faf934e4e9c1',
         '978205674': '6fa3e5b270df54bed24dc5eec572a7ac2b9d67ffe02f127ef76b4861fddd59c7'}
        # Process each (key, digest) from key/value pairs stored in expected.
        for key, digest in expected.items():
            # Store self.scene[key][1] (read from self.scene[key]) in body.
            body = self.scene[key][1]
            # Run this branch only if key equals '1186399119'.
            if key == '1186399119':
                # Store text from body with '  clueStory: {fileID: 226000003}\n' replaced by '' in body.
                body = body.replace('  clueStory: {fileID: 226000003}\n', '')
            # Expect hexadecimal checksum string from hashlib.sha256(body.encode()) to equal digest.
            self.assertEqual(hashlib.sha256(body.encode()).hexdigest(), digest, key)

    # Check that scene dialogue matches defaults and reveal order.
    def test_scene_dialogue_matches_defaults_and_reveal_order(self):
        # Store self.scene['226000003'][1] (read from self.scene['226000003']) in npc.
        npc = self.scene['226000003'][1]
        # Store text read from file ROOT / 'Assets/Scripts/Level2NoteNpc.cs' in source.
        source = (ROOT / 'Assets/Scripts/Level2NoteNpc.cs').read_text()
        # Store metadata decoded from JSON in serialized field 'returnedDialogue' read from npc by
        # test_collision_assets.field in returned.
        returned = json.loads(field(npc, 'returnedDialogue'))
        # Expect "She's not going to hurt you anymore" to appear in returned.
        self.assertIn("She's not going to hurt you anymore", returned)
        # Expect 'public string returnedDialogue = ' + json.dumps(returned) + ';' (calculated from returned, json)
        # to appear in parsed metadata from the original unrigged glTF.
        self.assertIn('public string returnedDialogue = ' + json.dumps(returned) + ';', source)
        # Store list of json.loads(line) using line from all regex matches extracted from npc using the pattern
        # written here in pages.
        pages = [json.loads(line) for line in re.findall(r'^  - (".*")$', npc, re.M)]
        # Store all regex matches extracted from parsed metadata from the original unrigged glTF using the pattern
        # written here in defaults.
        defaults = re.findall(r'^        "(.*)"[,]?$', source, re.M)
        # Expect pages to equal defaults.
        self.assertEqual(pages, defaults)
        # Expect length of pages to equal 7.
        self.assertEqual(len(pages), 7)
        # Process each (page, phrase) from corresponding entries paired from pages, ['serial killer', 'target',
        # 'woman in white', "The White Lady wasn't trying to kill you.", 'justice', 'Because I am the White
        # Lady.', "Renato's victims"].
        for page, phrase in zip(pages, ['serial killer', 'target', 'woman in white',
                "The White Lady wasn't trying to kill you.", 'justice',
                'Because I am the White Lady.', "Renato's victims"]):
            # Expect phrase to appear in page.
            self.assertIn(phrase, page)


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
