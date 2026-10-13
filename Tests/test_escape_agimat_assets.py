# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Regression checks for the scene references used by escape and ghost defense."""
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Reuse ROOT, field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import ROOT, field, reference
# Import test_collision_assets as assets for the helpers used below.
import test_collision_assets as assets
# Import test_ghost_corner_scare as scare_tests for the helpers used below.
import test_ghost_corner_scare as scare_tests


class EscapeAgimatAssetsTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Initialize the shared fixture data in assets.CollisionAssetsTests.
        assets.CollisionAssetsTests.setUpClass()
        # Store assets.CollisionAssetsTests.assets[assets.ASSETS[0]] (read from
        # assets.CollisionAssetsTests.assets) in (cls.docs, cls.components).
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]

    # Find the serialized MonoBehaviour with the requested gameplay class name.
    # name is the serialized field or gameplay component name requested by the caller.
    def component(self, name):
        # Return the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 114 and 'Assembly-CSharp::' + name + '\n' in b to the helper caller.
        return next(b for k, b in self.docs.values()
                    if k == 114 and 'Assembly-CSharp::' + name + '\n' in b)

    # Follow a component reference to its owning GameObject name.
    # component_id is a component fileID from the loaded Unity scene documents.
    def object_name(self, component_id):
        # Return serialized field 'm_Name' read from self.docs[reference(self.docs[component_id][1],
        # 'm_GameObject')][1] (read from self.docs[reference(self.docs[component_id][1], 'm_GameObject')]) by
        # test_collision_assets.field to the helper caller.
        return field(self.docs[reference(self.docs[component_id][1], 'm_GameObject')][1], 'm_Name')

    # Check that escape uses requested landmarks and only artifacts.
    def test_escape_uses_requested_landmarks_and_only_artifacts(self):
        # Store the serialized 'EscapeManager' MonoBehaviour found in the test documents in escape.
        escape = self.component('EscapeManager')
        # Expect serialized field 'requireAllArtifacts' read from escape by test_collision_assets.field to equal
        # '1'.
        self.assertEqual(field(escape, 'requireAllArtifacts'), '1')
        # Expect serialized field 'requiredGroups' read from escape by test_collision_assets.field to equal '[]'.
        self.assertEqual(field(escape, 'requiredGroups'), '[]')
        # Expect serialized field 'escapeSign' read from escape by test_collision_assets.field to have no
        # result/reference (None).
        self.assertIsNone(field(escape, 'escapeSign'))
        # Store re.search('  exitWalls:\\n(.*?)  exitDistance:', escape, re.S)[1] (read from re.search('
        # exitWalls:\\n(.*?)  exitDistance:', escape, re.S)) in walls.
        walls = re.search(r'  exitWalls:\n(.*?)  exitDistance:', escape, re.S)[1]
        # Store all regex matches extracted from walls using the pattern written here in wall_ids.
        wall_ids = re.findall(r'fileID: (\d+)', walls)
        # Expect list of self.object_name(i) using i from wall_ids to contain the same entries/counts as
        # ['ARCH_Wall_Plaster.085'].
        self.assertCountEqual([self.object_name(i) for i in wall_ids],
                             ['ARCH_Wall_Plaster.085'])
        # Expect whether every entry satisfies sequence of self.docs[i][0] == 65 using i from wall_ids to be true.
        self.assertTrue(all(self.docs[i][0] == 65 for i in wall_ids))
        # Expect serialized field 'nextSceneName' read from escape by test_collision_assets.field to equal
        # 'Lvl-2'.
        self.assertEqual(field(escape, 'nextSceneName'), 'Lvl-2')
        # Store serialized field 'nextSceneName' read from escape by test_collision_assets.field in destination.
        destination = field(escape, 'nextSceneName')
        # Store text read from file ROOT / 'ProjectSettings/EditorBuildSettings.asset' in build_settings.
        build_settings = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
        # Store all regex matches extracted from build_settings using the pattern written here in enabled_scenes.
        enabled_scenes = re.findall(r'enabled: 1\n    path: (.+)', build_settings)
        # Expect whether at least one entry satisfies sequence of path.endswith('/' + destination + '.unity')
        # using path from enabled_scenes to be true.
        self.assertTrue(any(path.endswith('/' + destination + '.unity') for path in enabled_scenes))
        # Expect whether ROOT / 'Assets/Scenes' / (destination + '.unity') exists as a file to be true.
        self.assertTrue((ROOT / 'Assets/Scenes' / (destination + '.unity')).is_file())
        # Expect serialized field 'fadeDuration' read from escape by test_collision_assets.field converted to a
        # number with decimals to be greater than 0.
        self.assertGreater(float(field(escape, 'fadeDuration')), 0)
        # Expect self.docs[reference(escape, 'player')][0] (read from self.docs[reference(escape, 'player')]) to
        # equal 4.
        self.assertEqual(self.docs[reference(escape, 'player')][0], 4)

    # Check that agimat pickups share consumable asset and warning lead.
    def test_agimat_pickups_share_consumable_asset_and_warning_lead(self):
        # Store path ROOT joined with 'Assets/Assets/Inventory/Item_Consumable.asset' in path.
        path = ROOT / 'Assets/Assets/Inventory/Item_Consumable.asset'
        # Expect '  isAgimat: 1' to appear in text read from file path.
        self.assertIn('  isAgimat: 1', path.read_text())
        # Store re.search('guid: (\\w+)', path.with_suffix('.asset.meta').read_text())[1] (read from
        # re.search('guid: (\\w+)', path.with_suffix('.asset.meta').read_text())) in guid.
        guid = re.search(r'guid: (\w+)', path.with_suffix('.asset.meta').read_text())[1]
        # Store list of b using (k, b) from values stored in self.docs filtered by k == 114 and 'promptMessage:
        # Press [E] to pick up Agimat.' in b in pickups.
        pickups = [b for k, b in self.docs.values() if k == 114
                   and 'promptMessage: Press [E] to pick up Agimat.' in b]
        # Expect length of pickups to be at least 1.
        self.assertGreaterEqual(len(pickups), 1)
        # Process each pickup from pickups.
        for pickup in pickups:
            # Expect guid to appear in serialized field 'itemData' read from pickup by
            # test_collision_assets.field.
            self.assertIn(guid, field(pickup, 'itemData'))
            # Expect serialized field 'isArtifact' read from pickup by test_collision_assets.field to equal '0'.
            self.assertEqual(field(pickup, 'isArtifact'), '0')
        # Store the serialized 'GhostEnemy' MonoBehaviour found in the test documents in ghost.
        ghost = self.component('GhostEnemy')
        # Store the serialized 'InvisibilityConsumable' MonoBehaviour found in the test documents in consumable.
        consumable = self.component('InvisibilityConsumable')
        # At least three seconds of warning for a stationary player.
        # Store (float(field(consumable, 'warningDistance')) - float(field(ghost, 'catchRadius'))) /
        # float(field(ghost, 'moveSpeed')) (calculated from float, field, ghost, consumable) in lead.
        lead = (float(field(consumable, 'warningDistance')) - float(field(ghost, 'catchRadius'))) / float(field(ghost, 'moveSpeed'))
        # Expect lead to be at least 3.
        self.assertGreaterEqual(lead, 3)
        # Expect serialized field 'playerSafetyDistance' read from ghost by test_collision_assets.field converted
        # to a number with decimals to be at least serialized field 'detectionRadius' read from ghost by
        # test_collision_assets.field converted to a number with decimals.
        self.assertGreaterEqual(float(field(ghost, 'playerSafetyDistance')), float(field(ghost, 'detectionRadius')))
        # Expect serialized field 'checkpointGraceDuration' read from ghost by test_collision_assets.field
        # converted to a number with decimals to be greater than 0.
        self.assertGreater(float(field(ghost, 'checkpointGraceDuration')), 0)
        # Expect serialized field 'checkpointPatrolRadius' read from ghost by test_collision_assets.field
        # converted to a number with decimals to be greater than serialized field 'playerSafetyDistance' read from
        # ghost by test_collision_assets.field converted to a number with decimals.
        self.assertGreater(float(field(ghost, 'checkpointPatrolRadius')), float(field(ghost, 'playerSafetyDistance')))

    # Check that all scares and catch have white lady scream.
    def test_all_scares_and_catch_have_white_lady_scream(self):
        # Initialize clip with 'd3b7b3c659d59354cb3e17ecbe5fa4fa'.
        clip = 'd3b7b3c659d59354cb3e17ecbe5fa4fa'
        # Store list of b using (k, b) from values stored in self.docs filtered by k == 114 and
        # 'Assembly-CSharp::ArtifactCornerScare' in b in scares.
        scares = [b for k, b in self.docs.values() if k == 114 and 'Assembly-CSharp::ArtifactCornerScare' in b]
        # Expect length of scares to equal 2.
        self.assertEqual(len(scares), 2)
        # Expect list of field(b, 'requiredArtifacts') using b from scares to contain the same entries/counts as
        # ['1', '2'].
        self.assertCountEqual([field(b, 'requiredArtifacts') for b in scares], ['1', '2'])
        # Process each scare from scares.
        for scare in scares:
            # Expect clip to appear in serialized field 'scareAudio' read from scare by
            # test_collision_assets.field.
            self.assertIn(clip, field(scare, 'scareAudio'))
            # Process each name from ['cornerSpawn', 'wallExit', 'ghost', 'playerCamera', 'artifactManager'].
            for name in ['cornerSpawn', 'wallExit', 'ghost', 'playerCamera', 'artifactManager']:
                # Expect fileID referenced by field name in scare via test_collision_assets.reference to appear in
                # Unity documents keyed by fileID, prepared in setUpClass.
                self.assertIn(reference(scare, name), self.docs)
        # Expect clip to appear in serialized field 'screamClip' read from the serialized 'GhostAudio'
        # MonoBehaviour found in the test documents by test_collision_assets.field.
        self.assertIn(clip, field(self.component('GhostAudio'), 'screamClip'))
        # Expect clip to appear in serialized field 'caughtClip' read from the serialized 'PlayerAudio'
        # MonoBehaviour found in the test documents by test_collision_assets.field.
        self.assertIn(clip, field(self.component('PlayerAudio'), 'caughtClip'))

    # Check that new corner route crosses second wall.
    def test_new_corner_route_crosses_second_wall(self):
        # Store the result of scare_tests.GhostCornerScareTests in helper.
        helper = scare_tests.GhostCornerScareTests()
        # Store (self.docs, self.components) in (helper.docs, helper.components).
        helper.docs, helper.components = self.docs, self.components
        # Store the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 114 and 'Assembly-CSharp::ArtifactCornerScare' in b and (field(b, 'requiredArtifacts') == '1')
        # in scare.
        scare = next(b for k, b in self.docs.values() if k == 114
                     and 'Assembly-CSharp::ArtifactCornerScare' in b and field(b, 'requiredArtifacts') == '1')
        # Store world point calculated from transform ID fileID referenced by field 'cornerSpawn' in scare via
        # test_collision_assets.reference and local point [0, 0, 0] by the scare-test helper in spawn.
        spawn = helper.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
        # Store world point calculated from transform ID fileID referenced by field 'wallExit' in scare via
        # test_collision_assets.reference and local point [0, 0, 0] by the scare-test helper in end.
        end = helper.world(reference(scare, 'wallExit'), [0, 0, 0])
        # Store the first matching entry from sequence of i using (i, (k, b)) from key/value pairs stored in
        # self.docs filtered by k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.089' in wall.
        wall = next(i for i, (k, b) in self.docs.items() if k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.089')
        # Store the first matching entry from sequence of i using (i, k, b) from self.components[wall] (read from
        # self.components) filtered by k == 4 in tr.
        tr = next(i for i, k, b in self.components[wall] if k == 4)
        # Store world point calculated from transform ID tr and local point [0, 0, 0] by the scare-test helper in
        # center.
        center = helper.world(tr, [0, 0, 0])
        # Expect spawn[0] (read from spawn) to be greater than center[0] + 1 (calculated from center).
        self.assertGreater(spawn[0], center[0] + 1)
        # Expect end[0] (read from end) to be below center[0] - 1 (calculated from center).
        self.assertLess(end[0], center[0] - 1)
        # Expect spawn[1] (read from spawn) to approximately equal 0.32.
        self.assertAlmostEqual(spawn[1], .32)
        # Expect spawn[2] (read from spawn) to approximately equal 35.
        self.assertAlmostEqual(spawn[2], 35)
        # Store the first matching entry from sequence of b using (_, k, b) from self.components[reference(scare,
        # 'm_GameObject')] (read from self.components) filtered by k == 4 in scare_tr.
        scare_tr = next(b for _, k, b in self.components[reference(scare, 'm_GameObject')] if k == 4)
        # Store self.docs[reference(scare_tr, 'm_Father')][1] (read from self.docs[reference(scare_tr,
        # 'm_Father')]) in parent.
        parent = self.docs[reference(scare_tr, 'm_Father')][1]
        # Expect 'fileID: 8800000000000000012' to appear in parent.
        self.assertIn('fileID: 8800000000000000012', parent)


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
