"""Navigation scene wiring and structural skinning regressions (no Unity required)."""
import json
import struct
import unittest
from pathlib import Path
import test_collision_assets as assets
from test_collision_assets import field, reference

ROOT = Path(__file__).resolve().parents[1]


class GhostNavigationRigTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        assets.CollisionAssetsTests.setUpClass()
        cls.docs, cls.components = assets.CollisionAssetsTests.assets[assets.ASSETS[0]]
        data = (ROOT / 'Assets/Resources/WhiteLadyRig.glb').read_bytes()
        magic, version, length = struct.unpack_from('<4sII', data)
        assert magic == b'glTF' and version == 2 and length == len(data)
        json_length = struct.unpack_from('<I', data, 12)[0]
        cls.rig = json.loads(data[20:20+json_length])
        cls.buffer = data[28+json_length:]

    def values(self, index):
        a = self.rig['accessors'][index]
        v = self.rig['bufferViews'][a['bufferView']]
        dtype = {5126: 'f', 5123: 'H', 5125: 'I'}[a['componentType']]
        width = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4, 'MAT4': 16}[a['type']]
        start = v.get('byteOffset', 0) + a.get('byteOffset', 0)
        fmt = '<' + dtype*width
        return list(struct.iter_unpack(fmt, self.buffer[start:start+a['count']*struct.calcsize(fmt)]))

    def test_agent_starts_disabled_and_environment_is_explicit(self):
        agent = self.docs['1186399122'][1]
        self.assertEqual(field(agent, 'm_Enabled'), '0')
        navigation = self.docs['8800000000000000021'][1]
        transform = self.docs[reference(navigation, 'environmentRoot')][1]
        environment = self.docs[reference(transform, 'm_GameObject')][1]
        self.assertEqual(field(environment, 'm_Name'), 'Environment')
        self.assertEqual(field(navigation, 'agentTypeID'), field(agent, 'm_AgentTypeID'))
        ghost = self.docs['1186399119'][1]
        self.assertEqual(field(ghost, 'requireNavigation'), '1')
        self.assertEqual(reference(ghost, 'm_GameObject'), reference(agent, 'm_GameObject'))

    def test_rig_is_attached_to_real_ghost_and_offsets_match(self):
        animation = self.docs['8800000000000000022'][1]
        self.assertEqual(reference(animation, 'm_GameObject'), '1186399117')
        self.assertIn('addedObject: {fileID: 8800000000000000022}', self.docs['437939276'][1])
        self.assertEqual(float(field(animation, 'visualFootOffset')),
                         -float(field(self.docs['1186399122'][1], 'm_BaseOffset')))
        for name in ['GhostWalk', 'GhostIdle']:
            meta = (ROOT / f'Assets/Resources/GhostAnimations/{name}.fbx.meta').read_text()
            self.assertIn('  animationType: 3', meta)
            self.assertIn('  avatarSetup: 1', meta)

    def test_humanoid_hierarchy_and_inverse_bind_transforms(self):
        nodes = self.rig['nodes']
        names = {n['name']: i for i, n in enumerate(nodes)}
        required = ['Hips', 'Spine', 'Chest', 'Neck', 'Head']
        for side in ['Left', 'Right']:
            required += [side+s for s in ['Shoulder','UpperArm','LowerArm','Hand','UpperLeg','LowerLeg','Foot','Toes']]
        self.assertTrue(set(required).issubset(names))
        self.assertEqual(len(names), len(nodes), 'Avatar bones must have unique names')
        parents = {c: i for i, n in enumerate(nodes) for c in n.get('children', [])}
        def position(i):
            p = nodes[i].get('translation', [0, 0, 0])
            return [a+b for a,b in zip(p,position(parents[i]))] if i in parents else p
        skin = self.rig['skins'][0]
        matrices = self.values(skin['inverseBindMatrices'])
        self.assertEqual(len(matrices), len(skin['joints']))
        for node_id, matrix in zip(skin['joints'], matrices):
            for coordinate, inverse in zip(position(node_id), matrix[12:15]):
                self.assertAlmostEqual(coordinate, -inverse, places=5)
        for side in ['Left', 'Right']:
            height = position(names[side+'UpperArm'])[1]
            for part in ['LowerArm','Hand']:
                self.assertAlmostEqual(position(names[side+part])[1], height, places=5)

    def test_every_vertex_has_normalized_valid_weights(self):
        joint_count = len(self.rig['skins'][0]['joints'])
        vertex_count = 0
        for mesh in self.rig['meshes']:
            for primitive in mesh['primitives']:
                attributes = primitive['attributes']
                weights = self.values(attributes['WEIGHTS_0'])
                joints = self.values(attributes['JOINTS_0'])
                self.assertEqual(len(weights), self.rig['accessors'][attributes['POSITION']]['count'])
                for weight, joint in zip(weights, joints):
                    self.assertAlmostEqual(sum(weight), 1, places=5)
                    self.assertTrue(all(0 <= w <= 1 for w in weight))
                    self.assertTrue(all(0 <= j < joint_count for j in joint))
                vertex_count += len(weights)
        self.assertEqual(vertex_count, 168979)
        image = self.rig['images'][0]
        view = self.rig['bufferViews'][image['bufferView']]
        start = view['byteOffset']
        self.assertEqual(self.buffer[start:start+2], b'\xff\xd8', 'Embedded texture must be JPEG, not an LFS pointer')


if __name__ == '__main__':
    unittest.main()
