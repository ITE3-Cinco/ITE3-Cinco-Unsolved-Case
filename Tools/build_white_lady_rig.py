"""Build a basic skinned, T-pose White Lady from the original unrigged glTF.
Requires Python 3 + numpy. Original geometry, UVs, texture and attribution retained.
The gown follows hips/thighs gently; hair follows the head. Review deformation in Unity.
"""
import copy
import json
from pathlib import Path
import struct
import numpy as np

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Assets/free assets/a_horror_girl_character_code99type101'
DESTINATION = ROOT / 'Assets/Resources/WhiteLadyRig.glb'
source = json.loads((SOURCE / 'scene.gltf').read_text())
source_buffer = (SOURCE / source['buffers'][0]['uri']).read_bytes()
output = {'asset': {'version': '2.0', 'generator': 'White Lady basic humanoid rig',
                     'copyright': 'Based on A HORROR GIRL CHARACTER CODE99/type101 by yongtun, CC BY 4.0; local rig and skinning added. See Documentation/GhostNavigationAndAnimation.md.'},
          'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'name': 'WhiteLadyRig', 'children': []}],
          'meshes': [], 'skins': [], 'accessors': [], 'bufferViews': [],
          'materials': copy.deepcopy(source['materials']),
          'textures': copy.deepcopy(source['textures']), 'images': [],
          'samplers': copy.deepcopy(source.get('samplers', []))}
binary = bytearray()


def buffer_view(data):
    binary.extend(b'\0' * (-len(binary) % 4))
    index = len(output['bufferViews'])
    output['bufferViews'].append({'buffer': 0, 'byteOffset': len(binary), 'byteLength': len(data)})
    binary.extend(data)
    return index


def accessor(data, component_type, kind, bounds=False):
    index = len(output['accessors'])
    item = {'bufferView': buffer_view(data.tobytes()), 'componentType': component_type,
            'count': len(data), 'type': kind}
    if bounds:
        item.update(min=data.min(axis=0).tolist(), max=data.max(axis=0).tolist())
    output['accessors'].append(item)
    return index


def read(index):
    a = source['accessors'][index]
    v = source['bufferViews'][a['bufferView']]
    dtype = {5126: '<f4', 5125: '<u4', 5123: '<u2'}[a['componentType']]
    width = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3}[a['type']]
    return np.frombuffer(source_buffer, dtype=dtype, count=a['count'] * width,
                         offset=v.get('byteOffset', 0) + a.get('byteOffset', 0)).reshape(-1, width).copy()


# Model coordinates are Y-up, centered at the waist. Shift the feet to Y=0.
# The hands/forearms are fitted to the original A-pose, then lifted to a T-pose.
bones = []

def bone(name, parent, position, end):
    bones.append((name, parent, np.array(position, float), np.array(end, float)))

bone('Hips', None, (0, .95, 0), (0, 1.13, 0))
bone('Spine', 'Hips', (0, 1.13, 0), (0, 1.32, 0))
bone('Chest', 'Spine', (0, 1.32, 0), (0, 1.53, 0))
bone('Neck', 'Chest', (0, 1.53, 0), (0, 1.67, 0))
bone('Head', 'Neck', (0, 1.67, 0), (0, 1.96, 0))
for side, sign in [('Left', 1), ('Right', -1)]:
    bone(side+'Shoulder', 'Chest', (sign*.08, 1.48, 0), (sign*.18, 1.47, 0))
    bone(side+'UpperArm', side+'Shoulder', (sign*.18, 1.47, 0), (sign*.33, 1.18, 0))
    bone(side+'LowerArm', side+'UpperArm', (sign*.33, 1.18, 0), (sign*.48, .97, .01))
    bone(side+'Hand', side+'LowerArm', (sign*.48, .97, .01), (sign*.56, .86, .01))
    bone(side+'UpperLeg', 'Hips', (sign*.10, .94, 0), (sign*.10, .51, .005))
    bone(side+'LowerLeg', side+'UpperLeg', (sign*.10, .51, .005), (sign*.10, .09, 0))
    bone(side+'Foot', side+'LowerLeg', (sign*.10, .09, 0), (sign*.10, .045, .12))
    bone(side+'Toes', side+'Foot', (sign*.10, .045, .12), (sign*.10, .045, .18))

indices = {name: i for i, (name, _, _, _) in enumerate(bones)}
rest_positions = [b[2] for b in bones]
t_positions = [v.copy() for v in rest_positions]
rotations = [np.eye(3) for _ in bones]
for side, sign in [('Left', 1), ('Right', -1)]:
    shoulder = indices[side+'UpperArm']
    anchor = t_positions[shoulder]
    for suffix in ['UpperArm', 'LowerArm', 'Hand']:
        i = indices[side+suffix]
        direction = bones[i][3] - bones[i][2]
        angle = -np.arctan2(direction[1], abs(direction[0])) * sign
        c, s = np.cos(angle), np.sin(angle)
        rotations[i] = np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
        t_positions[i] = anchor.copy()
        anchor += np.array([sign*np.linalg.norm(direction), 0, 0])

for i, (name, parent, _, _) in enumerate(bones):
    parent_index = indices[parent] if parent else None
    position = t_positions[i] - (t_positions[parent_index] if parent else 0)
    output['nodes'].append({'name': name, 'translation': position.tolist(), 'children': []})
    output['nodes'][parent_index+1 if parent else 0]['children'].append(i+1)

inverse = np.tile(np.eye(4), (len(bones), 1, 1))
for i, pos in enumerate(t_positions):
    inverse[i, :3, 3] = -pos
# glTF matrices are column-major.
ibm = accessor(inverse.transpose(0, 2, 1).astype('<f4').reshape(-1, 16), 5126, 'MAT4')
output['skins'].append({'name': 'WhiteLadyHumanoid', 'joints': list(range(1, len(bones)+1)),
                        'skeleton': 1, 'inverseBindMatrices': ibm})

for original_mesh in source['meshes']:
    prim = original_mesh['primitives'][0]
    vertices = read(prim['attributes']['POSITION']); vertices[:, 1] += 1.00018
    normals = read(prim['attributes']['NORMAL'])
    distances = []
    for _, _, start, end in bones:
        direction = end-start
        t = np.clip(((vertices-start) @ direction) / np.dot(direction, direction), 0, 1)
        distances.append(np.linalg.norm(vertices-(start+t[:, None]*direction), axis=1))
    distances = np.stack(distances, axis=1)
    weights = 1 / np.maximum(distances, .015)**4
    # Do not allow the gown or hanging hair to bind to nearby arms.
    for side in ['Left', 'Right']:
        arm = [indices[side+s] for s in ['Shoulder', 'UpperArm', 'LowerArm', 'Hand']]
        body_width = np.interp(vertices[:, 1], [0, .9, 1.12, 1.28, 1.5, 2], [.35, .30, .28, .18, .15, .15])
        torso = np.abs(vertices[:, 0]) < body_width
        weights[np.ix_(torso, arm)] = 0
    # Separate the lowered hands from the adjacent skirt before raising the arms.
    for side, sign in [('Left', 1), ('Right', -1)]:
        hand_region = (vertices[:, 0]*sign > .35) & (vertices[:, 1] > .65)
        non_arm = [i for i, b in enumerate(bones) if b[0] not in
                   [side+s for s in ['UpperArm', 'LowerArm', 'Hand']]]
        weights[np.ix_(hand_region, non_arm)] = 0
    # Most of the long gown follows the pelvis; a small leg influence suggests walking
    # without stretching the hem fully with each footstep.
    gown = (vertices[:, 1] < 1.08) & (np.abs(vertices[:, 0]) < .28)
    weights[gown] = 0
    weights[gown, indices['Hips']] = .8
    for side, sign in [('Left', 1), ('Right', -1)]:
        region = gown & (vertices[:, 0]*sign >= 0)
        weights[region, indices[side+'UpperLeg']] = .2
    # Keep long hair attached to the head rather than tearing across the shoulder.
    hair = (vertices[:, 1] > 1.35) & (np.abs(vertices[:, 0]) < .13)
    weights[hair] = 0
    weights[hair, indices['Head']] = 1
    joints = np.argsort(weights, axis=1)[:, -4:]
    chosen = np.take_along_axis(weights, joints, axis=1)
    chosen /= chosen.sum(axis=1, keepdims=True)
    joints[chosen == 0] = 0
    t_vertices = np.zeros_like(vertices)
    t_normals = np.zeros_like(normals)
    for influence in range(4):
        for i in range(len(bones)):
            mask = joints[:, influence] == i
            w = chosen[mask, influence, None]
            t_vertices[mask] += ((vertices[mask]-rest_positions[i]) @ rotations[i].T + t_positions[i])*w
            t_normals[mask] += (normals[mask] @ rotations[i].T)*w
    t_normals /= np.maximum(np.linalg.norm(t_normals, axis=1, keepdims=True), 1e-8)
    attrs = {'POSITION': accessor(t_vertices.astype('<f4'), 5126, 'VEC3', True),
             'NORMAL': accessor(t_normals.astype('<f4'), 5126, 'VEC3'),
             'TEXCOORD_0': accessor(read(prim['attributes']['TEXCOORD_0']).astype('<f4'), 5126, 'VEC2'),
             'JOINTS_0': accessor(joints.astype('<u2'), 5123, 'VEC4'),
             'WEIGHTS_0': accessor(chosen.astype('<f4'), 5126, 'VEC4')}
    index_accessor = source['accessors'][prim['indices']]
    mesh_id = len(output['meshes'])
    output['meshes'].append({'name': original_mesh['name'], 'primitives': [{'attributes': attrs,
        'indices': accessor(read(prim['indices']), index_accessor['componentType'], 'SCALAR'),
        'material': prim['material'], 'mode': 4}]})
    node_id = len(output['nodes'])
    output['nodes'].append({'name': original_mesh['name'], 'mesh': mesh_id, 'skin': 0})
    output['scenes'][0]['nodes'].append(node_id)

for item in source['images']:
    texture = SOURCE / item['uri']
    output['images'].append({'bufferView': buffer_view(texture.read_bytes()),
                             'mimeType': 'image/png' if texture.suffix.lower() == '.png' else 'image/jpeg'})
for node in output['nodes']:
    if node.get('children') == []:
        del node['children']
for mesh in output['meshes']:
    for primitive in mesh['primitives']:
        for index in primitive['attributes'].values():
            output['bufferViews'][output['accessors'][index]['bufferView']]['target'] = 34962
        output['bufferViews'][output['accessors'][primitive['indices']]['bufferView']]['target'] = 34963
output['buffers'] = [{'byteLength': len(binary)}]
encoded = json.dumps(output, separators=(',', ':')).encode()
encoded += b' ' * (-len(encoded) % 4)
binary.extend(b'\0' * (-len(binary) % 4))
result = struct.pack('<4sII', b'glTF', 2, 12+8+len(encoded)+8+len(binary))
result += struct.pack('<I4s', len(encoded), b'JSON') + encoded
result += struct.pack('<I4s', len(binary), b'BIN\0') + binary
DESTINATION.parent.mkdir(parents=True, exist_ok=True)
DESTINATION.write_bytes(result)
print(f'Created {DESTINATION.name}: {len(bones)} joints, {len(output["meshes"])} skinned meshes, {len(result)} bytes')
