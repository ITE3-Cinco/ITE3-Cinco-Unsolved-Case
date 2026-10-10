"""Serialized Level 3 key, stamina and caught-camera wiring.

Behavior is covered by Level3GameplayTests in Unity; these checks require no editor.
"""
import re
import unittest
from test_collision_assets import ROOT, field, reference


class Level3KeyEscapeTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        text = (ROOT / 'Assets/Scenes/Level3.unity').read_text()
        blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
        cls.docs = {key: (int(kind), body) for kind, key, body in blocks}
        if len(cls.docs) != len(blocks):
            raise AssertionError('Duplicate Level3 file IDs')
        cls.flow = cls.component('Level3Flow')

    @classmethod
    def component(cls, name):
        return next(body for kind, body in cls.docs.values()
                    if kind == 114 and field(body, 'm_EditorClassIdentifier') == 'Assembly-CSharp::' + name)

    def test_key_is_connected_to_interaction_and_flow(self):
        key = self.component('Level3Key')
        key_go = reference(key, 'm_GameObject')
        flow_id = reference(key, 'flow')
        self.assertEqual(self.docs[flow_id][1], self.flow)
        self.assertEqual(field(key, 'm_Enabled'), '1')
        collider_id, collider = next((i, b) for i, (k, b) in self.docs.items()
                                    if k == 65 and reference(b, 'm_GameObject') == key_go)
        self.assertEqual(field(collider, 'm_IsTrigger'), '1')
        self.assertEqual(field(collider, 'm_Enabled'), '1')
        instance = reference(self.docs[key_go][1], 'm_PrefabInstance')
        self.assertIn('addedObject: {fileID: ' + collider_id + '}', self.docs[instance][1])
        meta = (ROOT / 'Assets/Scripts/Level3Key.cs.meta').read_text()
        self.assertIn(re.search(r'guid: (\w+)', meta)[1], field(key, 'm_Script'))
        self.assertIn('[E]', field(key, 'promptMessage'))
        objective = self.docs[reference(self.flow, 'objective')][1]
        self.assertEqual(field(objective, 'primaryObjective'), 'Find the key, then escape through the main door.')

    def test_key_inventory_asset_uses_the_supplied_sprite(self):
        item_path = ROOT / 'Assets/Assets/Inventory/Item_Key.asset'
        item = item_path.read_text()
        item_guid = re.search(r'guid: (\w+)', item_path.with_suffix('.asset.meta').read_text())[1]
        self.assertEqual(field(self.flow, 'keyItem'),
                         '{fileID: 11400000, guid: ' + item_guid + ', type: 2}')
        self.assertEqual(field(item, 'm_EditorClassIdentifier'), 'Assembly-CSharp::ItemData')
        self.assertEqual(field(item, 'isAgimat'), '0')
        icon_meta = (ROOT / 'Assets/Scenes/Buttons-Icons/key icon.png.meta').read_text()
        icon_guid = re.search(r'guid: (\w+)', icon_meta)[1]
        icon_id = re.search(r'      internalID: (\d+)', icon_meta)[1]
        self.assertEqual(field(item, 'itemIcon'),
                         '{fileID: ' + icon_id + ', guid: ' + icon_guid + ', type: 3}')
        inventory = self.component('Inventory')
        self.assertNotIn(item_guid, inventory, 'The key must not be preloaded at spawn/retry')
        self.assertGreater(int(field(inventory, 'maxSlots')), 1)
        hotbar = self.docs[reference(inventory, 'hotbarUI')][1]
        self.assertIn('slotIcons:', hotbar)

    def test_stamina_boost_is_enabled_only_for_level3(self):
        player = self.docs[reference(self.flow, 'player')][1]
        self.assertEqual(field(player, 'useLowStaminaSprintBoost'), '1')
        self.assertEqual(float(field(player, 'sprintBoostThreshold')), 45)
        self.assertEqual(float(field(player, 'runSpeed')), 5)
        self.assertEqual(float(field(player, 'lowStaminaRunSpeed')), 5.5)
        for path in (ROOT / 'Assets/Scenes').rglob('*.unity'):
            if path.name != 'Level3.unity':
                self.assertNotIn('useLowStaminaSprintBoost: 1', path.read_text(), str(path))

    def test_caught_camera_and_translucent_lose_background_resolve(self):
        camera_kind, camera = self.docs[reference(self.flow, 'caughtCamera')]
        self.assertEqual(camera_kind, 20)
        self.assertEqual(field(camera, 'm_Enabled'), '1')
        self.assertEqual(field(self.docs[reference(camera, 'm_GameObject')][1], 'm_IsActive'), '1')
        self.assertEqual(float(field(self.flow, 'caughtFaceDistance')), .65)
        lose = self.docs[reference(self.flow, 'loseController')][1]
        canvas_go = reference(lose, 'loseCanvas')
        canvas_transform = next(i for i, (k, b) in self.docs.items() if k == 224 and reference(b, 'm_GameObject') == canvas_go)
        background_go = next(reference(b, 'm_GameObject') for k, b in self.docs.values()
                             if k == 224 and reference(b, 'm_Father') == canvas_transform
                             and field(self.docs[reference(b, 'm_GameObject')][1], 'm_Name') == 'Background')
        image = next(b for k, b in self.docs.values() if k == 114 and reference(b, 'm_GameObject') == background_go)
        self.assertEqual(field(image, 'm_Color'), '{r: 1, g: 1, b: 1, a: 0.2}')
