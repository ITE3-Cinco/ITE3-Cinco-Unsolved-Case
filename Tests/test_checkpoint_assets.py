"""Checkpoint wiring checks for PoliceHQ and the reusable environment prefab."""
import test_collision_assets as collision_assets
from test_collision_assets import field, reference, ASSETS
import unittest


class CheckpointAssetsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        collision_assets.CollisionAssetsTests.setUpClass()
        cls.assets = collision_assets.CollisionAssetsTests.assets

    def test_every_map_has_an_enabled_checkpoint_interaction_and_spawn(self):
        for path, (documents, components) in self.assets.items():
            maps = [(key, body) for key, (kind, body) in documents.items()
                    if kind == 1 and (field(body, 'm_Name') or '').startswith('Map Holder')]
            self.assertEqual(len(maps), 4)
            for key, body in maps:
                with self.subTest(asset=path, map=field(body, 'm_Name')):
                    behaviours = [(i, b) for i, k, b in components[key] if k == 114]
                    checkpoints = [(i, b) for i, b in behaviours if 'Assembly-CSharp::RespawnPoint' in b]
                    interactions = [(i, b) for i, b in behaviours if 'Assembly-CSharp::Interactable' in b]
                    self.assertEqual(len(checkpoints), 1)
                    self.assertEqual(len(interactions), 1)
                    checkpoint_id, checkpoint = checkpoints[0]
                    interact_id, interact = interactions[0]
                    for component_id, behaviour in (checkpoints[0], interactions[0]):
                        self.assertEqual(field(behaviour, 'm_Enabled'), '1')
                        self.assertIn(f'component: {{fileID: {component_id}}}', body)
                    self.assertEqual(field(interact, 'isCheckpoint'), '1')
                    self.assertEqual(reference(interact, 'checkpoint'), checkpoint_id)
                    self.assertEqual(field(checkpoint, 'isInitialSpawn'), '0')
                    self.assertEqual(reference(interact, 'itemData'), '0')
                    self.assertEqual(field(interact, 'isArtifact'), '0')
                    spawn_id = reference(checkpoint, 'checkpointSpawnPoint')
                    kind, spawn = documents[spawn_id]
                    self.assertEqual(kind, 4)
                    holder_transform = next(i for i, k, b in components[key] if k == 4)
                    self.assertNotEqual(spawn_id, holder_transform)
                    self.assertEqual(reference(spawn, 'm_Father'), holder_transform)
                    self.assertIn(f'{{fileID: {spawn_id}}}', documents[holder_transform][1])
                    colliders = [(i, b) for i, k, b in components[key]
                                 if k == 65 and field(b, 'm_Enabled') == '1']
                    self.assertEqual(len(colliders), 1)
                    collider_id, collider = colliders[0]
                    self.assertIn(f'component: {{fileID: {collider_id}}}', body)
                    self.assertEqual(field(collider, 'm_IsTrigger'), '1')
                    self.assertEqual(field(collider, 'm_Size'), '{x: 1, y: 1, z: 1}')
                    self.assertEqual(field(collider, 'm_Center'), '{x: 0, y: 0, z: 0}')

    def test_player_fallback_does_not_reference_a_wall_map(self):
        documents, _ = self.assets[ASSETS[0]]
        respawns = [b for kind, b in documents.values()
                    if kind == 114 and 'Assembly-CSharp::PlayerRespawn' in b]
        self.assertEqual(len(respawns), 1)
        # An empty reference uses the player's cached starting pose.
        self.assertEqual(reference(respawns[0], 'defaultSpawnPoint'), '0')


if __name__ == '__main__':
    unittest.main()
