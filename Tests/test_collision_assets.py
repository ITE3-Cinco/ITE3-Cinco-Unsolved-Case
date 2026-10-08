"""Serialized collider regressions; run with python3 -m unittest discover -s Tests.

These checks need no Unity installation. Play Mode is still required to validate
CharacterController motion and imported mesh cooking.
"""
import collections
from pathlib import Path
import re
import unittest


ROOT = Path(__file__).resolve().parents[1]
ASSETS = (
    "Assets/Scenes/PoliceHQ.unity",
    "Assets/Environment/Prefab/Environment.prefab",
)


def field(body, name):
    match = re.search(r"^  " + re.escape(name) + r": (.*)$", body, re.MULTILINE)
    return match[1] if match else None


def reference(body, name):
    value = field(body, name) or ""
    match = re.search(r"fileID: (-?\d+)", value)
    return match[1] if match else None


class CollisionAssetsTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.assets = {}
        for path in ASSETS:
            text = (ROOT / path).read_text()
            blocks = re.findall(
                r"^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)",
                text, re.MULTILINE | re.DOTALL,
            )
            documents = {key: (int(kind), body) for kind, key, body in blocks}
            if len(documents) != len(blocks):
                raise AssertionError(f"Duplicate Unity file IDs in {path}")
            components = collections.defaultdict(list)
            for key, (kind, body) in documents.items():
                components[reference(body, "m_GameObject")].append((key, kind, body))
            cls.assets[path] = (documents, components)

    def test_meshless_hierarchy_boxes_cannot_block_walkways(self):
        for path, (documents, components) in self.assets.items():
            checked = 0
            for key, (kind, body) in documents.items():
                name = field(body, "m_Name") or ""
                if kind != 1 or not (name.startswith("DOOR_Hinge_") or name == "PoliceHeadquarters"):
                    continue
                for _, component_kind, collider in components[key]:
                    if component_kind == 65:
                        with self.subTest(asset=path, object=name):
                            self.assertEqual(field(collider, "m_Enabled"), "0")
                        checked += 1
            self.assertEqual(checked, 23, path)

    def test_markers_are_triggers(self):
        for path, (documents, components) in self.assets.items():
            checked = 0
            for key, (kind, body) in documents.items():
                if kind != 1 or not (field(body, "m_Name") or "").startswith("MARKER_"):
                    continue
                for _, component_kind, collider in components[key]:
                    if component_kind == 65:
                        with self.subTest(asset=path, object=field(body, "m_Name")):
                            self.assertEqual(field(collider, "m_IsTrigger"), "1")
                        checked += 1
            self.assertEqual(checked, 6, path)

    def test_prefab_door_leaves_keep_solid_colliders(self):
        documents, components = self.assets[ASSETS[1]]
        checked = 0
        for key, (kind, body) in documents.items():
            name = field(body, "m_Name") or ""
            if kind != 1 or not name.startswith("DOOR_Leaf"):
                continue
            with self.subTest(object=name):
                colliders = [b for _, k, b in components[key] if k in (64, 65)]
                self.assertEqual(len(colliders), 1)
                self.assertEqual(field(colliders[0], "m_Enabled"), "1")
                self.assertEqual(field(colliders[0], "m_IsTrigger"), "0")
            checked += 1
        self.assertEqual(checked, 20)

    def test_mesh_colliders_use_their_visible_mesh_and_component_link(self):
        for path, (documents, components) in self.assets.items():
            for key, (kind, body) in documents.items():
                if kind != 1 or not (field(body, "m_Name") or "").startswith("DOOR_Leaf"):
                    continue
                meshes = [field(b, "m_Mesh") for _, k, b in components[key] if k == 33]
                for collider_id, component_kind, collider in components[key]:
                    if component_kind != 64:
                        continue
                    with self.subTest(asset=path, object=field(body, "m_Name")):
                        self.assertIn(field(collider, "m_Mesh"), meshes)
                        self.assertEqual(field(collider, "m_Convex"), "0")
                        self.assertIn(f"  - component: {{fileID: {collider_id}}}", body)


if __name__ == "__main__":
    unittest.main()
