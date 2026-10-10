"""Level 2 serialized integration. Runtime behavior is covered by Unity tests."""
import re
import struct
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def read(path):
    return (ROOT / path).read_text()


def guid(path):
    return re.search(r'guid: (\w+)', read(path + '.meta'))[1]


def documents(text):
    return re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)


class Level2NoteAssetsTests(unittest.TestCase):
    def test_scene_has_no_dangling_local_references(self):
        for path in ['Assets/Scenes/Lvl-2.unity', 'Assets/Assets/Prefabs/Level2Note.prefab',
                     'Assets/Assets/Prefabs/Level2NoteNpc.prefab', 'Assets/Assets/Prefabs/WhiteLady.prefab']:
            with self.subTest(path=path):
                text = read(path)
                blocks = documents(text)
                ids = {key for _, key, _ in blocks}
                self.assertEqual(len(ids), len(blocks), 'Duplicate document IDs')
                for ref in re.findall(r'\{fileID: (-?\d+)\}', text):
                    self.assertTrue(ref == '0' or ref in ids, f'{path}: missing {ref}')

    def test_three_notes_use_only_the_existing_note_and_quest(self):
        scene = read('Assets/Scenes/Lvl-2.unity')
        note_guid = guid('Assets/Assets/Prefabs/Level2Note.prefab')
        instances = [b for k, _, b in documents(scene) if k == '1001'
                     and f'm_SourcePrefab: {{fileID: 100100000, guid: {note_guid}' in b]
        self.assertEqual(len(instances), 3)
        self.assertNotIn(guid('Assets/Assets/Prefabs/=== ITEM ===.prefab'), scene)
        for instance in instances:
            self.assertRegex(instance, r'propertyPath: noteQuest\n      value: *\n      objectReference: \{fileID: 220000012\}')
        note = read('Assets/Assets/Prefabs/Level2Note.prefab')
        self.assertIn(guid('Assets/Assets/Inventory/Item_Note.asset'), note)
        self.assertIn('  isArtifact: 1', note)
        self.assertIn('  m_IsTrigger: 1', note)
        self.assertIn(guid('Assets/Assets/Images/Materials/scan-2026-09-11-03.mat'), note)
        self.assertIn('propertyPath: stackableItems.Array.data[0]', scene)
        quest = next(b for _, _, b in documents(scene) if '  notes:' in b)
        self.assertEqual(len(re.findall(r'^  - \{fileID:', quest, re.M)), 3)

    def test_environment_is_restored_with_the_original_guid_and_real_geometry(self):
        asset = 'Assets/Assets/free assets/Level 2 - Backrooms Map/backrooms_with_baked_textures.glb'
        self.assertEqual(guid(asset), 'b8a9d750d669c9d498e803a6a2bd4934')
        data = (ROOT / asset).read_bytes()
        self.assertEqual(data[:4], b'glTF')
        self.assertEqual(struct.unpack_from('<I', data, 8)[0], len(data))
        scene = read('Assets/Scenes/Lvl-2.unity')
        self.assertEqual(len([1 for k, _, _ in documents(scene) if k == '64']), 23)
        self.assertIn('  environmentRoot: {fileID: 220000007}', scene)

    def test_white_lady_reuses_level_one_components_and_assets(self):
        ghost = read('Assets/Assets/Prefabs/WhiteLady.prefab')
        for script in ['GhostEnemy', 'GhostAudio', 'GhostAnimation']:
            self.assertIn(guid(f'Assets/Scripts/{script}.cs'), ghost)
        self.assertIn('NavMeshAgent:', ghost)
        self.assertIn('  requireNavigation: 1', ghost)
        self.assertIn('  requireLineOfSight: 1', ghost)
        self.assertIn('  artifactsBeforeHunt: 1', ghost)
        self.assertIn('  screamClip: {fileID: 8300000,', ghost)
        self.assertIn('  hummingClip: {fileID: 8300000,', ghost)
        scene = read('Assets/Scenes/Lvl-2.unity')
        self.assertIn('propertyPath: patrolWaypoints.Array.size\n      value: 5', scene)
        self.assertIn('propertyPath: loseSceneController\n      value:\n      objectReference: {fileID: 220000003}', scene)

    def test_npc_resources_and_progression_are_wired(self):
        npc = read('Assets/Assets/Prefabs/Level2NoteNpc.prefab')
        for field in ['characterResource', 'animationResource']:
            resource = re.search(r'  ' + field + r': (.+)', npc)[1]
            self.assertTrue((ROOT / 'Assets/Resources' / (resource + '.fbx')).exists())
            meta = read('Assets/Resources/' + resource + '.fbx.meta')
            self.assertIn('  animationType: 3', meta)
            self.assertIn('  avatarSetup: 1', meta)
        scene = read('Assets/Scenes/Lvl-2.unity')
        self.assertIn('propertyPath: quest\n      value:\n      objectReference: {fileID: 220000012}', scene)
        self.assertIn('propertyPath: retrySceneName\n      value: Lvl-2', scene)
        self.assertIn('  nextSceneName: Level3', scene)
        build = read('ProjectSettings/EditorBuildSettings.asset')
        self.assertIn('enabled: 1\n    path: Assets/Scenes/Lvl-2.unity', build)
        self.assertIn('  nextSceneName: Lvl-2', read('Assets/Scenes/PoliceHQ.unity'))


if __name__ == '__main__':
    unittest.main()
