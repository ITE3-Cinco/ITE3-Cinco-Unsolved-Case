"""Serialized Level 2 integration checks; runtime behavior uses Level2NpcTests."""
from pathlib import Path
import hashlib
import json
import re
import unittest
from test_level2_ghost import documents
from test_collision_assets import field, reference

ROOT = Path(__file__).resolve().parents[1]


class Level2NpcAssets(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.scene = documents(ROOT / 'Assets/Scenes/Lvl-2.unity')

    def test_npc_wiring_and_fixed_root(self):
        npc = self.scene['226000003'][1]
        ghost = self.scene[reference(npc, 'whiteLady')][1]
        self.assertEqual(reference(ghost, 'clueStory'), '226000003')
        idle = self.scene[reference(npc, 'nervousIdle')][1]
        self.assertEqual(reference(npc, 'm_GameObject'), reference(idle, 'm_GameObject'))
        self.assertEqual(reference(idle, 'environmentRoot'), '8800000000000000033')
        self.assertEqual(field(npc, 'isArtifact'), '0')
        self.assertEqual(field(npc, 'nextSceneName'), 'Level3')
        for name, instance in [('artifacts', '557273661'), ('objective', '867314172'), ('notifications', '867314172')]:
            target = self.scene[reference(npc, name)][1]
            self.assertEqual(reference(target, 'm_PrefabInstance'), instance)
        components = [kind for kind, body in self.scene.values()
                      if re.search(r'^  m_GameObject: \{fileID: 226000000\}$', body, re.M)]
        self.assertCountEqual(components, [4, 136, 114, 114])
        self.assertEqual(field(self.scene['226000001'][1], 'm_LocalPosition'), '{x: -83.489, y: -1.745, z: 164.93}')
        player = self.scene['1487628291'][1]
        self.assertRegex(player, r'propertyPath: interactionText\n      value: *\n      objectReference: \{fileID: 226000012\}')

    def test_existing_dialogue_component_has_a_resolvable_script_name(self):
        script = ROOT / 'Assets/Scripts/NotificationManager.cs'
        self.assertIn('public class NotificationManager : MonoBehaviour', script.read_text())
        meta = Path(str(script) + '.meta').read_text()
        guid = re.search(r'^guid: (\w+)', meta, re.M)[1]
        self.assertEqual(guid, '86ee917182946ef4aae4199a25f194c4')
        ui = (ROOT / 'Assets/Assets/Prefabs/=== UI ===.prefab').read_text()
        self.assertIn('m_Script: {fileID: 11500000, guid: ' + guid + ', type: 3}', ui)
        self.assertFalse((ROOT / 'Assets/Scripts/NotificationText.cs').exists())

    def test_all_three_existing_notes_and_inventory_capacity(self):
        notes = [b for k, b in self.scene.values() if k == 1001 and
                 'm_SourcePrefab: {fileID: 100100000, guid: 400c42ff1cb449eeb4d11d96009213c4' in b]
        self.assertEqual(len(notes), 3)
        prefab = documents(ROOT / 'Assets/Assets/Prefabs/Note Artifact.prefab')
        self.assertEqual(field(prefab['1005'][1], 'isArtifact'), '1')
        self.assertIn('9c7916d32634eb748a23e544f1a118a0', field(prefab['1005'][1], 'itemData'))
        manager = documents(ROOT / 'Assets/Assets/Prefabs/ArtifactManager.prefab')
        self.assertEqual(int(field(manager['3433380000644894917'][1], 'totalArtifacts')), len(notes))
        player = documents(ROOT / 'Assets/Assets/Prefabs/===PLAYER===.prefab')
        self.assertGreaterEqual(int(field(player['5252281844144999244'][1], 'maxSlots')), len(notes) + 2)
        self.assertIn('value: All clues found. Return to the frightened woman.', self.scene['557273661'][1])

    def test_placed_character_and_nervous_clip_are_connected_to_interaction_root(self):
        idle = self.scene['226000004'][1]
        visual = self.scene[reference(idle, 'character')][1]
        instance_id = reference(visual, 'm_PrefabInstance')
        model = self.scene[instance_id][1]
        self.assertIn('    m_TransformParent: {fileID: 226000001}', model)
        self.assertIn('guid: f6cc5544201dc094291bc1ad60c1112f', model)
        self.assertIn('  m_Children:\n  - {fileID: 226000005}', self.scene['226000001'][1])
        self.assertNotIn('{fileID: ' + instance_id + '}', self.scene['9223372036854775807'][1])
        self.assertEqual(len([b for k, b in self.scene.values() if k == 1001 and
                             'guid: f6cc5544201dc094291bc1ad60c1112f' in b]), 1)
        for axis in 'xyz':
            self.assertRegex(model, rf'propertyPath: m_LocalPosition\.{axis}\n      value: 0\n')
        clip = field(idle, 'nervousClip')
        self.assertIn('fileID: 7400000', clip)
        self.assertIn('guid: ba763fd43a5b3374aa7d14e330518b9e', clip)
        self.assertNotIn('characterResource:', idle)
        for name in ['char_npc', 'Nervously Look Around']:
            path = ROOT / f'Assets/Assets/free assets/char_npc/{name}.fbx'
            self.assertTrue(path.is_file())
            meta = Path(str(path) + '.meta').read_text()
            self.assertIn('animationType: 3', meta)
            self.assertIn('avatarSetup: 1', meta)
        self.assertIn('internalID: 7400000', meta)
        self.assertIn('loopTime: 1', meta)
        for axis in ['Orientation', 'PositionY', 'PositionXZ']:
            self.assertIn(f'loopBlend{axis}: 1', meta)

    def test_current_environment_ghost_and_notes_unchanged(self):
        # Hashes of serialized documents from the current level-2-merge
        # 5ef1ec3719f8bb43e3f4a9a5712a70f960857653 base. Geometry and quest wiring stay fixed;
        # RenderSettings are covered by test_level2_atmosphere after intentional darkening.
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
        for key, digest in expected.items():
            body = self.scene[key][1]
            if key == '1186399119':
                body = body.replace('  clueStory: {fileID: 226000003}\n', '')
            self.assertEqual(hashlib.sha256(body.encode()).hexdigest(), digest, key)

    def test_scene_dialogue_matches_defaults_and_reveal_order(self):
        npc = self.scene['226000003'][1]
        source = (ROOT / 'Assets/Scripts/Level2NoteNpc.cs').read_text()
        returned = json.loads(field(npc, 'returnedDialogue'))
        self.assertIn("She's not going to hurt you anymore", returned)
        self.assertIn('public string returnedDialogue = ' + json.dumps(returned) + ';', source)
        pages = [json.loads(line) for line in re.findall(r'^  - (".*")$', npc, re.M)]
        defaults = re.findall(r'^        "(.*)"[,]?$', source, re.M)
        self.assertEqual(pages, defaults)
        self.assertEqual(len(pages), 7)
        for page, phrase in zip(pages, ['serial killer', 'target', 'woman in white',
                "The White Lady wasn't trying to kill you.", 'justice',
                'Because I am the White Lady.', "Renato's victims"]):
            self.assertIn(phrase, page)


if __name__ == '__main__':
    unittest.main()
