# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Scene wiring and placement regressions for the second-artifact encounter."""
# Import collections for grouping components by their GameObject IDs.
import collections
# Import itertools for generating all box-corner sign combinations.
import itertools
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Import test_collision_assets as assets for the helpers used below.
import test_collision_assets as assets
# Reuse field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import field, reference


class GhostCornerScareTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Initialize the shared fixture data in assets.CollisionAssetsTests.
        assets.CollisionAssetsTests.setUpClass()
        # Store assets.CollisionAssetsTests.assets[assets.ASSETS[0]] (read from
        # assets.CollisionAssetsTests.assets) in (cls.docs, cls.components).
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]
        # Store list of (i, b) using (i, (k, b)) from key/value pairs stored in cls.docs filtered by k == 114 and
        # 'Assembly-CSharp::ArtifactCornerScare' in b in cls.scares.
        cls.scares = [(i, b) for i, (k, b) in cls.docs.items()
                      if k == 114 and 'Assembly-CSharp::ArtifactCornerScare' in b]

    # Check that second artifact trigger is wired to existing gameplay.
    def test_second_artifact_trigger_is_wired_to_existing_gameplay(self):
        # Expect length of self.scares to equal 2.
        self.assertEqual(len(self.scares), 2)
        # Store the first matching entry from sequence of (i, b) using (i, b) from self.scares filtered by
        # field(b, 'requiredArtifacts') == '2' in (_, scare).
        _, scare = next((i, b) for i, b in self.scares if field(b, 'requiredArtifacts') == '2')
        # Process each (name, kind) from [('artifactManager', 114), ('ghost', 114), ('player', 4),
        # ('playerCamera', 20), ('cornerSpawn', 4), ('wallExit', 4)].
        for name, kind in [('artifactManager', 114), ('ghost', 114), ('player', 4),
                           ('playerCamera', 20), ('cornerSpawn', 4), ('wallExit', 4)]:
            # Expect self.docs[reference(scare, name)][0] (read from self.docs[reference(scare, name)]) to equal
            # kind.
            self.assertEqual(self.docs[reference(scare, name)][0], kind, name)
        # Store self.docs[reference(scare, 'artifactManager')][1] (read from self.docs[reference(scare,
        # 'artifactManager')]) in manager.
        manager = self.docs[reference(scare, 'artifactManager')][1]
        # Expect 'Assembly-CSharp::ArtifactManager' to appear in manager.
        self.assertIn('Assembly-CSharp::ArtifactManager', manager)
        # Expect serialized field 'totalArtifacts' read from manager by test_collision_assets.field to equal '3'.
        self.assertEqual(field(manager, 'totalArtifacts'), '3')
        # Expect serialized field 'requiredArtifacts' read from scare by test_collision_assets.field to equal '2'.
        self.assertEqual(field(scare, 'requiredArtifacts'), '2')
        # Expect serialized field 'm_Enabled' read from scare by test_collision_assets.field to equal '1'.
        self.assertEqual(field(scare, 'm_Enabled'), '1')
        # Store self.docs[reference(scare, 'ghost')][1] (read from self.docs[reference(scare, 'ghost')]) in ghost.
        ghost = self.docs[reference(scare, 'ghost')][1]
        # Expect 'Assembly-CSharp::GhostEnemy' to appear in ghost.
        self.assertIn('Assembly-CSharp::GhostEnemy', ghost)
        # Expect serialized field 'm_Enabled' read from ghost by test_collision_assets.field to equal '1'.
        self.assertEqual(field(ghost, 'm_Enabled'), '1')
        # Expect 'patrolWaypoints:' to appear in ghost.
        self.assertIn('patrolWaypoints:', ghost)
        # Store re.search('guid: (\\w+)', (assets.ROOT /
        # 'Assets/Scripts/ArtifactCornerScare.cs.meta').read_text())[1] (read from re.search('guid: (\\w+)',
        # (assets.ROOT / 'Assets/Scripts/ArtifactCornerScare.cs.meta').read_text())) in script_guid.
        script_guid = re.search(r'guid: (\w+)',
            (assets.ROOT / 'Assets/Scripts/ArtifactCornerScare.cs.meta').read_text())[1]
        # Expect script_guid to appear in serialized field 'm_Script' read from scare by
        # test_collision_assets.field.
        self.assertIn(script_guid, field(scare, 'm_Script'))

    # Check that exactly three active artifacts feed the counter.
    def test_exactly_three_active_artifacts_feed_the_counter(self):
        # Store an empty list in active.
        active = []
        # Process each (_, (kind, body)) from key/value pairs stored in self.docs.
        for _, (kind, body) in self.docs.items():
            # Run this branch only if kind differs from 114 or 'Assembly-CSharp::Interactable' does not appear in
            # body or serialized field 'isArtifact' read from body by test_collision_assets.field differs from
            # '1'.
            if kind != 114 or 'Assembly-CSharp::Interactable' not in body or field(body, 'isArtifact') != '1':
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Store fileID referenced by field 'm_GameObject' in body via test_collision_assets.reference in go.
            go = reference(body, 'm_GameObject')
            # Store go in original.
            original = go
            # Repeat the following steps while go.
            while go:
                # Run this branch only if serialized field 'm_IsActive' read from self.docs[go][1] (read from
                # self.docs[go]) by test_collision_assets.field differs from '1'.
                if field(self.docs[go][1], 'm_IsActive') != '1':
                    # Stop this loop early; the remaining entries are not processed.
                    break
                # Store the first matching entry from sequence of b using (_, k, b) from self.components[go] (read
                # from self.components) filtered by k == 4 in transform.
                transform = next(b for _, k, b in self.components[go] if k == 4)
                # Store fileID referenced by field 'm_Father' in transform via test_collision_assets.reference in
                # parent.
                parent = reference(transform, 'm_Father')
                # Store fileID referenced by field 'm_GameObject' in self.docs[parent][1] (read from
                # self.docs[parent]) via test_collision_assets.reference when parent differs from '0'; otherwise
                # None (no result/reference) in go.
                go = reference(self.docs[parent][1], 'm_GameObject') if parent != '0' else None
            else:
                # Append serialized field 'm_Name' read from self.docs[original][1] (read from
                # self.docs[original]) by test_collision_assets.field to active for later lookup or binary output.
                active.append(field(self.docs[original][1], 'm_Name'))
        # Expect active to contain the same entries/counts as ['Sketch_Artifact', 'Image_Artifact',
        # 'Witness_Sketch_Note'].
        self.assertCountEqual(active, ['Sketch_Artifact', 'Image_Artifact', 'Witness_Sketch_Note'])

    # Parse a serialized vector or quaternion into a list of numeric values.
    @staticmethod
    # body is one Unity YAML document supplied by the test/helper caller. key is the serialized vector field name
    # requested by the caller.
    def vector(body, key):
        # Return list of float(v) using v from all regex matches extracted from serialized field key read from
        # body by test_collision_assets.field using the pattern written here to the helper caller.
        return [float(v) for v in re.findall(r': ([\d.eE+-]+)', field(body, key))]

    # Apply quaternion rotation to a local point without needing the Unity runtime.
    @staticmethod
    # q is the four quaternion components read from a Unity Transform m_LocalRotation field. v is the local
    # point/vector supplied by the world-position helper.
    def rotate(q, v):
        # Store q in (x, y, z, w).
        x, y, z, w = q
        # Store v in (a, b, c).
        a, b, c = v
        # Return [(1 - 2 * y * y - 2 * z * z) * a + (2 * x * y - 2 * z * w) * b + (2 * x * z + 2 * y * w) * c, (2
        # * x * y + 2 * z * w) * a + (1 - 2 * x * x - 2 * z * z) * b + (2 * y * z - 2 * x * w) * c, (2 * x * z - 2
        # * y * w) * a + (2 * y * z + 2 * x * w) * b + (1 - 2 * x * x - 2 * y * y) * c] to the helper caller.
        return [(1-2*y*y-2*z*z)*a+(2*x*y-2*z*w)*b+(2*x*z+2*y*w)*c,
                (2*x*y+2*z*w)*a+(1-2*x*x-2*z*z)*b+(2*y*z-2*x*w)*c,
                (2*x*z-2*y*w)*a+(2*y*z+2*x*w)*b+(1-2*x*x-2*y*y)*c]

    # Walk up transform parents, applying scale, rotation and position to get a world point.
    # transform_id is a Unity Transform fileID from the scene documents. point is the local coordinate the caller
    # wants converted into a world point.
    def world(self, transform_id, point):
        # Repeat the following steps while transform_id differs from '0'.
        while transform_id != '0':
            # Store self.docs[transform_id][1] (read from self.docs[transform_id]) in body.
            body = self.docs[transform_id][1]
            # Store point list of a * b using (a, b) from corresponding entries paired from point, numeric vector
            # components parsed from field 'm_LocalScale' of body rotated by quaternion numeric vector components
            # parsed from field 'm_LocalRotation' of body in point.
            point = self.rotate(self.vector(body, 'm_LocalRotation'),
                                [a*b for a, b in zip(point, self.vector(body, 'm_LocalScale'))])
            # Store list of a + b using (a, b) from corresponding entries paired from point, numeric vector
            # components parsed from field 'm_LocalPosition' of body in point.
            point = [a+b for a, b in zip(point, self.vector(body, 'm_LocalPosition'))]
            # Store fileID referenced by field 'm_Father' in body via test_collision_assets.reference in
            # transform_id.
            transform_id = reference(body, 'm_Father')
        # Return point to the helper caller.
        return point

    # Check that both spawns have floor support and clear body space.
    def test_both_spawns_have_floor_support_and_clear_body_space(self):
        # Store an empty list in boxes.
        boxes = []
        # Process each (go, (kind, body)) from key/value pairs stored in self.docs.
        for go, (kind, body) in self.docs.items():
            # Run this branch only if kind differs from 1.
            if kind != 1:
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Store serialized field 'm_Name' read from body by test_collision_assets.field or '' in name.
            name = field(body, 'm_Name') or ''
            # Run this branch only if not (name starts with ('ARCH_', 'CEILING_', 'COL_')).
            if not name.startswith(('ARCH_', 'CEILING_', 'COL_')):
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Store the first matching entry from sequence of i using (i, k, b) from self.components[go] (read
            # from self.components) filtered by k == 4 in tr.
            tr = next((i for i, k, b in self.components[go] if k == 4), None)
            # Run this branch only if tr is None (no result/reference).
            if tr is None:
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Process each (_, k, box) from self.components[go] (read from self.components).
            for _, k, box in self.components[go]:
                # Run this branch only if k differs from 65 or serialized field 'm_Enabled' read from box by
                # test_collision_assets.field differs from '1' or serialized field 'm_IsTrigger' read from box by
                # test_collision_assets.field differs from '0'.
                if k != 65 or field(box, 'm_Enabled') != '1' or field(box, 'm_IsTrigger') != '0':
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Store (self.vector(box, 'm_Center'), self.vector(box, 'm_Size')) in (center, size).
                center, size = self.vector(box, 'm_Center'), self.vector(box, 'm_Size')
                # Store list of self.world(tr, [c + s * d / 2 for (c, s, d) in zip(center, size, signs)]) using
                # signs from the result of itertools.product using [-1, 1] in corners.
                corners = [self.world(tr, [c+s*d/2 for c,s,d in zip(center,size,signs)])
                           for signs in itertools.product([-1, 1], repeat=3)]
                # Append (name, [min((p[j] for p in corners)) for j in range(3)], [max((p[j] for p in corners))
                # for j in range(3)]) to boxes for later lookup or binary output.
                boxes.append((name, [min(p[j] for p in corners) for j in range(3)],
                              [max(p[j] for p in corners) for j in range(3)]))
        # Process each (_, scare) from self.scares.
        for _, scare in self.scares:
            # Store world point calculated from transform ID fileID referenced by field 'cornerSpawn' in scare via
            # test_collision_assets.reference and local point [0, 0, 0] by the scare-test helper in point.
            point = self.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
            # Process each (dx, dz) from [(0, 0), (0.4, 0), (-0.4, 0), (0, 0.4), (0, -0.4)].
            for dx, dz in [(0, 0), (.4, 0), (-.4, 0), (0, .4), (0, -.4)]:
                # Store (point[0] + dx, point[2] + dz) in (x, z).
                x, z = point[0]+dx, point[2]+dz
                # Expect whether at least one entry satisfies sequence of name.startswith('ARCH_CorridorFloor')
                # and lo[0] <= x <= hi[0] and (lo[2] <= z <= hi[2]) and (abs(point[1] - hi[1]) < 0.02) using
                # (name, lo, hi) from boxes to be true.
                self.assertTrue(any(name.startswith('ARCH_CorridorFloor') and
                                    lo[0] <= x <= hi[0] and lo[2] <= z <= hi[2] and
                                    abs(point[1]-hi[1]) < .02 for name,lo,hi in boxes),
                                f'Scare has no corridor floor at {point}')
            # Process each (name, lo, hi) from boxes.
            for name, lo, hi in boxes:
                # Run this branch only if whether name starts with 'ARCH_CorridorFloor'.
                if name.startswith('ARCH_CorridorFloor'):
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Expect whether every entry satisfies sequence of lo[j] - 0.3 < point[j] < hi[j] + 0.3 using j
                # from [0, 2] and lo[1] (read from lo) is below point[1] + 1.8 (calculated from point) and hi[1]
                # (read from hi) is above point[1] + 0.05 (calculated from point) to be false.
                self.assertFalse(all(lo[j]-.3 < point[j] < hi[j]+.3 for j in [0, 2]) and
                                 lo[1] < point[1]+1.8 and hi[1] > point[1]+.05,
                                 f'Scare body intersects {name}')

    # Check that exit route crosses a wall with spawn in corridor.
    def test_exit_route_crosses_a_wall_with_spawn_in_corridor(self):
        # Store the first matching entry from sequence of (i, b) using (i, b) from self.scares filtered by
        # field(b, 'requiredArtifacts') == '2' in (_, scare).
        _, scare = next((i, b) for i, b in self.scares if field(b, 'requiredArtifacts') == '2')
        # Store world point calculated from transform ID fileID referenced by field 'cornerSpawn' in scare via
        # test_collision_assets.reference and local point [0, 0, 0] by the scare-test helper in spawn.
        spawn = self.world(reference(scare, 'cornerSpawn'), [0, 0, 0])
        # Store world point calculated from transform ID fileID referenced by field 'wallExit' in scare via
        # test_collision_assets.reference and local point [0, 0, 0] by the scare-test helper in end.
        end = self.world(reference(scare, 'wallExit'), [0, 0, 0])
        # Store the first matching entry from sequence of i using (i, (k, b)) from key/value pairs stored in
        # self.docs filtered by k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.054' in wall_go.
        wall_go = next(i for i, (k, b) in self.docs.items()
                       if k == 1 and field(b, 'm_Name') == 'ARCH_Wall_Plaster.054')
        # Store the first matching entry from sequence of i using (i, k, b) from self.components[wall_go] (read
        # from self.components) filtered by k == 4 in transform.
        transform = next(i for i, k, b in self.components[wall_go] if k == 4)
        # Store the first matching entry from sequence of b using (_, k, b) from self.components[wall_go] (read
        # from self.components) filtered by k == 65 in box.
        box = next(b for _, k, b in self.components[wall_go] if k == 65)
        # Expect serialized field 'm_Enabled' read from box by test_collision_assets.field to equal '1'.
        self.assertEqual(field(box, 'm_Enabled'), '1')
        # Store (self.vector(box, 'm_Center'), self.vector(box, 'm_Size')) in (center, size).
        center, size = self.vector(box, 'm_Center'), self.vector(box, 'm_Size')
        # Store list of self.world(transform, [c + s * d / 2 for (c, s, d) in zip(center, size, signs)]) using
        # signs from the result of itertools.product using [-1, 1] in corners.
        corners = [self.world(transform, [c+s*d/2 for c, s, d in zip(center, size, signs)])
                   for signs in itertools.product([-1, 1], repeat=3)]
        # Store list of min((p[k] for p in corners)) using k from integer sequence from 0 to 3 minus one in lo.
        lo = [min(p[k] for p in corners) for k in range(3)]
        # Store list of max((p[k] for p in corners)) using k from integer sequence from 0 to 3 minus one in hi.
        hi = [max(p[k] for p in corners) for k in range(3)]
        # Expect spawn[0] (read from spawn) to be greater than hi[0] + 0.5 (calculated from hi).
        self.assertGreater(spawn[0], hi[0] + 0.5)
        # Expect end[0] (read from end) to be below lo[0] - 0.5 (calculated from lo).
        self.assertLess(end[0], lo[0] - 0.5)
        # Markers now represent feet. The upper body must cross the wall,
        # whose collider begins above the floor in this authored layout.
        # Expect spawn[1] + 2 (calculated from spawn) to be greater than lo[1] (read from lo).
        self.assertGreater(spawn[1] + 2, lo[1])
        # Expect spawn[1] (read from spawn) to be below hi[1] (read from hi).
        self.assertLess(spawn[1], hi[1])
        # Expect spawn[2] (read from spawn) to be greater than lo[2] (read from lo).
        self.assertGreater(spawn[2], lo[2])
        # Expect spawn[2] (read from spawn) to be below hi[2] (read from hi).
        self.assertLess(spawn[2], hi[2])
        # Process each axis from (1, 2).
        for axis in (1, 2):
            # Expect spawn[axis] (read from spawn) to approximately equal end[axis] (read from end).
            self.assertAlmostEqual(spawn[axis], end[axis])
        # Expect serialized field 'exitSpeed' read from scare by test_collision_assets.field converted to a number
        # with decimals to be greater than 0.
        self.assertGreater(float(field(scare, 'exitSpeed')), 0)


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
