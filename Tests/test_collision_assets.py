# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Serialized collider regressions; run with python3 -m unittest discover -s Tests.

These checks need no Unity installation. Play Mode is still required to validate
CharacterController motion and imported mesh cooking.
"""
# Import collections for grouping components by their GameObject IDs.
import collections
# Import Path from Python pathlib to build paths relative to this script and read asset files.
from pathlib import Path
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest


# Resolve this Python file path, then move up two directory levels to locate the repository root in ROOT.
ROOT = Path(__file__).resolve().parents[1]
# Store ('Assets/Scenes/PoliceHQ.unity', 'Assets/Environment/Prefab/Environment.prefab') in ASSETS.
ASSETS = (
    "Assets/Scenes/PoliceHQ.unity",
    "Assets/Environment/Prefab/Environment.prefab",
)


# Read a named top-level field from one Unity YAML document.
# body is one Unity YAML document supplied by the test/helper caller. name is the serialized field or gameplay
# component name requested by the caller.
def field(body, name):
    # Store the first regex match in body using the pattern written here in match.
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", body, re.MULTILINE)
    # Return match[1] (read from match) when match; otherwise None (no result/reference) to the helper caller.
    return match[1] if match else None


# Extract the referenced Unity fileID from a serialized field.
# body is one Unity YAML document supplied by the test/helper caller. name is the serialized field or gameplay
# component name requested by the caller.
def reference(body, name):
    # Store serialized field name read from body by test_collision_assets.field or '' in value.
    value = field(body, name) or ""
    # Store the first regex match in value using the pattern written here in match.
    match = re.search(r"fileID: (-?\d+)", value)
    # Return match[1] (read from match) when match; otherwise None (no result/reference) to the helper caller.
    return match[1] if match else None


class CollisionAssetsTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Store an empty dictionary in cls.assets.
        cls.assets = {}
        # Process each path from the scene/prefab paths listed in ASSETS.
        for path in ASSETS:
            # Store text read from file ROOT / path in text.
            text = (ROOT / path).read_text()
            # Store all regex matches extracted from text using the pattern written here in blocks.
            blocks = re.findall(
                r"^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)",
                text, re.MULTILINE | re.DOTALL,
            )
            # Store dictionary mapping key to (int(kind), body) from blocks in documents.
            documents = {key: (int(kind), body) for kind, key, body in blocks}
            # Run this branch only if length of documents differs from length of blocks.
            if len(documents) != len(blocks):
                # Raise AssertionError(f'Duplicate Unity file IDs in {path}') because the loaded asset data
                # violates this required structure.
                raise AssertionError(f"Duplicate Unity file IDs in {path}")
            # Start a dictionary of component lists; accessing a new GameObject fileID creates its empty list
            # automatically.
            components = collections.defaultdict(list)
            # Process each (key, (kind, body)) from key/value pairs stored in documents.
            for key, (kind, body) in documents.items():
                # Read the component owning m_GameObject fileID and add this component ID, Unity class ID and
                # fields to that object list.
                components[reference(body, "m_GameObject")].append((key, kind, body))
            # Save both document lookup and per-GameObject component lookup under this asset path for all tests in
            # the class.
            cls.assets[path] = (documents, components)

    # Check that meshless hierarchy boxes cannot block walkways.
    def test_meshless_hierarchy_boxes_cannot_block_walkways(self):
        # Process each (path, (documents, components)) from key/value pairs stored in self.assets.
        for path, (documents, components) in self.assets.items():
            # Initialize checked with 0.
            checked = 0
            # Process each (key, (kind, body)) from key/value pairs stored in documents.
            for key, (kind, body) in documents.items():
                # Store serialized field 'm_Name' read from body by test_collision_assets.field or '' in name.
                name = field(body, "m_Name") or ""
                # Run this branch only if kind differs from 1 or not (name starts with 'DOOR_Hinge_' or name
                # equals 'PoliceHeadquarters').
                if kind != 1 or not (name.startswith("DOOR_Hinge_") or name == "PoliceHeadquarters"):
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Process each (_, component_kind, collider) from components[key] (read from components).
                for _, component_kind, collider in components[key]:
                    # Run this branch only if component_kind equals 65.
                    if component_kind == 65:
                        # Label this asset/object combination as a separate unittest subtest so a failure
                        # identifies the exact fixture.
                        with self.subTest(asset=path, object=name):
                            # Expect serialized field 'm_Enabled' read from collider by
                            # test_collision_assets.field to equal '0'.
                            self.assertEqual(field(collider, "m_Enabled"), "0")
                        # Add 1 to checked.
                        checked += 1
            # Expect checked to equal 24 when path equals ASSETS[0] (read from ASSETS); otherwise 23.
            self.assertEqual(checked, 24 if path == ASSETS[0] else 23, path)

    # Check that markers are triggers.
    def test_markers_are_triggers(self):
        # Process each (path, (documents, components)) from key/value pairs stored in self.assets.
        for path, (documents, components) in self.assets.items():
            # Initialize checked with 0.
            checked = 0
            # Process each (key, (kind, body)) from key/value pairs stored in documents.
            for key, (kind, body) in documents.items():
                # Run this branch only if kind differs from 1 or not (field(body, 'm_Name') or '' starts with
                # 'MARKER_').
                if kind != 1 or not (field(body, "m_Name") or "").startswith("MARKER_"):
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Process each (_, component_kind, collider) from components[key] (read from components).
                for _, component_kind, collider in components[key]:
                    # Run this branch only if component_kind equals 65.
                    if component_kind == 65:
                        # Label this asset/object combination as a separate unittest subtest so a failure
                        # identifies the exact fixture.
                        with self.subTest(asset=path, object=field(body, "m_Name")):
                            # Expect serialized field 'm_IsTrigger' read from collider by
                            # test_collision_assets.field to equal '1'.
                            self.assertEqual(field(collider, "m_IsTrigger"), "1")
                        # Add 1 to checked.
                        checked += 1
            # Expect checked to equal 6.
            self.assertEqual(checked, 6, path)

    # Check that prefab door leaves keep solid colliders.
    def test_prefab_door_leaves_keep_solid_colliders(self):
        # Store self.assets[ASSETS[1]] (read from self.assets) in (documents, components).
        documents, components = self.assets[ASSETS[1]]
        # Initialize checked with 0.
        checked = 0
        # Process each (key, (kind, body)) from key/value pairs stored in documents.
        for key, (kind, body) in documents.items():
            # Store serialized field 'm_Name' read from body by test_collision_assets.field or '' in name.
            name = field(body, "m_Name") or ""
            # Run this branch only if kind differs from 1 or not (name starts with 'DOOR_Leaf').
            if kind != 1 or not name.startswith("DOOR_Leaf"):
                # Skip this unmatched entry and continue with the next loop item.
                continue
            # Label this asset/object combination as a separate unittest subtest so a failure identifies the exact
            # fixture.
            with self.subTest(object=name):
                # Store list of b using (_, k, b) from components[key] (read from components) filtered by k in
                # (64, 65) in colliders.
                colliders = [b for _, k, b in components[key] if k in (64, 65)]
                # Expect length of colliders to equal 1.
                self.assertEqual(len(colliders), 1)
                # Expect serialized field 'm_Enabled' read from colliders[0] (read from colliders) by
                # test_collision_assets.field to equal '1'.
                self.assertEqual(field(colliders[0], "m_Enabled"), "1")
                # Expect serialized field 'm_IsTrigger' read from colliders[0] (read from colliders) by
                # test_collision_assets.field to equal '0'.
                self.assertEqual(field(colliders[0], "m_IsTrigger"), "0")
            # Add 1 to checked.
            checked += 1
        # Expect checked to equal 20.
        self.assertEqual(checked, 20)

    # Check that mesh colliders use their visible mesh and component link.
    def test_mesh_colliders_use_their_visible_mesh_and_component_link(self):
        # Process each (path, (documents, components)) from key/value pairs stored in self.assets.
        for path, (documents, components) in self.assets.items():
            # Process each (key, (kind, body)) from key/value pairs stored in documents.
            for key, (kind, body) in documents.items():
                # Run this branch only if kind differs from 1 or not (field(body, 'm_Name') or '' starts with
                # 'DOOR_Leaf').
                if kind != 1 or not (field(body, "m_Name") or "").startswith("DOOR_Leaf"):
                    # Skip this unmatched entry and continue with the next loop item.
                    continue
                # Store list of field(b, 'm_Mesh') using (_, k, b) from components[key] (read from components)
                # filtered by k == 33 in meshes.
                meshes = [field(b, "m_Mesh") for _, k, b in components[key] if k == 33]
                # Process each (collider_id, component_kind, collider) from components[key] (read from
                # components).
                for collider_id, component_kind, collider in components[key]:
                    # Run this branch only if component_kind differs from 64.
                    if component_kind != 64:
                        # Skip this unmatched entry and continue with the next loop item.
                        continue
                    # Label this asset/object combination as a separate unittest subtest so a failure identifies
                    # the exact fixture.
                    with self.subTest(asset=path, object=field(body, "m_Name")):
                        # Expect serialized field 'm_Mesh' read from collider by test_collision_assets.field to
                        # appear in meshes.
                        self.assertIn(field(collider, "m_Mesh"), meshes)
                        # Expect serialized field 'm_Convex' read from collider by test_collision_assets.field to
                        # equal '0'.
                        self.assertEqual(field(collider, "m_Convex"), "0")
                        # Expect formatted text inserting collider_id to appear in body.
                        self.assertIn(f"  - component: {{fileID: {collider_id}}}", body)


# Run the test runner only when this file is executed directly, not when another test imports its helpers.
if __name__ == "__main__":
    # Let Python unittest discover and run the test methods in this file.
    unittest.main()
