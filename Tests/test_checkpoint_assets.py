# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Checkpoint wiring checks for PoliceHQ and the reusable environment prefab."""
# Import test_collision_assets as collision_assets for the helpers used below.
import test_collision_assets as collision_assets
# Reuse field, reference, ASSETS from Tests/test_collision_assets.py; that module defines repository paths and
# Unity field/fileID parsing.
from test_collision_assets import field, reference, ASSETS
# Import unittest for running the test cases and reporting assertion failures.
import unittest


class CheckpointAssetsTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Initialize the shared fixture data in collision_assets.CollisionAssetsTests.
        collision_assets.CollisionAssetsTests.setUpClass()
        # Store collision_assets.CollisionAssetsTests.assets in cls.assets.
        cls.assets = collision_assets.CollisionAssetsTests.assets

    # Check that every map has an enabled checkpoint interaction and spawn.
    def test_every_map_has_an_enabled_checkpoint_interaction_and_spawn(self):
        # Process each (path, (documents, components)) from key/value pairs stored in self.assets.
        for path, (documents, components) in self.assets.items():
            # Store list of (key, body) using (key, (kind, body)) from key/value pairs stored in documents
            # filtered by kind == 1 and (field(body, 'm_Name') or '').startswith('Map Holder') in maps.
            maps = [(key, body) for key, (kind, body) in documents.items()
                    if kind == 1 and (field(body, 'm_Name') or '').startswith('Map Holder')]
            # Expect length of maps to equal 4.
            self.assertEqual(len(maps), 4)
            # Process each (key, body) from maps.
            for key, body in maps:
                # Label this asset/object combination as a separate unittest subtest so a failure identifies the
                # exact fixture.
                with self.subTest(asset=path, map=field(body, 'm_Name')):
                    # Store list of (i, b) using (i, k, b) from components[key] (read from components) filtered by
                    # k == 114 in behaviours.
                    behaviours = [(i, b) for i, k, b in components[key] if k == 114]
                    # Store list of (i, b) using (i, b) from behaviours filtered by
                    # 'Assembly-CSharp::RespawnPoint' in b in checkpoints.
                    checkpoints = [(i, b) for i, b in behaviours if 'Assembly-CSharp::RespawnPoint' in b]
                    # Store list of (i, b) using (i, b) from behaviours filtered by
                    # 'Assembly-CSharp::Interactable' in b in interactions.
                    interactions = [(i, b) for i, b in behaviours if 'Assembly-CSharp::Interactable' in b]
                    # Expect length of checkpoints to equal 1.
                    self.assertEqual(len(checkpoints), 1)
                    # Expect length of interactions to equal 1.
                    self.assertEqual(len(interactions), 1)
                    # Store checkpoints[0] (read from checkpoints) in (checkpoint_id, checkpoint).
                    checkpoint_id, checkpoint = checkpoints[0]
                    # Store interactions[0] (read from interactions) in (interact_id, interact).
                    interact_id, interact = interactions[0]
                    # Process each (component_id, behaviour) from (checkpoints[0], interactions[0]).
                    for component_id, behaviour in (checkpoints[0], interactions[0]):
                        # Expect serialized field 'm_Enabled' read from behaviour by test_collision_assets.field
                        # to equal '1'.
                        self.assertEqual(field(behaviour, 'm_Enabled'), '1')
                        # Expect formatted text inserting component_id to appear in body.
                        self.assertIn(f'component: {{fileID: {component_id}}}', body)
                    # Expect serialized field 'isCheckpoint' read from interact by test_collision_assets.field to
                    # equal '1'.
                    self.assertEqual(field(interact, 'isCheckpoint'), '1')
                    # Expect fileID referenced by field 'checkpoint' in interact via
                    # test_collision_assets.reference to equal checkpoint_id.
                    self.assertEqual(reference(interact, 'checkpoint'), checkpoint_id)
                    # Expect serialized field 'isInitialSpawn' read from checkpoint by test_collision_assets.field
                    # to equal '0'.
                    self.assertEqual(field(checkpoint, 'isInitialSpawn'), '0')
                    # Expect fileID referenced by field 'itemData' in interact via test_collision_assets.reference
                    # to equal '0'.
                    self.assertEqual(reference(interact, 'itemData'), '0')
                    # Expect serialized field 'isArtifact' read from interact by test_collision_assets.field to
                    # equal '0'.
                    self.assertEqual(field(interact, 'isArtifact'), '0')
                    # Store fileID referenced by field 'checkpointSpawnPoint' in checkpoint via
                    # test_collision_assets.reference in spawn_id.
                    spawn_id = reference(checkpoint, 'checkpointSpawnPoint')
                    # Store documents[spawn_id] (read from documents) in (kind, spawn).
                    kind, spawn = documents[spawn_id]
                    # Expect kind to equal 4.
                    self.assertEqual(kind, 4)
                    # Store the first matching entry from sequence of i using (i, k, b) from components[key] (read
                    # from components) filtered by k == 4 in holder_transform.
                    holder_transform = next(i for i, k, b in components[key] if k == 4)
                    # Expect spawn_id to differ from holder_transform.
                    self.assertNotEqual(spawn_id, holder_transform)
                    # Expect fileID referenced by field 'm_Father' in spawn via test_collision_assets.reference to
                    # equal holder_transform.
                    self.assertEqual(reference(spawn, 'm_Father'), holder_transform)
                    # Expect formatted text inserting spawn_id to appear in documents[holder_transform][1] (read
                    # from documents[holder_transform]).
                    self.assertIn(f'{{fileID: {spawn_id}}}', documents[holder_transform][1])
                    # Store list of (i, b) using (i, k, b) from components[key] (read from components) filtered by
                    # k == 65 and field(b, 'm_Enabled') == '1' in colliders.
                    colliders = [(i, b) for i, k, b in components[key]
                                 if k == 65 and field(b, 'm_Enabled') == '1']
                    # Expect length of colliders to equal 1.
                    self.assertEqual(len(colliders), 1)
                    # Store colliders[0] (read from colliders) in (collider_id, collider).
                    collider_id, collider = colliders[0]
                    # Expect formatted text inserting collider_id to appear in body.
                    self.assertIn(f'component: {{fileID: {collider_id}}}', body)
                    # Expect serialized field 'm_IsTrigger' read from collider by test_collision_assets.field to
                    # equal '1'.
                    self.assertEqual(field(collider, 'm_IsTrigger'), '1')
                    # Expect serialized field 'm_Size' read from collider by test_collision_assets.field to equal
                    # '{x: 1, y: 1, z: 1}'.
                    self.assertEqual(field(collider, 'm_Size'), '{x: 1, y: 1, z: 1}')
                    # Expect serialized field 'm_Center' read from collider by test_collision_assets.field to
                    # equal '{x: 0, y: 0, z: 0}'.
                    self.assertEqual(field(collider, 'm_Center'), '{x: 0, y: 0, z: 0}')

    # Check that player fallback does not reference a wall map.
    def test_player_fallback_does_not_reference_a_wall_map(self):
        # Store self.assets[ASSETS[0]] (read from self.assets) in (documents, _).
        documents, _ = self.assets[ASSETS[0]]
        # Store list of b using (kind, b) from values stored in documents filtered by kind == 114 and
        # 'Assembly-CSharp::PlayerRespawn' in b in respawns.
        respawns = [b for kind, b in documents.values()
                    if kind == 114 and 'Assembly-CSharp::PlayerRespawn' in b]
        # Expect length of respawns to equal 1.
        self.assertEqual(len(respawns), 1)
        # An empty reference uses the player's cached starting pose.
        # Expect fileID referenced by field 'defaultSpawnPoint' in respawns[0] (read from respawns) via
        # test_collision_assets.reference to equal '0'.
        self.assertEqual(reference(respawns[0], 'defaultSpawnPoint'), '0')


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
