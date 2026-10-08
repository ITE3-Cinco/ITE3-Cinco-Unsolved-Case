"""Scene wiring and placement regressions for the second-artifact encounter."""
import collections
import itertools
import re
import unittest
import test_collision_assets as assets
from test_collision_assets import field, reference


class GhostCornerScareTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        assets.CollisionAssetsTests.setUpClass()
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]
        cls.scares = [(i, b) for i, (k, b) in cls.docs.items()
                      if k == 114 and 'Assembly-CSharp::ArtifactCornerScare' in b]

    def test_second_artifact_trigger_is_wired_to_existing_gameplay(self):
        self.assertEqual(len(self.scares), 2)
        _, scare = next((i, b) for i, b in self.scares if field(b, 'requiredArtifacts') == '2')
        for name, kind in [('artifactManager', 114), ('ghost', 114), ('player', 4),
                           ('playerCamera', 20), ('cornerSpawn', 4), ('wallExit', 4)]:
            self.assertEqual(self.docs[reference(scare, name)][0], kind, name)
        manager = self.docs[reference(scare, 'artifactManager')][1]
        self.assertIn('Assembly-CSharp::ArtifactManager', manager)
        self.assertEqual(field(manager, 'totalArtifacts'), '3')
        self.assertEqual(field(scare, 'requiredArtifacts'), '2')
        self.assertEqual(field(scare, 'm_Enabled'), '1')
        ghost = self.docs[reference(scare, 'ghost')][1]
        self.assertIn('Assembly-CSharp::GhostEnemy', ghost)
        self.assertEqual(field(ghost, 'm_Enabled'), '1')
        self.assertIn('patrolWaypoints:', ghost)
        script_guid = re.search(r'guid: (\w+)',
            (assets.ROOT / 'Assets/Scripts/ArtifactCornerScare.cs.meta').read_text())[1]
        self.assertIn(script_guid, field(scare, 'm_Script'))

    def test_exactly_three_active_artifacts_feed_the_counter(self):
        active = []
        for _, (kind, body) in self.docs.items():
            if kind != 114 or 'Assembly-CSharp::Interactable' not in body or field(body, 'isArtifact') != '1':
                continue
            go = reference(body, 'm_GameObject')
            original = go
            while go:
                if field(self.docs[go][1], 'm_IsActive') != '1':
                    break
                transform = next(b for _, k, b in self.components[go] if k == 4)
                parent = reference(transform, 'm_Father')
                go = reference(self.docs[parent][1], 'm_GameObject') if parent != '0' else None
            else:
                active.append(field(self.docs[original][1], 'm_Name'))
        self.assertCountEqual(active, ['Sketch_Artifact', 'Image_Artifact', 'Witness_Sketch_Note'])

    @staticmethod
    def vector(body, key):
        return [float(v) for v in re.findall(r': ([\d.eE+-]+)', field(body, key))]

    @staticmethod
    def rotate(q, v):
        x, y, z, w = q
        a, b, c = v
        return [(1-2*y*y-2*z*z)*a+(2*x*y-2*z*w)*b+(2*x*z+2*y*w)*c,
                (2*x*y+2*z*w)*a+(1-2*x*x-2*z*z)*b+(2*y*z-2*x*w)*c,
                (2*x*z-2*y*w)*a+(2*y*z+2*x*w)*b+(1-2*x*x-2*y*y)*c]

    def world(self, transform_id, point):
        while transform_id != '0':
            body = self.docs[transform_id][1]
            point = self.rotate(self.vector(body, 'm_LocalRotation'),
                                [a*b for a, b in zip(point, self.vector(body, 'm_LocalScale'))])
            point = [a+b for a, b in zip(point, self.vector(body, 'm_LocalPosition'))]
            transform_id = reference(body, 'm_Father')
        return point

    def test_both_spawns_have_floor_support_and_clear_body_space(self):
        boxes = []
        for go, (kind, body) in self.docs.items():
            if kind != 1:
                continue
            name = field(body, 'm_Name') or ''
            if not name.startswith(('ARCH_', 'CEILING_', 'COL_')):
                continue
            tr = next((i for i, k, b in self.components[go] if k == 4), None)
            if tr is None:
                continue
            for _, k, box in self.components[go]:
                if k != 65 or field(box, 'm_Enabled') != '1' or field(box, 'm_IsTrigger') != '0':
                    continue
                center, size = self.vector(box, 'm_Center'), self.vector(box, 'm_Size')
                corners = [self.world(tr, [c+s*d/2 for c,s,d in zip(center,size,signs)])
                           for signs in itertools.product([-1, 1], repeat=3)]
                boxes.append((name, [min(p[j] for p in corners) for j in range(3)],
                              [max(p[j] for p in corners) for j in range(3)]))
        for _, scare in self.scares:
            point = self.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
            for dx, dz in [(0, 0), (.4, 0), (-.4, 0), (0, .4), (0, -.4)]:
                x, z = point[0]+dx, point[2]+dz
                self.assertTrue(any(name.startswith('ARCH_CorridorFloor') and
                                    lo[0] <= x <= hi[0] and lo[2] <= z <= hi[2] and
                                    abs(point[1]-hi[1]) < .02 for name,lo,hi in boxes),
                                f'Scare has no corridor floor at {point}')
            for name, lo, hi in boxes:
                if name.startswith('ARCH_CorridorFloor'):
                    continue
                self.assertFalse(all(lo[j]-.3 < point[j] < hi[j]+.3 for j in [0, 2]) and
                                 lo[1] < point[1]+1.8 and hi[1] > point[1]+.05,
                                 f'Scare body intersects {name}')

    def test_exit_route_crosses_a_wall_with_spawn_in_corridor(self):
        _, scare = next((i, b) for i, b in self.scares if field(b, 'requiredArtifacts') == '2')
        spawn = self.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
        end = self.world(reference(scare, 'wallExit'), [0, 0, 0])
        wall_go = next(i for i, (k, b) in self.docs.items()
                       if k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.054')
        transform = next(i for i, k, b in self.components[wall_go] if k == 4)
        box = next(b for _, k, b in self.components[wall_go] if k == 65)
        self.assertEqual(field(box, 'm_Enabled'), '1')
        center, size = self.vector(box, 'm_Center'), self.vector(box, 'm_Size')
        corners = [self.world(transform, [c+s*d/2 for c, s, d in zip(center, size, signs)])
                   for signs in itertools.product([-1, 1], repeat=3)]
        lo = [min(p[k] for p in corners) for k in range(3)]
        hi = [max(p[k] for p in corners) for k in range(3)]
        self.assertGreater(spawn[0], hi[0] + 0.5)
        self.assertLess(end[0], lo[0] - 0.5)
        # Markers now represent feet. The upper body must cross the wall,
        # whose collider begins above the floor in this authored layout.
        self.assertGreater(spawn[1] + 2, lo[1])
        self.assertLess(spawn[1], hi[1])
        self.assertGreater(spawn[2], lo[2])
        self.assertLess(spawn[2], hi[2])
        for axis in (1, 2):
            self.assertAlmostEqual(spawn[axis], end[axis])
        self.assertGreater(float(field(scare, 'exitSpeed')), 0)


if __name__ == '__main__':
    unittest.main()
