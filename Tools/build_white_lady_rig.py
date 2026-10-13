"""Build a basic skinned, T-pose White Lady from the original unrigged glTF.
Requires Python 3 + numpy. Original geometry, UVs, texture and attribution retained.
The gown follows hips/thighs gently; hair follows the head. Review deformation in Unity.
"""
# Import copy for deep copying source materials/textures without shared nested state.
import copy
# Import json for encoding and decoding glTF/GLB JSON metadata.
import json
# Import Path from Python pathlib to build paths relative to this script and read asset files.
from pathlib import Path
# Import struct for packing/unpacking binary headers and typed accessor data.
import struct
# Import numpy as np for array math for skin weights, rotations and mesh positions.
import numpy as np

# Run from any directory with Python 3 and NumPy; paths are relative to this repository.
# This rebuilds Assets/Resources/WhiteLadyRig.glb for Unity import, outside gameplay.
# Resolve this Python file path, then move up two directory levels to locate the repository root in ROOT.
ROOT = Path(__file__).resolve().parents[1]
# Join ROOT with the original horror-girl model directory; SOURCE is where scene.gltf, its geometry buffer and
# textures originate.
SOURCE = ROOT / 'Assets/Assets/free assets/a_horror_girl_character_code99type101'
# Choose the Unity Resources output path; running this tool overwrites WhiteLadyRig.glb there.
DESTINATION = ROOT / 'Assets/Resources/WhiteLadyRig.glb'
# Read scene.gltf from SOURCE and decode its JSON into source, the original model metadata.
source = json.loads((SOURCE / 'scene.gltf').read_text())
# Read the file URI from the first glTF buffer entry, resolve it inside SOURCE, and load its bytes into
# source_buffer.
source_buffer = (SOURCE / source['buffers'][0]['uri']).read_bytes()
# Create the output scene while retaining source material, texture and attribution data.
# Start one output scene and model root, with empty mesh, bone, skin and buffer lists. Deep-copy source
# material, texture and sampler metadata so changes to output do not change source.
output = {'asset': {'version': '2.0', 'generator': 'White Lady basic humanoid rig',
                     'copyright': 'Based on A HORROR GIRL CHARACTER CODE99/type101 by yongtun, CC BY 4.0; local rig and skinning added. See Documentation/GhostNavigationAndAnimation.md.'},
          'scene': 0, 'scenes': [{'nodes': [0]}], 'nodes': [{'name': 'WhiteLadyRig', 'children': []}],
          'meshes': [], 'skins': [], 'accessors': [], 'bufferViews': [],
          'materials': copy.deepcopy(source['materials']),
          'textures': copy.deepcopy(source['textures']), 'images': [],
          'samplers': copy.deepcopy(source.get('samplers', []))}
# Start a mutable byte buffer that accumulates vertex data, skin data and embedded image bytes.
binary = bytearray()


# Append bytes on a four-byte boundary and return the buffer-view index used by accessors.
# data is a byte sequence from array.tobytes() or a texture file read by the rig-building caller.
def buffer_view(data):
    # Append zero bytes until binary ends on a four-byte boundary required by glTF buffer data.
    binary.extend(b'\0' * (-len(binary) % 4))
    # Use the current buffer-view count as the zero-based ID of the next appended view.
    index = len(output['bufferViews'])
    # Record the current binary offset and data byte length; buffer 0 is the single embedded output buffer.
    output['bufferViews'].append({'buffer': 0, 'byteOffset': len(binary), 'byteLength': len(data)})
    # Append all bytes/entries from data to binary.
    binary.extend(data)
    # Return index to the helper caller.
    return index


# Describe one typed array: its buffer location, element count, shape and optional bounds.
# data is the NumPy array passed by the rig-building caller. component_type is a glTF numeric
# type code from the caller (5126=float, 5123=unsigned short, 5125=unsigned int). kind is the glTF accessor shape
# from the caller, such as SCALAR, VEC3 or MAT4. bounds is the caller flag requesting per-axis min/max values for
# position data.
def accessor(data, component_type, kind, bounds=False):
    # Use the current accessor count as the zero-based ID of the next typed array description.
    index = len(output['accessors'])
    # Append data bytes through buffer_view, then describe their location, numeric type, row count and shape in
    # item.
    item = {'bufferView': buffer_view(data.tobytes()), 'componentType': component_type,
            'count': len(data), 'type': kind}
    # Run this branch only if bounds.
    if bounds:
        # Find the smallest and largest value in each array column; convert them to JSON lists for accessor
        # bounds.
        item.update(min=data.min(axis=0).tolist(), max=data.max(axis=0).tolist())
    # Append item to output['accessors'] for later lookup or binary output.
    output['accessors'].append(item)
    # Return index to the helper caller.
    return index


# Decode a source accessor into a copied NumPy array using its component type and byte offset.
# index is an accessor ID from the source glTF or generated GLB metadata, supplied by the caller.
def read(index):
    # Retrieve the original accessor metadata using the caller index; a describes how to decode that array.
    a = source['accessors'][index]
    # Follow a.bufferView into source bufferViews; v gives the binary region containing the array.
    v = source['bufferViews'][a['bufferView']]
    # Translate the glTF componentType in a into a little-endian NumPy float32, uint32 or uint16 dtype.
    dtype = {5126: '<f4', 5125: '<u4', 5123: '<u2'}[a['componentType']]
    # Translate the accessor shape in a into one, two or three numbers per array row.
    width = {'SCALAR': 1, 'VEC2': 2, 'VEC3': 3}[a['type']]
    # Decode source_buffer at the combined buffer-view/accessor offset. Reshape into rows of width and return a
    # copy that callers may edit independently.
    return np.frombuffer(source_buffer, dtype=dtype, count=a['count'] * width,
                         offset=v.get('byteOffset', 0) + a.get('byteOffset', 0)).reshape(-1, width).copy()


# Model coordinates are Y-up, centered at the waist. Shift the feet to Y=0.
# The hands/forearms are fitted to the original A-pose, then lifted to a T-pose.
# Start the ordered bone-definition list; bone() appends name, parent, start and end for each joint.
bones = []

# Store a named bone, its parent and its start/end points for later distance-based skinning.
# name is the Humanoid bone name supplied by a bone-definition call. parent is the parent bone name supplied by
# the bone-definition call; None marks a skeleton root. position is the rest-pose start coordinate supplied by a
# bone-definition call. end is the rest-pose end coordinate used to measure vertex distance to this bone.
def bone(name, parent, position, end):
    # Append the caller bone name and parent with floating-point arrays for its segment start and end.
    bones.append((name, parent, np.array(position, float), np.array(end, float)))

# Define torso bones and mirrored limbs with Unity-compatible Humanoid names.
# Register bone 'Hips', its parent None (no result/reference) and its start/end coordinates in the ordered bones
# list.
bone('Hips', None, (0, .95, 0), (0, 1.13, 0))
# Register bone 'Spine', its parent 'Hips' and its start/end coordinates in the ordered bones list.
bone('Spine', 'Hips', (0, 1.13, 0), (0, 1.32, 0))
# Register bone 'Chest', its parent 'Spine' and its start/end coordinates in the ordered bones list.
bone('Chest', 'Spine', (0, 1.32, 0), (0, 1.53, 0))
# Register bone 'Neck', its parent 'Chest' and its start/end coordinates in the ordered bones list.
bone('Neck', 'Chest', (0, 1.53, 0), (0, 1.67, 0))
# Register bone 'Head', its parent 'Neck' and its start/end coordinates in the ordered bones list.
bone('Head', 'Neck', (0, 1.67, 0), (0, 1.96, 0))
# Process each (side, sign) from [('Left', 1), ('Right', -1)].
for side, sign in [('Left', 1), ('Right', -1)]:
    # Register bone side + 'Shoulder' (calculated from side), its parent 'Chest' and its start/end coordinates in
    # the ordered bones list.
    bone(side+'Shoulder', 'Chest', (sign*.08, 1.48, 0), (sign*.18, 1.47, 0))
    # Register bone side + 'UpperArm' (calculated from side), its parent side + 'Shoulder' (calculated from side)
    # and its start/end coordinates in the ordered bones list.
    bone(side+'UpperArm', side+'Shoulder', (sign*.18, 1.47, 0), (sign*.33, 1.18, 0))
    # Register bone side + 'LowerArm' (calculated from side), its parent side + 'UpperArm' (calculated from side)
    # and its start/end coordinates in the ordered bones list.
    bone(side+'LowerArm', side+'UpperArm', (sign*.33, 1.18, 0), (sign*.48, .97, .01))
    # Register bone side + 'Hand' (calculated from side), its parent side + 'LowerArm' (calculated from side) and
    # its start/end coordinates in the ordered bones list.
    bone(side+'Hand', side+'LowerArm', (sign*.48, .97, .01), (sign*.56, .86, .01))
    # Register bone side + 'UpperLeg' (calculated from side), its parent 'Hips' and its start/end coordinates in
    # the ordered bones list.
    bone(side+'UpperLeg', 'Hips', (sign*.10, .94, 0), (sign*.10, .51, .005))
    # Register bone side + 'LowerLeg' (calculated from side), its parent side + 'UpperLeg' (calculated from side)
    # and its start/end coordinates in the ordered bones list.
    bone(side+'LowerLeg', side+'UpperLeg', (sign*.10, .51, .005), (sign*.10, .09, 0))
    # Register bone side + 'Foot' (calculated from side), its parent side + 'LowerLeg' (calculated from side) and
    # its start/end coordinates in the ordered bones list.
    bone(side+'Foot', side+'LowerLeg', (sign*.10, .09, 0), (sign*.10, .045, .12))
    # Register bone side + 'Toes' (calculated from side), its parent side + 'Foot' (calculated from side) and its
    # start/end coordinates in the ordered bones list.
    bone(side+'Toes', side+'Foot', (sign*.10, .045, .12), (sign*.10, .045, .18))

# Index bone names, copy their rest positions and prepare rotations for the T-pose.
# Store dictionary mapping name to i from index-and-entry pairs from bones in indices.
indices = {name: i for i, (name, _, _, _) in enumerate(bones)}
# Read tuple entry 2 (the original start point) from each bone into the rest-pose position list.
rest_positions = [b[2] for b in bones]
# Copy each rest-position array into a separate T-pose list so arm changes do not modify the original rest
# positions.
t_positions = [v.copy() for v in rest_positions]
# Create one 3-by-3 identity rotation per bone; arm rotations are replaced while untouched body bones keep
# identity.
rotations = [np.eye(3) for _ in bones]
# Process each (side, sign) from [('Left', 1), ('Right', -1)].
for side, sign in [('Left', 1), ('Right', -1)]:
    # Look up the current side upper-arm bone index from the name-to-index dictionary.
    shoulder = indices[side+'UpperArm']
    # Start the arm chain at its upper-arm T-pose position; anchor initially refers to that position array.
    anchor = t_positions[shoulder]
    # Process each suffix from ['UpperArm', 'LowerArm', 'Hand'].
    for suffix in ['UpperArm', 'LowerArm', 'Hand']:
        # Combine Left/Right with the limb name and retrieve its bone index from indices.
        i = indices[side+suffix]
        # Subtract this bone start point (tuple entry 2) from its end point (entry 3) to get the rest-pose segment
        # direction.
        direction = bones[i][3] - bones[i][2]
        # Calculate the XY-plane angle needed to lift this arm segment horizontally; sign mirrors the right and
        # left arms.
        angle = -np.arctan2(direction[1], abs(direction[0])) * sign
        # Calculate cosine and sine of the angle in radians for the next rotation matrix.
        c, s = np.cos(angle), np.sin(angle)
        # Store a rotation around the Z axis for bone i; c and s rotate its lowered arm toward the horizontal
        # T-pose.
        rotations[i] = np.array([[c, -s, 0], [s, c, 0], [0, 0, 1]])
        # Store an independent copy of the current chain anchor as this bone T-pose start position.
        t_positions[i] = anchor.copy()
        # Advance the chain anchor along X by the original bone length; sign chooses the left or right direction.
        anchor += np.array([sign*np.linalg.norm(direction), 0, 0])

# Write each bone as a child node using a translation relative to its parent.
# Process each (i, (name, parent, _, _)) from index-and-entry pairs from bones.
for i, (name, parent, _, _) in enumerate(bones):
    # Look up this bone parent index using indices; root bones use None because they have no parent.
    parent_index = indices[parent] if parent else None
    # Subtract the parent T-pose world position from this bone position to obtain the glTF parent-relative
    # translation.
    position = t_positions[i] - (t_positions[parent_index] if parent else 0)
    # Create the bone glTF node with a parent-relative translation and an initially empty child list.
    output['nodes'].append({'name': name, 'translation': position.tolist(), 'children': []})
    # Connect this node to its parent; bone node indices are offset by one because output node 0 is the model
    # root.
    output['nodes'][parent_index+1 if parent else 0]['children'].append(i+1)

# Inverse bind matrices undo the rest pose before animated bone transforms move the vertices.
# Allocate one 4-by-4 identity matrix per bone; the next loop inserts each inverse rest-pose translation.
inverse = np.tile(np.eye(4), (len(bones), 1, 1))
# Process each (i, pos) from index-and-entry pairs from t_positions.
for i, pos in enumerate(t_positions):
    # Write negative bone position into the translation column of inverse matrix i, undoing its rest-pose
    # location.
    inverse[i, :3, 3] = -pos
# glTF matrices are column-major.
# Transpose each inverse matrix to glTF column-major order, encode float32 values, and store its MAT4 accessor ID
# in ibm.
ibm = accessor(inverse.transpose(0, 2, 1).astype('<f4').reshape(-1, 16), 5126, 'MAT4')
# Append a dictionary containing 'name', 'joints', 'skeleton', 'inverseBindMatrices' as keys to output['skins']
# for later lookup or binary output.
output['skins'].append({'name': 'WhiteLadyHumanoid', 'joints': list(range(1, len(bones)+1)),
                        'skeleton': 1, 'inverseBindMatrices': ibm})

# Process each source mesh: estimate skin weights, raise its arms and write skinned geometry.
# Process each original_mesh from source['meshes'] (read from source).
for original_mesh in source['meshes']:
    # Read the first geometry primitive from this original mesh; its accessors provide positions, normals, UVs and
    # indices.
    prim = original_mesh['primitives'][0]
    # Store source glTF accessor prim['attributes']['POSITION'] (read from prim['attributes']) decoded into a
    # copied NumPy array in vertices. Add the model floor-alignment offset to every vertex Y coordinate; column 1
    # is the vertical axis.
    vertices = read(prim['attributes']['POSITION']); vertices[:, 1] += 1.00018
    # Use the primitive NORMAL accessor ID to decode source surface-normal vectors.
    normals = read(prim['attributes']['NORMAL'])
    # Measure each vertex distance to every bone segment; closer bones receive stronger influence.
    # Store an empty list in distances.
    distances = []
    # Process each (_, _, start, end) from bones.
    for _, _, start, end in bones:
        # Subtract the bone start from its end to obtain the segment direction used for nearest-point distances.
        direction = end-start
        # Project each vertex onto the bone segment and clamp the fraction from 0 to 1 so the nearest point stays
        # between its ends.
        t = np.clip(((vertices-start) @ direction) / np.dot(direction, direction), 0, 1)
        # Calculate each vertex distance from its nearest point on this bone segment; save the distances for skin
        # weighting.
        distances.append(np.linalg.norm(vertices-(start+t[:, None]*direction), axis=1))
    # Combine per-bone distance arrays into one table: each row is a vertex and each column is a bone.
    distances = np.stack(distances, axis=1)
    # Convert vertex-to-bone distances into influence strengths: nearer bones weigh more; the lower distance bound
    # avoids division by zero.
    weights = 1 / np.maximum(distances, .015)**4
    # Do not allow the gown or hanging hair to bind to nearby arms.
    # Process each side from ['Left', 'Right'].
    for side in ['Left', 'Right']:
        # Collect the four bone indices for this side arm from indices so their body-vertex influences can be
        # removed.
        arm = [indices[side+s] for s in ['Shoulder', 'UpperArm', 'LowerArm', 'Hand']]
        # Read vertex heights from Y column 1 and interpolate an estimated body half-width at each height.
        body_width = np.interp(vertices[:, 1], [0, .9, 1.12, 1.28, 1.5, 2], [.35, .30, .28, .18, .15, .15])
        # Mark vertices whose horizontal distance from the center is smaller than the estimated body half-width.
        torso = np.abs(vertices[:, 0]) < body_width
        # Set arm influence to zero for the marked torso vertices; this keeps the body from following nearby arms.
        weights[np.ix_(torso, arm)] = 0
    # Separate the lowered hands from the adjacent skirt before raising the arms.
    # Process each (side, sign) from [('Left', 1), ('Right', -1)].
    for side, sign in [('Left', 1), ('Right', -1)]:
        # Mark vertices on this side that lie outside the torso and above the lower skirt, identifying the lowered
        # hand region.
        hand_region = (vertices[:, 0]*sign > .35) & (vertices[:, 1] > .65)
        # List every bone index except this side upper arm, forearm and hand; these influences must be removed
        # from hand vertices.
        non_arm = [i for i, b in enumerate(bones) if b[0] not in
                   [side+s for s in ['UpperArm', 'LowerArm', 'Hand']]]
        # Clear non-arm influences in the hand region so raising the arm does not pull the nearby skirt.
        weights[np.ix_(hand_region, non_arm)] = 0
    # Most of the long gown follows the pelvis; a small leg influence suggests walking
    # without stretching the hem fully with each footstep.
    # Identify central vertices below waist height as the gown region using Y height and distance from X center.
    gown = (vertices[:, 1] < 1.08) & (np.abs(vertices[:, 0]) < .28)
    # Clear the gown existing weights before assigning the intended hips and leg blend.
    weights[gown] = 0
    # Give every gown vertex 80% influence from the Hips bone found through indices.
    weights[gown, indices['Hips']] = .8
    # Process each (side, sign) from [('Left', 1), ('Right', -1)].
    for side, sign in [('Left', 1), ('Right', -1)]:
        # Select only the gown vertices on the current left/right side.
        region = gown & (vertices[:, 0]*sign >= 0)
        # Give that side gown vertices 20% influence from its upper leg; hips supply the other 80%.
        weights[region, indices[side+'UpperLeg']] = .2
    # Keep long hair attached to the head rather than tearing across the shoulder.
    # Identify the narrow upper central region as hanging hair using vertex height and distance from the X center.
    hair = (vertices[:, 1] > 1.35) & (np.abs(vertices[:, 0]) < .13)
    # Clear existing influences for the hair vertices before attaching them to the head.
    weights[hair] = 0
    # Give hair vertices full influence from the Head bone found through indices.
    weights[hair, indices['Head']] = 1
    # Keep the four strongest influences per vertex and normalize their weights to sum to one.
    # Sort each vertex bone strengths and keep the last four indices, which are its strongest bone influences.
    joints = np.argsort(weights, axis=1)[:, -4:]
    # Read the four selected strength values from weights using the joint indices for each vertex.
    chosen = np.take_along_axis(weights, joints, axis=1)
    # Divide each selected weight by that vertex total so its four influences sum to one.
    chosen /= chosen.sum(axis=1, keepdims=True)
    # Point zero-weight slots to valid joint index 0; they still contribute no movement.
    joints[chosen == 0] = 0
    # Create a zero-filled position array matching vertices; weighted transformed positions accumulate here.
    t_vertices = np.zeros_like(vertices)
    # Create a zero-filled normal array matching normals; weighted rotated normals accumulate here.
    t_normals = np.zeros_like(normals)
    # Apply the weighted rest-to-T-pose transform to positions and rotations to surface normals.
    # Process each influence from integer sequence from 0 to 4 minus one.
    for influence in range(4):
        # Process each i from integer sequence from 0 to length of bones minus one.
        for i in range(len(bones)):
            # Mark vertices whose current influence slot points to bone i in the selected joints table.
            mask = joints[:, influence] == i
            # Read those vertices normalized weights for this influence slot; the extra axis allows multiplication
            # by XYZ vectors.
            w = chosen[mask, influence, None]
            # Move marked vertices relative to this bone rest position, rotate into the T-pose, add the new bone
            # position and accumulate the weighted result.
            t_vertices[mask] += ((vertices[mask]-rest_positions[i]) @ rotations[i].T + t_positions[i])*w
            # Rotate marked surface normals with this bone, multiply by its weights and add them to the output
            # normal array.
            t_normals[mask] += (normals[mask] @ rotations[i].T)*w
    # Normalize transformed normals back to unit length; the tiny lower bound avoids dividing a zero-length
    # vector.
    t_normals /= np.maximum(np.linalg.norm(t_normals, axis=1, keepdims=True), 1e-8)
    # Store positions, normals, UVs, bone indices and weights as glTF mesh attributes.
    # Store a dictionary containing 'POSITION', 'NORMAL', 'TEXCOORD_0', 'JOINTS_0', 'WEIGHTS_0' as keys in attrs.
    attrs = {'POSITION': accessor(t_vertices.astype('<f4'), 5126, 'VEC3', True),
             'NORMAL': accessor(t_normals.astype('<f4'), 5126, 'VEC3'),
             'TEXCOORD_0': accessor(read(prim['attributes']['TEXCOORD_0']).astype('<f4'), 5126, 'VEC2'),
             'JOINTS_0': accessor(joints.astype('<u2'), 5123, 'VEC4'),
             'WEIGHTS_0': accessor(chosen.astype('<f4'), 5126, 'VEC4')}
    # Follow the primitive triangle-index accessor ID to preserve its original numeric component type.
    index_accessor = source['accessors'][prim['indices']]
    # Use the current output mesh count as the index of the mesh appended next.
    mesh_id = len(output['meshes'])
    # Append a dictionary containing 'name', 'primitives' as keys to output['meshes'] for later lookup or binary
    # output.
    output['meshes'].append({'name': original_mesh['name'], 'primitives': [{'attributes': attrs,
        'indices': accessor(read(prim['indices']), index_accessor['componentType'], 'SCALAR'),
        'material': prim['material'], 'mode': 4}]})
    # Use the current output node count as the index of the mesh node appended next.
    node_id = len(output['nodes'])
    # Create a node referencing the new mesh and the first skin, retaining the original mesh name.
    output['nodes'].append({'name': original_mesh['name'], 'mesh': mesh_id, 'skin': 0})
    # Add the mesh node index to the output scene root list so the skinned mesh appears in the scene.
    output['scenes'][0]['nodes'].append(node_id)

# Embed source textures so the finished GLB contains both geometry and images.
# Process each item from source['images'] (read from source).
for item in source['images']:
    # Read this source image URI and join it to SOURCE to locate the original texture file.
    texture = SOURCE / item['uri']
    # Append a dictionary containing 'bufferView', 'mimeType' as keys to output['images'] for later lookup or
    # binary output.
    output['images'].append({'bufferView': buffer_view(texture.read_bytes()),
                             'mimeType': 'image/png' if texture.suffix.lower() == '.png' else 'image/jpeg'})
# Remove empty child lists and label vertex/index buffer views for graphics upload.
# Process each node from output['nodes'] (read from output).
for node in output['nodes']:
    # Run this branch only if dictionary value for key 'children' in node (None if absent) equals an empty list.
    if node.get('children') == []:
        # Remove node['children'] because this optional entry is unnecessary in the output.
        del node['children']
# Process each mesh from output['meshes'] (read from output).
for mesh in output['meshes']:
    # Process each primitive from mesh['primitives'] (read from mesh).
    for primitive in mesh['primitives']:
        # Process each index from values stored in primitive['attributes'].
        for index in primitive['attributes'].values():
            # Initialize output['bufferViews'][output['accessors'][index]['bufferView']]['target'] with 34962.
            output['bufferViews'][output['accessors'][index]['bufferView']]['target'] = 34962
        # Initialize output['bufferViews'][output['accessors'][primitive['indices']]['bufferView']]['target'] with
        # 34963.
        output['bufferViews'][output['accessors'][primitive['indices']]['bufferView']]['target'] = 34963
# Pack padded JSON and binary chunks with a GLB header, then overwrite the destination file.
# Describe the single embedded buffer using its accumulated byte length.
output['buffers'] = [{'byteLength': len(binary)}]
# Convert output metadata to compact JSON and encode it as UTF-8 bytes for the GLB JSON chunk.
encoded = json.dumps(output, separators=(',', ':')).encode()
# Pad JSON with spaces until its byte length is a multiple of four, as required by GLB.
encoded += b' ' * (-len(encoded) % 4)
# Append zero bytes until binary ends on a four-byte boundary required by glTF buffer data.
binary.extend(b'\0' * (-len(binary) % 4))
# Build the little-endian GLB header: magic glTF, version 2 and total length including both chunk headers and
# padded contents.
result = struct.pack('<4sII', b'glTF', 2, 12+8+len(encoded)+8+len(binary))
# Append the JSON chunk length/type header followed by the padded encoded metadata.
result += struct.pack('<I4s', len(encoded), b'JSON') + encoded
# Append the binary chunk length/type header followed by the padded geometry, skin and texture bytes.
result += struct.pack('<I4s', len(binary), b'BIN\0') + binary
# Create DESTINATION.parent output directory, including parents, if it does not already exist.
DESTINATION.parent.mkdir(parents=True, exist_ok=True)
# Write result to file DESTINATION; this overwrites the generated GLB asset.
DESTINATION.write_bytes(result)
# Print the generated asset name and joint/mesh/byte counts so the tool operator can confirm the output.
print(f'Created {DESTINATION.name}: {len(bones)} joints, {len(output["meshes"])} skinned meshes, {len(result)} bytes')
