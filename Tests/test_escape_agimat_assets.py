"""Regression checks for the scene references used by escape and ghost defense."""
import re
import unittest
from test_collision_assets import ROOT, field, reference
import test_collision_assets as assets
import test_ghost_corner_scare as scare_tests


class EscapeAgimatAssetsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        assets.CollisionAssetsTests.setUpClass()
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]

    def component(self, name):
        return next(b for k, b in self.docs.values()
                    if k == 114 and 'Assembly-CSharp::' + name + '\n' in b)

    def object_name(self, component_id):
        return field(self.docs[reference(self.docs[component_id][1], 'm_GameObject')][1], 'm_Name')

    def test_escape_uses_requested_landmarks_and_only_artifacts(self):
        escape = self.component('EscapeManager')
        self.assertEqual(field(escape, 'requireAllArtifacts'), '1')
        self.assertEqual(field(escape, 'requiredGroups'), '[]')
        self.assertEqual(self.object_name(reference(escape, 'escapeSign')), 'SIGN_COLD_CASE_FILES')
        walls = re.search(r'  exitWalls:\n(.*?)  exitDistance:', escape, re.S)[1]
        wall_ids = re.findall(r'fileID: (\d+)', walls)
        self.assertCountEqual([self.object_name(i) for i in wall_ids],
                             ['ARCH_Wall_Plaster.085', 'ARCH_Wall_Plaster.084 (1)'])
        self.assertTrue(all(self.docs[i][0] == 65 for i in wall_ids))
        self.assertEqual(field(escape, 'nextSceneName'), 'Level2')
        self.assertGreater(float(field(escape, 'fadeDuration')), 0)
        self.assertEqual(self.docs[reference(escape, 'player')][0], 4)

    def test_agimat_pickups_share_consumable_asset_and_warning_lead(self):
        path = ROOT / 'Assets/Assets/Inventory/Item_Consumable.asset'
        self.assertIn('  isAgimat: 1', path.read_text())
        guid = re.search(r'guid: (\w+)', path.with_suffix('.asset.meta').read_text())[1]
        pickups = [b for k, b in self.docs.values() if k == 114
                   and 'promptMessage: Press [E] to pick up Agimat.' in b]
        self.assertGreaterEqual(len(pickups), 1)
        for pickup in pickups:
            self.assertIn(guid, field(pickup, 'itemData'))
            self.assertEqual(field(pickup, 'isArtifact'), '0')
        ghost = self.component('GhostEnemy')
        consumable = self.component('InvisibilityConsumable')
        # At least three seconds of warning for a stationary player.
        lead = (float(field(consumable, 'warningDistance')) - float(field(ghost, 'catchRadius'))) / float(field(ghost, 'moveSpeed'))
        self.assertGreaterEqual(lead, 3)
        self.assertGreaterEqual(float(field(ghost, 'playerSafetyDistance')), float(field(ghost, 'detectionRadius')))
        self.assertGreater(float(field(ghost, 'checkpointGraceDuration')), 0)
        self.assertGreater(float(field(ghost, 'checkpointPatrolRadius')), float(field(ghost, 'playerSafetyDistance')))

    def test_all_scares_and_catch_have_white_lady_scream(self):
        clip = 'd3b7b3c659d59354cb3e17ecbe5fa4fa'
        scares = [b for k, b in self.docs.values() if k == 114 and 'Assembly-CSharp::ArtifactCornerScare' in b]
        self.assertEqual(len(scares), 2)
        self.assertCountEqual([field(b, 'requiredArtifacts') for b in scares], ['1', '2'])
        for scare in scares:
            self.assertIn(clip, field(scare, 'scareAudio'))
            for name in ['cornerSpawn', 'wallExit', 'ghost', 'playerCamera', 'artifactManager']:
                self.assertIn(reference(scare, name), self.docs)
        self.assertIn(clip, field(self.component('GhostAudio'), 'screamClip'))
        self.assertIn(clip, field(self.component('PlayerAudio'), 'caughtClip'))

    def test_new_corner_route_crosses_second_wall(self):
        helper = scare_tests.GhostCornerScareTests()
        helper.docs, helper.components = self.docs, self.components
        scare = next(b for k, b in self.docs.values() if k == 114
                     and 'Assembly-CSharp::ArtifactCornerScare' in b and field(b, 'requiredArtifacts') == '1')
        spawn = helper.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
        end = helper.world(reference(scare, 'wallExit'), [0, 0, 0])
        wall = next(i for i, (k, b) in self.docs.items() if k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.089')
        tr = next(i for i, k, b in self.components[wall] if k == 4)
        center = helper.world(tr, [0, 0, 0])
        self.assertGreater(spawn[0], center[0] + 1)
        self.assertLess(end[0], center[0] - 1)
        self.assertAlmostEqual(spawn[1], 2)
        self.assertAlmostEqual(spawn[2], 44)
        scare_tr = next(b for _, k, b in self.components[reference(scare, 'm_GameObject')] if k == 4)
        parent = self.docs[reference(scare_tr, 'm_Father')][1]
        self.assertIn('fileID: 8800000000000000012', parent)


if __name__ == '__main__':
    unittest.main()
