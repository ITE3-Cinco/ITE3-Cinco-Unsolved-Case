"""Serialized Level 2 integration checks; runtime behavior uses Level2NpcTests."""
from pathlib import Path
import hashlib
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
        self.assertEqual(field(self.scene['226000001'][1], 'm_LocalPosition'), '{x: -84.5, y: -1.74, z: 165}')
        player = self.scene['1487628291'][1]
        self.assertRegex(player, r'propertyPath: interactionText\n      value: *\n      objectReference: \{fileID: 226000012\}')

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

    def test_resources_preserve_original_identity_and_humanoid_import(self):
        for name, guid in [('Ch07_nonPBR', '003ddc0741112ab4c9d611634311be7b'),
                           ('Nervously Look Around', '74f9627af08e5564bb9020d9e2fe5f7a')]:
            path = ROOT / f'Assets/Resources/Level2NPC/{name}.fbx'
            self.assertTrue(path.is_file())
            meta = Path(str(path) + '.meta').read_text()
            self.assertIn('guid: ' + guid, meta)
            self.assertIn('animationType: 3', meta)
            self.assertIn('avatarSetup: 1', meta)

    def test_current_environment_ghost_and_notes_unchanged(self):
        # Hashes of serialized documents from the user-confirmed level-2-merge
        # cf18229 base. No imported or reconstructed historical environment.
        expected = {'1': '72d265b7b2a916d26a2cd17e7d7ea22fc44de6a8aa817e91cb32ef478644a190',
         '1186399119': 'cdbfb0d458c983f25a7f5a385d4d8cd8678761c07e02a0f6ac58a9b0e4b63838',
         '2': '7d6f1b63afb08381b8d5651a9cd16a319a7b38ef565e8becb3d7543743cac3ab',
         '3': '4a8983ae894d61f1925364ad9a576a6b946ea2726a6f6e154e52d28db624a558',
         '4': '8c75158c3eb85863fa14829ba849f7a2e8b87f391e64eeabb1b04361ef8d5d9d',
         '437939276': '1ccc12e32dc5908a59431284dd8a14cbc70d18fb724a1d54ed85cc8a8218f883',
         '8800000000000000032': '3d8117068f44873ac2817516ce10d25abb9302ef038a0d316a00739483370b7e',
         '8800000000000000033': 'a47e9738325d2e0f3394bc22942a6bb9e4745c6bc490fea994d2943362efec57',
         '8800000000000000110': '900f5f4b5018c9e06ed8e0d3c44f4b7f0f806f06ae21c895c4596bca58b3c9a1',
         '8800000000000000111': 'f10ce9e890dfea2dfd6bac46092e44b0b68649898488178171bbf7f39c37bb91',
         '8800000000000000112': '6b933ac56b8d2147905b12314007a7e40b0620c203f2811496238305b055507a',
         '978205674': '230ccdeda9ef4563993690786dddce7a24f81bc92d8588ca981aaeb7c4c01e5e'}
        for key, digest in expected.items():
            self.assertEqual(hashlib.sha256(self.scene[key][1].encode()).hexdigest(), digest, key)


if __name__ == '__main__':
    unittest.main()
