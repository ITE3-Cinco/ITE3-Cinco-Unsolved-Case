# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Navigation scene wiring and structural skinning regressions (no Unity required)."""
# Import itertools for generating all box-corner sign combinations.
import itertools
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import json for encoding and decoding glTF/GLB JSON metadata.
import json
# Import struct for packing/unpacking binary headers and typed accessor data.
import struct
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Import Path from Python pathlib to build paths relative to this script and read asset files.
from pathlib import Path
# Import test_collision_assets as assets for the helpers used below.
import test_collision_assets as assets
# Import test_ghost_corner_scare as scare_tests for the helpers used below.
import test_ghost_corner_scare as scare_tests
# Reuse field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import field, reference

# Resolve this Python file path, then move up two directory levels to locate the repository root in ROOT.
ROOT = Path(__file__).resolve().parents[1]


class GhostNavigationRigTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Initialize the shared fixture data in assets.CollisionAssetsTests.
        assets.CollisionAssetsTests.setUpClass()
        # Store assets.CollisionAssetsTests.assets[assets.ASSETS[0]] (read from
        # assets.CollisionAssetsTests.assets) in (cls.docs, cls.components).
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]
        # Store binary bytes read from file ROOT / 'Assets/Resources/WhiteLadyRig.glb' in data.
        data = (ROOT / 'Assets/Resources/WhiteLadyRig.glb').read_bytes()
        # Store values decoded from data using binary format '<4sII' in (magic, version, length).
        magic, version, length = struct.unpack_from('<4sII', data)
        # Require magic equals b'glTF' and version equals 2 and length equals length of data.
        assert magic == b'glTF' and version == 2 and length == len(data)
        # Store struct.unpack_from('<I', data, 12)[0] (read from struct.unpack_from('<I', data, 12)) in
        # json_length.
        json_length = struct.unpack_from('<I', data, 12)[0]
        # Decode the GLB JSON chunk starting after its 12-byte file header and 8-byte chunk header into rig
        # metadata.
        cls.rig = json.loads(data[20:20+json_length])
        # Skip the file header, JSON chunk and binary chunk header; retain the remaining GLB binary data for
        # accessor decoding.
        cls.buffer = data[28+json_length:]

    # Decode one rig accessor from its binary buffer using the stored type, count and offset.
    # index is an accessor ID from the source glTF or generated GLB metadata, supplied by the caller.
    def values(self, index):
        # Store self.rig['accessors'][index] (read from self.rig['accessors']) in a.
        a = self.rig['accessors'][index]
        # Store self.rig['bufferViews'][a['bufferView']] (read from self.rig['bufferViews']) in v.
        v = self.rig['bufferViews'][a['bufferView']]
        # Store the lookup value selected by a['componentType'] (read from a) from the table below in dtype.
        dtype = {5126: 'f', 5123: 'H', 5125: 'I'}[a['componentType']]
        # Store the lookup value selected by a['type'] (read from a) from the table below in width.
        width = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
        # Store v.get('byteOffset', 0) + a.get('byteOffset', 0) (calculated from v, a) in start.
        start = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        # Store '<' + dtype * width (calculated from dtype, width) in fmt.
        fmt = '<' + dtype*width
        # Return a list containing records decoded in order from self.buffer[start:start + a['count'] *
        # struct.calcsize(fmt)] (read from self.buffer) using binary format fmt to the helper caller.
        return list(struct.iter_unpack(fmt, self.buffer[start:start+a['count']*struct.calcsize(fmt)]))

    # Check that agent starts disabled and environment is explicit.
    def test_agent_starts_disabled_and_environment_is_explicit(self):
        # Store self.docs['1186399122'][1] (read from self.docs['1186399122']) in agent.
        agent = self.docs['1186399122'][1]
        # Expect serialized field 'm_Enabled' read from agent by test_collision_assets.field to equal '0'.
        self.assertEqual(field(agent, 'm_Enabled'), '0')
        # Store self.docs['8800000000000000021'][1] (read from self.docs['8800000000000000021']) in navigation.
        navigation = self.docs['8800000000000000021'][1]
        # Store self.docs[reference(navigation, 'environmentRoot')][1] (read from self.docs[reference(navigation,
        # 'environmentRoot')]) in transform.
        transform = self.docs[reference(navigation, 'environmentRoot')][1]
        # Store self.docs[reference(transform, 'm_GameObject')][1] (read from self.docs[reference(transform,
        # 'm_GameObject')]) in environment.
        environment = self.docs[reference(transform, 'm_GameObject')][1]
        # Expect serialized field 'm_Name' read from environment by test_collision_assets.field to equal
        # 'Environment'.
        self.assertEqual(field(environment, 'm_Name'), 'Environment')
        # Expect serialized field 'agentTypeID' read from navigation by test_collision_assets.field to equal
        # serialized field 'm_AgentTypeID' read from agent by test_collision_assets.field.
        self.assertEqual(field(navigation, 'agentTypeID'), field(agent, 'm_AgentTypeID'))
        # Store self.docs['1186399119'][1] (read from self.docs['1186399119']) in ghost.
        ghost = self.docs['1186399119'][1]
        # Expect serialized field 'requireNavigation' read from ghost by test_collision_assets.field to equal '1'.
        self.assertEqual(field(ghost, 'requireNavigation'), '1')
        # Expect fileID referenced by field 'm_GameObject' in ghost via test_collision_assets.reference to equal
        # fileID referenced by field 'm_GameObject' in agent via test_collision_assets.reference.
        self.assertEqual(reference(ghost, 'm_GameObject'), reference(agent, 'm_GameObject'))

    # Check that rig is attached to real ghost and offsets match.
    def test_rig_is_attached_to_real_ghost_and_offsets_match(self):
        # Store self.docs['8800000000000000022'][1] (read from self.docs['8800000000000000022']) in animation.
        animation = self.docs['8800000000000000022'][1]
        # Expect fileID referenced by field 'm_GameObject' in animation via test_collision_assets.reference to
        # equal '1186399117'.
        self.assertEqual(reference(animation, 'm_GameObject'), '1186399117')
        # Expect 'addedObject: {fileID: 8800000000000000022}' to appear in self.docs['437939276'][1] (read from
        # self.docs['437939276']).
        self.assertIn('addedObject: {fileID: 8800000000000000022}', self.docs['437939276'][1])
        # Expect serialized field 'visualFootOffset' read from animation by test_collision_assets.field converted
        # to a number with decimals to equal 0.
        self.assertEqual(float(field(animation, 'visualFootOffset')), 0)
        # Expect serialized field 'm_BaseOffset' read from self.docs['1186399122'][1] (read from
        # self.docs['1186399122']) by test_collision_assets.field converted to a number with decimals to equal 0.
        self.assertEqual(float(field(self.docs['1186399122'][1], 'm_BaseOffset')), 0)
        # Store self.docs['1186399123'][1] (read from self.docs['1186399123']) in capsule.
        capsule = self.docs['1186399123'][1]
        # Store numeric vector components parsed from field 'm_Center' of capsule in center.
        center = scare_tests.GhostCornerScareTests.vector(capsule, 'm_Center')
        # Expect center[1] - float(field(capsule, 'm_Height')) / 2 (calculated from center, float, field, capsule)
        # to approximately equal 0.
        self.assertAlmostEqual(center[1] - float(field(capsule, 'm_Height')) / 2, 0)
        # Physical collision plus the player's skin must be inside the catch range.
        # Store the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 143 in player.
        player = next(b for k, b in self.docs.values() if k == 143)
        # Store float(field(capsule, 'm_Radius')) + float(field(player, 'm_Radius')) + float(field(player,
        # 'm_SkinWidth')) (calculated from float, field, player, capsule) in contact.
        contact = float(field(capsule, 'm_Radius')) + float(field(player, 'm_Radius')) + float(field(player, 'm_SkinWidth'))
        # Expect serialized field 'catchRadius' read from self.docs['1186399119'][1] (read from
        # self.docs['1186399119']) by test_collision_assets.field converted to a number with decimals to be
        # greater than contact.
        self.assertGreater(float(field(self.docs['1186399119'][1], 'catchRadius')), contact)
        # Process each name from ['GhostWalk', 'GhostIdle'].
        for name in ['GhostWalk', 'GhostIdle']:
            # Store text read from file ROOT / f'Assets/Resources/GhostAnimations/{name}.fbx.meta' in meta.
            meta = (ROOT / f'Assets/Resources/GhostAnimations/{name}.fbx.meta').read_text()
            # Expect '  animationType: 3' to appear in meta.
            self.assertIn('  animationType: 3', meta)
            # Expect '  avatarSetup: 1' to appear in meta.
            self.assertIn('  avatarSetup: 1', meta)

    # Check that walk and scream resolve to humanoid imports.
    def test_walk_and_scream_resolve_to_humanoid_imports(self):
        # Store self.docs['8800000000000000022'][1] (read from self.docs['8800000000000000022']) in animation.
        animation = self.docs['8800000000000000022'][1]
        # Walk uses the named Resources clip, not the unrelated Generic override.
        # Expect fileID referenced by field 'walkingClip' in animation via test_collision_assets.reference to
        # equal '0'.
        self.assertEqual(reference(animation, 'walkingClip'), '0')
        # Process each name from ['GhostWalk', 'GhostIdle', 'GhostScream'].
        for name in ['GhostWalk', 'GhostIdle', 'GhostScream']:
            # Store text read from file ROOT / f'Assets/Resources/GhostAnimations/{name}.fbx.meta' in meta.
            meta = (ROOT / f'Assets/Resources/GhostAnimations/{name}.fbx.meta').read_text()
            # Expect '  animationType: 3' to appear in meta.
            self.assertIn('  animationType: 3', meta)
            # Expect '  avatarSetup: 1' to appear in meta.
            self.assertIn('  avatarSetup: 1', meta)
            # Expect '  importAnimation: 1' to appear in meta.
            self.assertIn('  importAnimation: 1', meta)
            # Run this branch only if name equals 'GhostScream'.
            if name == 'GhostScream':
                # Expect re.search('guid: (\\w+)', meta)[1] (read from re.search('guid: (\\w+)', meta)) to appear
                # in serialized field 'screamClip' read from animation by test_collision_assets.field.
                self.assertIn(re.search(r'guid: (\w+)', meta)[1], field(animation, 'screamClip'))

    # Check that initial patrol route has continuous corridor floor.
    def test_initial_patrol_route_has_continuous_corridor_floor(self):
        # Store the result of scare_tests.GhostCornerScareTests in helper.
        helper = scare_tests.GhostCornerScareTests()
        # Store (self.docs, self.components) in (helper.docs, helper.components).
        helper.docs, helper.components = self.docs, self.components
        # Store self.docs['1186399119'][1] (read from self.docs['1186399119']) in ghost.
        ghost = self.docs['1186399119'][1]
        # Expect serialized field 'waitForPlayerBeforePatrolling' read from ghost by test_collision_assets.field
        # to equal '0'.
        self.assertEqual(field(ghost, 'waitForPlayerBeforePatrolling'), '0')
        # Expect serialized field 'artifactsBeforeHunt' read from ghost by test_collision_assets.field to equal
        # '1'.
        self.assertEqual(field(ghost, 'artifactsBeforeHunt'), '1')
        # Store an empty list in floors.
        floors = []
        # Process each (go, (kind, body)) from key/value pairs stored in self.docs.
        for go, (kind, body) in self.docs.items():
            # Run this branch only if kind differs from 1 or not (field(body, 'm_Name') or '' starts with
            # 'ARCH_CorridorFloor').
            if kind != 1 or not (field(body, 'm_Name') or '').startswith('ARCH_CorridorFloor'):
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Store self.components[go] (read from self.components) in components.
            components = self.components[go]
            # Store the first matching entry from sequence of i using (i, k, b) from components filtered by k == 4
            # in transform.
            transform = next(i for i, k, b in components if k == 4)
            # Process each (_, k, box) from components.
            for _, k, box in components:
                # Run this branch only if k differs from 65 or serialized field 'm_Enabled' read from box by
                # test_collision_assets.field differs from '1'.
                if k != 65 or field(box, 'm_Enabled') != '1':
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Store (helper.vector(box, 'm_Center'), helper.vector(box, 'm_Size')) in (center, size).
                center, size = helper.vector(box, 'm_Center'), helper.vector(box, 'm_Size')
                # Store list of helper.world(transform, [c + s * d / 2 for (c, s, d) in zip(center, size, signs)])
                # using signs from the result of itertools.product using [-1, 1] in corners.
                corners = [helper.world(transform, [c+s*d/2 for c,s,d in zip(center,size,signs)])
                           for signs in itertools.product([-1, 1], repeat=3)]
                # Append ([min((p[j] for p in corners)) for j in range(3)], [max((p[j] for p in corners)) for j in
                # range(3)]) to floors for later lookup or binary output.
                floors.append(([min(p[j] for p in corners) for j in range(3)],
                               [max(p[j] for p in corners) for j in range(3)]))
        # Store list of helper.world(i, [0, 0, 0]) using i from ['377456405', '967623085', '146934705'] in
        # waypoints.
        waypoints = [helper.world(i, [0, 0, 0]) for i in ['377456405', '967623085', '146934705']]
        # Store self.docs['437939276'][1] (read from self.docs['437939276']) in prefab.
        prefab = self.docs['437939276'][1]
        # Store list of float(re.search('propertyPath: m_LocalPosition.' + axis + '\\n      value: ([^\\n]+)',
        # prefab)[1]) using axis from 'xyz' in spawn.
        spawn = [float(re.search(r'propertyPath: m_LocalPosition.' + axis + r'\n      value: ([^\n]+)', prefab)[1])
                 for axis in 'xyz']
        # Store world point calculated from transform ID '41796415' and local point spawn by the scare-test helper
        # in spawn.
        spawn = helper.world('41796415', spawn)
        # Subtract serialized field 'm_BaseOffset' read from self.docs['1186399122'][1] (read from
        # self.docs['1186399122']) by test_collision_assets.field converted to a number with decimals from
        # spawn[1].
        spawn[1] -= float(field(self.docs['1186399122'][1], 'm_BaseOffset'))
        # Store [spawn] + waypoints + waypoints[:1] (calculated from waypoints, spawn) in points.
        points = [spawn] + waypoints + waypoints[:1]
        # Sample the actual full patrol legs, including a 0.5m agent radius.
        # Process each (start, end) from corresponding entries paired from points, points[1:] (read from points).
        for start, end in zip(points, points[1:]):
            # Store int(sum(((a - b) ** 2 for (a, b) in zip(start, end))) ** 0.5 * 4) + 1 (calculated from int,
            # sum, a, b, zip, start, end) in steps.
            steps = int(sum((a-b)**2 for a,b in zip(start,end))**0.5 * 4) + 1
            # Process each step from integer sequence from 0 to steps + 1 (calculated from steps) minus one.
            for step in range(steps + 1):
                # Store list of a + (b - a) * step / steps using (a, b) from corresponding entries paired from
                # start, end in point.
                point = [a+(b-a)*step/steps for a,b in zip(start,end)]
                # Process each (dx, dz) from [(0, 0), (0.5, 0), (-0.5, 0), (0, 0.5), (0, -0.5)].
                for dx, dz in [(0, 0), (.5, 0), (-.5, 0), (0, .5), (0, -.5)]:
                    # Store (point[0] + dx, point[2] + dz) in (x, z).
                    x, z = point[0]+dx, point[2]+dz
                    # Expect whether at least one entry satisfies sequence of lo[0] <= x <= hi[0] and lo[2] <= z
                    # <= hi[2] and (abs(point[1] - hi[1]) < 0.2) using (lo, hi) from floors to be true.
                    self.assertTrue(any(lo[0] <= x <= hi[0] and lo[2] <= z <= hi[2]
                                        and abs(point[1]-hi[1]) < .2 for lo,hi in floors),
                                    f'Patrol leaves corridor floor at {point}, offset {(dx,dz)}')

    # Check that wake lids cover canvas before first frame.
    def test_wake_lids_cover_canvas_before_first_frame(self):
        # Store (self.docs['988547755'][1], self.docs['674584998'][1]) in (top, bottom).
        top, bottom = self.docs['988547755'][1], self.docs['674584998'][1]
        # Expect serialized field 'm_AnchorMin' read from top by test_collision_assets.field to equal '{x: 0, y:
        # 0.5}'.
        self.assertEqual(field(top, 'm_AnchorMin'), '{x: 0, y: 0.5}')
        # Expect serialized field 'm_AnchorMax' read from top by test_collision_assets.field to equal '{x: 1, y:
        # 1}'.
        self.assertEqual(field(top, 'm_AnchorMax'), '{x: 1, y: 1}')
        # Expect serialized field 'm_AnchorMin' read from bottom by test_collision_assets.field to equal '{x: 0,
        # y: 0}'.
        self.assertEqual(field(bottom, 'm_AnchorMin'), '{x: 0, y: 0}')
        # Expect serialized field 'm_AnchorMax' read from bottom by test_collision_assets.field to equal '{x: 1,
        # y: 0.5}'.
        self.assertEqual(field(bottom, 'm_AnchorMax'), '{x: 1, y: 0.5}')
        # Process each lid from [top, bottom].
        for lid in [top, bottom]:
            # Expect serialized field 'm_SizeDelta' read from lid by test_collision_assets.field to equal '{x: 0,
            # y: 0}'.
            self.assertEqual(field(lid, 'm_SizeDelta'), '{x: 0, y: 0}')
            # Expect serialized field 'm_AnchoredPosition' read from lid by test_collision_assets.field to equal
            # '{x: 0, y: 0}'.
            self.assertEqual(field(lid, 'm_AnchoredPosition'), '{x: 0, y: 0}')
            # Expect fileID referenced by field 'm_Father' in lid via test_collision_assets.reference to equal
            # '1513529659'.
            self.assertEqual(reference(lid, 'm_Father'), '1513529659')
        # Expect serialized field 'm_RenderMode' read from self.docs['1513529658'][1] (read from
        # self.docs['1513529658']) by test_collision_assets.field to equal '0'.
        self.assertEqual(field(self.docs['1513529658'][1], 'm_RenderMode'), '0')

    # Check that humanoid hierarchy and inverse bind transforms.
    def test_humanoid_hierarchy_and_inverse_bind_transforms(self):
        # Store self.rig['nodes'] (read from self.rig) in nodes.
        nodes = self.rig['nodes']
        # Store dictionary mapping n['name'] to i from index-and-entry pairs from nodes in names.
        names = {n['name']: i for i, n in enumerate(nodes)}
        # Store ['Hips', 'Spine', 'Chest', 'Neck', 'Head'] in required.
        required = ['Hips', 'Spine', 'Chest', 'Neck', 'Head']
        # Process each side from ['Left', 'Right'].
        for side in ['Left', 'Right']:
            # Add list of side + s using s from ['Shoulder', 'UpperArm', 'LowerArm', 'Hand', 'UpperLeg',
            # 'LowerLeg', 'Foot', 'Toes'] to required.
            required += [side+s for s in ['Shoulder','UpperArm','LowerArm','Hand','UpperLeg','LowerLeg','Foot','Toes']]
        # Expect the result of set(required).issubset using names to be true.
        self.assertTrue(set(required).issubset(names))
        # Expect length of names to equal length of nodes.
        self.assertEqual(len(names), len(nodes), 'Avatar bones must have unique names')
        # Store dictionary mapping c to i from index-and-entry pairs from nodes, dictionary value for key
        # 'children' in n (default an empty list) in parents.
        parents = {c: i for i, n in enumerate(nodes) for c in n.get('children', [])}
        # Add parent translations recursively to find a rig bone world position.
        # i is the rig node index supplied by the recursive bone-position caller.
        def position(i):
            # Store dictionary value for key 'translation' in nodes[i] (default [0, 0, 0]) in p.
            p = nodes[i].get('translation', [0, 0, 0])
            # Return list of a + b using (a, b) from corresponding entries paired from p, bone world translation
            # calculated recursively from node parents[i] (read from parents) and its parents when i appears in
            # parents; otherwise p to the helper caller.
            return [a+b for a,b in zip(p,position(parents[i]))] if i in parents else p
        # Store self.rig['skins'][0] (read from self.rig['skins']) in skin.
        skin = self.rig['skins'][0]
        # Store typed rig accessor data decoded from the GLB binary chunk at accessor index
        # skin['inverseBindMatrices'] (read from skin) in matrices.
        matrices = self.values(skin['inverseBindMatrices'])
        # Expect length of matrices to equal length of skin['joints'].
        self.assertEqual(len(matrices), len(skin['joints']))
        # Process each (node_id, matrix) from corresponding entries paired from skin['joints'] (read from skin),
        # matrices.
        for node_id, matrix in zip(skin['joints'], matrices):
            # Process each (coordinate, inverse) from corresponding entries paired from bone world translation
            # calculated recursively from node node_id and its parents, matrix[12:15] (read from matrix).
            for coordinate, inverse in zip(position(node_id), matrix[12:15]):
                # Expect coordinate to approximately equal negative inverse.
                self.assertAlmostEqual(coordinate, -inverse, places=5)
        # Process each side from ['Left', 'Right'].
        for side in ['Left', 'Right']:
            # Store position(names[side + 'UpperArm'])[1] (read from position(names[side + 'UpperArm'])) in
            # height.
            height = position(names[side+'UpperArm'])[1]
            # Process each part from ['LowerArm', 'Hand'].
            for part in ['LowerArm','Hand']:
                # Expect position(names[side + part])[1] (read from position(names[side + part])) to approximately
                # equal height.
                self.assertAlmostEqual(position(names[side+part])[1], height, places=5)

    # Check that every vertex has normalized valid weights.
    def test_every_vertex_has_normalized_valid_weights(self):
        # Store length of self.rig['skins'][0]['joints'] in joint_count.
        joint_count = len(self.rig['skins'][0]['joints'])
        # Initialize vertex_count with 0.
        vertex_count = 0
        # Process each mesh from self.rig['meshes'] (read from self.rig).
        for mesh in self.rig['meshes']:
            # Process each primitive from mesh['primitives'] (read from mesh).
            for primitive in mesh['primitives']:
                # Store primitive['attributes'] (read from primitive) in attributes.
                attributes = primitive['attributes']
                # Store typed rig accessor data decoded from the GLB binary chunk at accessor index
                # attributes['WEIGHTS_0'] (read from attributes) in weights.
                weights = self.values(attributes['WEIGHTS_0'])
                # Store typed rig accessor data decoded from the GLB binary chunk at accessor index
                # attributes['JOINTS_0'] (read from attributes) in joints.
                joints = self.values(attributes['JOINTS_0'])
                # Expect length of weights to equal self.rig['accessors'][attributes['POSITION']]['count'] (read
                # from self.rig['accessors'][attributes['POSITION']]).
                self.assertEqual(len(weights), self.rig['accessors'][attributes['POSITION']]['count'])
                # Process each (weight, joint) from corresponding entries paired from weights, joints.
                for weight, joint in zip(weights, joints):
                    # Expect sum of weight to approximately equal 1.
                    self.assertAlmostEqual(sum(weight), 1, places=5)
                    # Expect whether every entry satisfies sequence of 0 <= w <= 1 using w from weight to be true.
                    self.assertTrue(all(0 <= w <= 1 for w in weight))
                    # Expect whether every entry satisfies sequence of 0 <= j < joint_count using j from joint to
                    # be true.
                    self.assertTrue(all(0 <= j < joint_count for j in joint))
                # Add length of weights to vertex_count.
                vertex_count += len(weights)
        # Expect vertex_count to equal 168979.
        self.assertEqual(vertex_count, 168979)
        # Store self.rig['images'][0] (read from self.rig['images']) in image.
        image = self.rig['images'][0]
        # Store self.rig['bufferViews'][image['bufferView']] (read from self.rig['bufferViews']) in view.
        view = self.rig['bufferViews'][image['bufferView']]
        # Store view['byteOffset'] (read from view) in start.
        start = view['byteOffset']
        # Expect self.buffer[start:start + 2] (read from self.buffer) to equal b'\xff\xd8'.
        self.assertEqual(self.buffer[start:start+2], b'\xff\xd8', 'Embedded texture must be JPEG, not an LFS pointer')


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == '__main__':
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
