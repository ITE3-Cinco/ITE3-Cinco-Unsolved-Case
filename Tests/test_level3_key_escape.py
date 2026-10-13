# Use: run with python3 -m unittest discover -s Tests from the repository root.
# How it works: read asset files, follow serialized references and assert the expected wiring.
# These checks do not launch the game; Unity tests cover runtime behavior.

"""Serialized Level 3 key, stamina and caught-camera wiring.

Behavior is covered by Level3GameplayTests in Unity; these checks require no editor.
"""
# Import re for reading Unity YAML fields and IDs with regular expressions.
import re
# Import unittest for running the test cases and reporting assertion failures.
import unittest
# Reuse ROOT, field, reference from Tests/test_collision_assets.py; that module defines repository paths and Unity
# field/fileID parsing.
from test_collision_assets import ROOT, field, reference


class Level3KeyEscapeTests(unittest.TestCase):
    # Load the shared scene and asset data once before this group of tests.
    @classmethod
    def setUpClass(cls):
        # Store text read from file ROOT / 'Assets/Scenes/Level3.unity' in text.
        text = (ROOT / 'Assets/Scenes/Level3.unity').read_text()
        # Store all regex matches extracted from text using the pattern written here in blocks.
        blocks = re.findall(r'^--- !u!(\d+) &(-?\d+)[^\n]*\n(.*?)(?=^--- !u!|\Z)', text, re.M | re.S)
        # Store dictionary mapping key to (int(kind), body) from blocks in cls.docs.
        cls.docs = {key: (int(kind), body) for kind, key, body in blocks}
        # Run this branch only if length of cls.docs differs from length of blocks.
        if len(cls.docs) != len(blocks):
            # Raise AssertionError('Duplicate Level3 file IDs') because the loaded asset data violates this
            # required structure.
            raise AssertionError('Duplicate Level3 file IDs')
        # Store the serialized 'Level3Flow' MonoBehaviour found in the test documents in cls.flow.
        cls.flow = cls.component('Level3Flow')

    # Find the serialized MonoBehaviour with the requested gameplay class name.
    @classmethod
    # name is the serialized field or gameplay component name requested by the caller.
    def component(cls, name):
        # Return the first matching entry from sequence of body using (kind, body) from values stored in cls.docs
        # filtered by kind == 114 and field(body, 'm_EditorClassIdentifier') == 'Assembly-CSharp::' + name to the
        # helper caller.
        return next(body for kind, body in cls.docs.values()
                    if kind == 114 and field(body, 'm_EditorClassIdentifier') == 'Assembly-CSharp::' + name)

    # Check that key is connected to interaction and flow.
    def test_key_is_connected_to_interaction_and_flow(self):
        # Store the serialized 'Level3Key' MonoBehaviour found in the test documents in key.
        key = self.component('Level3Key')
        # Store fileID referenced by field 'm_GameObject' in key via test_collision_assets.reference in key_go.
        key_go = reference(key, 'm_GameObject')
        # Store fileID referenced by field 'flow' in key via test_collision_assets.reference in flow_id.
        flow_id = reference(key, 'flow')
        # Expect self.docs[flow_id][1] (read from self.docs[flow_id]) to equal self.flow.
        self.assertEqual(self.docs[flow_id][1], self.flow)
        # Expect serialized field 'm_Enabled' read from key by test_collision_assets.field to equal '1'.
        self.assertEqual(field(key, 'm_Enabled'), '1')
        # Store the first matching entry from sequence of (i, b) using (i, (k, b)) from key/value pairs stored in
        # self.docs filtered by k == 65 and reference(b, 'm_GameObject') == key_go in (collider_id, collider).
        collider_id, collider = next((i, b) for i, (k, b) in self.docs.items()
                                    if k == 65 and reference(b, 'm_GameObject') == key_go)
        # Expect serialized field 'm_IsTrigger' read from collider by test_collision_assets.field to equal '1'.
        self.assertEqual(field(collider, 'm_IsTrigger'), '1')
        # Expect serialized field 'm_Enabled' read from collider by test_collision_assets.field to equal '1'.
        self.assertEqual(field(collider, 'm_Enabled'), '1')
        # Store fileID referenced by field 'm_PrefabInstance' in self.docs[key_go][1] (read from
        # self.docs[key_go]) via test_collision_assets.reference in instance.
        instance = reference(self.docs[key_go][1], 'm_PrefabInstance')
        # Expect 'addedObject: {fileID: ' + collider_id + '}' (calculated from collider_id) to appear in
        # self.docs[instance][1] (read from self.docs[instance]).
        self.assertIn('addedObject: {fileID: ' + collider_id + '}', self.docs[instance][1])
        # Store text read from file ROOT / 'Assets/Scripts/Level3Key.cs.meta' in meta.
        meta = (ROOT / 'Assets/Scripts/Level3Key.cs.meta').read_text()
        # Expect re.search('guid: (\\w+)', meta)[1] (read from re.search('guid: (\\w+)', meta)) to appear in
        # serialized field 'm_Script' read from key by test_collision_assets.field.
        self.assertIn(re.search(r'guid: (\w+)', meta)[1], field(key, 'm_Script'))
        # Expect '[E]' to appear in serialized field 'promptMessage' read from key by test_collision_assets.field.
        self.assertIn('[E]', field(key, 'promptMessage'))
        # Store self.docs[reference(self.flow, 'objective')][1] (read from self.docs[reference(self.flow,
        # 'objective')]) in objective.
        objective = self.docs[reference(self.flow, 'objective')][1]
        # Expect serialized field 'primaryObjective' read from objective by test_collision_assets.field to equal
        # 'Find the key, then escape through the main door.'.
        self.assertEqual(field(objective, 'primaryObjective'), 'Find the key, then escape through the main door.')

    # Check that key inventory asset uses the supplied sprite.
    def test_key_inventory_asset_uses_the_supplied_sprite(self):
        # Store path ROOT joined with 'Assets/Assets/Inventory/Item_Key.asset' in item_path.
        item_path = ROOT / 'Assets/Assets/Inventory/Item_Key.asset'
        # Store text read from file item_path in item.
        item = item_path.read_text()
        # Store re.search('guid: (\\w+)', item_path.with_suffix('.asset.meta').read_text())[1] (read from
        # re.search('guid: (\\w+)', item_path.with_suffix('.asset.meta').read_text())) in item_guid.
        item_guid = re.search(r'guid: (\w+)', item_path.with_suffix('.asset.meta').read_text())[1]
        # Expect serialized field 'keyItem' read from self.flow by test_collision_assets.field to equal '{fileID:
        # 11400000, guid: ' + item_guid + ', type: 2}' (calculated from item_guid).
        self.assertEqual(field(self.flow, 'keyItem'),
                         '{fileID: 11400000, guid: ' + item_guid + ', type: 2}')
        # Expect serialized field 'm_EditorClassIdentifier' read from item by test_collision_assets.field to equal
        # 'Assembly-CSharp::ItemData'.
        self.assertEqual(field(item, 'm_EditorClassIdentifier'), 'Assembly-CSharp::ItemData')
        # Expect serialized field 'isAgimat' read from item by test_collision_assets.field to equal '0'.
        self.assertEqual(field(item, 'isAgimat'), '0')
        # Store text read from file ROOT / 'Assets/Scenes/Buttons-Icons/key icon.png.meta' in icon_meta.
        icon_meta = (ROOT / 'Assets/Scenes/Buttons-Icons/key icon.png.meta').read_text()
        # Store re.search('guid: (\\w+)', icon_meta)[1] (read from re.search('guid: (\\w+)', icon_meta)) in
        # icon_guid.
        icon_guid = re.search(r'guid: (\w+)', icon_meta)[1]
        # Store re.search('      internalID: (\\d+)', icon_meta)[1] (read from re.search('      internalID:
        # (\\d+)', icon_meta)) in icon_id.
        icon_id = re.search(r'      internalID: (\d+)', icon_meta)[1]
        # Expect serialized field 'itemIcon' read from item by test_collision_assets.field to equal '{fileID: ' +
        # icon_id + ', guid: ' + icon_guid + ', type: 3}' (calculated from icon_guid, icon_id).
        self.assertEqual(field(item, 'itemIcon'),
                         '{fileID: ' + icon_id + ', guid: ' + icon_guid + ', type: 3}')
        # Store the serialized 'Inventory' MonoBehaviour found in the test documents in inventory.
        inventory = self.component('Inventory')
        # Expect item_guid to be absent from inventory.
        self.assertNotIn(item_guid, inventory, 'The key must not be preloaded at spawn/retry')
        # Expect serialized field 'maxSlots' read from inventory by test_collision_assets.field converted to an
        # integer to be greater than 1.
        self.assertGreater(int(field(inventory, 'maxSlots')), 1)
        # Store self.docs[reference(inventory, 'hotbarUI')][1] (read from self.docs[reference(inventory,
        # 'hotbarUI')]) in hotbar.
        hotbar = self.docs[reference(inventory, 'hotbarUI')][1]
        # Expect 'slotIcons:' to appear in hotbar.
        self.assertIn('slotIcons:', hotbar)

    # Check that stamina boost is enabled only for level3.
    def test_stamina_boost_is_enabled_only_for_level3(self):
        # Store self.docs[reference(self.flow, 'player')][1] (read from self.docs[reference(self.flow, 'player')])
        # in player.
        player = self.docs[reference(self.flow, 'player')][1]
        # Expect serialized field 'useLowStaminaSprintBoost' read from player by test_collision_assets.field to
        # equal '1'.
        self.assertEqual(field(player, 'useLowStaminaSprintBoost'), '1')
        # Expect serialized field 'sprintBoostThreshold' read from player by test_collision_assets.field converted
        # to a number with decimals to equal 45.
        self.assertEqual(float(field(player, 'sprintBoostThreshold')), 45)
        # Expect serialized field 'runSpeed' read from player by test_collision_assets.field converted to a number
        # with decimals to equal 5.
        self.assertEqual(float(field(player, 'runSpeed')), 5)
        # Expect serialized field 'lowStaminaRunSpeed' read from player by test_collision_assets.field converted
        # to a number with decimals to equal 5.5.
        self.assertEqual(float(field(player, 'lowStaminaRunSpeed')), 5.5)
        # Process each path from paths below ROOT / 'Assets/Scenes' matching '*.unity'.
        for path in (ROOT / 'Assets/Scenes').rglob('*.unity'):
            # Run this branch only if path.name differs from 'Level3.unity'.
            if path.name != 'Level3.unity':
                # Expect 'useLowStaminaSprintBoost: 1' to be absent from text read from file path.
                self.assertNotIn('useLowStaminaSprintBoost: 1', path.read_text(), str(path))

    # Check that caught camera and translucent lose background resolve.
    def test_caught_camera_and_translucent_lose_background_resolve(self):
        # Store self.docs[reference(self.flow, 'caughtCamera')] (read from self.docs) in (camera_kind, camera).
        camera_kind, camera = self.docs[reference(self.flow, 'caughtCamera')]
        # Expect camera_kind to equal 20.
        self.assertEqual(camera_kind, 20)
        # Expect serialized field 'm_Enabled' read from camera by test_collision_assets.field to equal '1'.
        self.assertEqual(field(camera, 'm_Enabled'), '1')
        # Expect serialized field 'm_IsActive' read from self.docs[reference(camera, 'm_GameObject')][1] (read
        # from self.docs[reference(camera, 'm_GameObject')]) by test_collision_assets.field to equal '1'.
        self.assertEqual(field(self.docs[reference(camera, 'm_GameObject')][1], 'm_IsActive'), '1')
        # Expect serialized field 'caughtFaceDistance' read from self.flow by test_collision_assets.field
        # converted to a number with decimals to equal 0.65.
        self.assertEqual(float(field(self.flow, 'caughtFaceDistance')), .65)
        # Store self.docs[reference(self.flow, 'loseController')][1] (read from self.docs[reference(self.flow,
        # 'loseController')]) in lose.
        lose = self.docs[reference(self.flow, 'loseController')][1]
        # Store fileID referenced by field 'loseCanvas' in lose via test_collision_assets.reference in canvas_go.
        canvas_go = reference(lose, 'loseCanvas')
        # Store the first matching entry from sequence of i using (i, (k, b)) from key/value pairs stored in
        # self.docs filtered by k == 224 and reference(b, 'm_GameObject') == canvas_go in canvas_transform.
        canvas_transform = next(i for i, (k, b) in self.docs.items() if k == 224 and reference(b, 'm_GameObject') == canvas_go)
        # Store the first matching entry from sequence of reference(b, 'm_GameObject') using (k, b) from values
        # stored in self.docs filtered by k == 224 and reference(b, 'm_Father') == canvas_transform and
        # (field(self.docs[reference(b, 'm_GameObject')][1], 'm_Name') == 'Background') in background_go.
        background_go = next(reference(b, 'm_GameObject') for k, b in self.docs.values()
                             if k == 224 and reference(b, 'm_Father') == canvas_transform
                             and field(self.docs[reference(b, 'm_GameObject')][1], 'm_Name') == 'Background')
        # Store the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 114 and reference(b, 'm_GameObject') == background_go in image.
        image = next(b for k, b in self.docs.values() if k == 114 and reference(b, 'm_GameObject') == background_go)
        # Expect serialized field 'm_Color' read from image by test_collision_assets.field to equal '{r: 1, g: 1,
        # b: 1, a: 0.5}'.
        self.assertEqual(field(image, 'm_Color'), '{r: 1, g: 1, b: 1, a: 0.5}')

    # Check that lose hides all gameplay canvases without hiding its own ui.
    def test_lose_hides_all_gameplay_canvases_without_hiding_its_own_ui(self):
        # Store self.docs[reference(self.flow, 'loseController')][1] (read from self.docs[reference(self.flow,
        # 'loseController')]) in lose.
        lose = self.docs[reference(self.flow, 'loseController')][1]
        # Store all regex matches extracted from lose.split('  gameplayUIToHide:\n')[1] (read from lose.split('
        # gameplayUIToHide:\n')) using the pattern written here in hidden.
        hidden = re.findall(r'^  - \{fileID: (\d+)\}', lose.split('  gameplayUIToHide:\n')[1], re.M)
        # Store fileID referenced by field 'loseCanvas' in lose via test_collision_assets.reference in lose_go.
        lose_go = reference(lose, 'loseCanvas')
        # Store set of reference(body, 'm_GameObject') using (kind, body) from values stored in self.docs filtered
        # by kind == 223 and reference(body, 'm_GameObject') != lose_go in gameplay_canvases.
        gameplay_canvases = {reference(body, 'm_GameObject') for kind, body in self.docs.values()
                             if kind == 223 and reference(body, 'm_GameObject') != lose_go}
        # Expect a set containing hidden to equal gameplay_canvases.
        self.assertEqual(set(hidden), gameplay_canvases)
        # Expect length of hidden to equal length of gameplay_canvases.
        self.assertEqual(len(hidden), len(gameplay_canvases))
        # Expect lose_go to be absent from hidden.
        self.assertNotIn(lose_go, hidden)
        # Process each go from hidden.
        for go in hidden:
            # Expect self.docs[go][0] (read from self.docs[go]) to equal 1.
            self.assertEqual(self.docs[go][0], 1)
            # Expect serialized field 'm_IsActive' read from self.docs[go][1] (read from self.docs[go]) by
            # test_collision_assets.field to equal '1'.
            self.assertEqual(field(self.docs[go][1], 'm_IsActive'), '1', 'UI starts visible for gameplay')
        # Expect serialized field 'caughtLightIntensity' read from self.flow by test_collision_assets.field
        # converted to a number with decimals to be greater than 0.
        self.assertGreater(float(field(self.flow, 'caughtLightIntensity')), 0)

    # Check that renato audio and final ending reference current assets.
    def test_renato_audio_and_final_ending_reference_current_assets(self):
        # Store the serialized 'RenatoAudio' MonoBehaviour found in the test documents in audio.
        audio = self.component('RenatoAudio')
        # Process each (component, name, path, file_id) from ((audio, 'whistleClip', 'Assets/Audio/Renato
        # Audio/RenatoWhistling.mp3', 8300000), (audio, 'chaseClip', 'Assets/Audio/Renato Audio/ChaseAudio.mp3',
        # 8300000), (self.flow, 'endingClip', 'Assets/Assets/ENDING CLIP/final ending.mp4', 32900000)).
        for component, name, path, file_id in (
            (audio, 'whistleClip', 'Assets/Audio/Renato Audio/RenatoWhistling.mp3', 8300000),
            (audio, 'chaseClip', 'Assets/Audio/Renato Audio/ChaseAudio.mp3', 8300000),
            (self.flow, 'endingClip', 'Assets/Assets/ENDING CLIP/final ending.mp4', 32900000),
        ):
            # Store re.search('^guid: (\\w+)', (ROOT / (path + '.meta')).read_text(), re.M)[1] (read from
            # re.search('^guid: (\\w+)', (ROOT / (path + '.meta')).read_text(), re.M)) in guid.
            guid = re.search(r'^guid: (\w+)', (ROOT / (path + '.meta')).read_text(), re.M)[1]
            # Store '{fileID: ' + str(file_id) + ', guid: ' + guid + ', type: 3}' (calculated from guid, str,
            # file_id) in expected.
            expected = '{fileID: ' + str(file_id) + ', guid: ' + guid + ', type: 3}'
            # Expect whether ROOT / path exists as a file to be true.
            self.assertTrue((ROOT / path).is_file(), path)
            # Expect serialized field name read from component by test_collision_assets.field to equal expected.
            self.assertEqual(field(component, name), expected, path)
            # Run this branch only if component is audio.
            if component is audio:
                # Store self.docs[reference(audio, name.replace('Clip', 'Source'))][1] (read from
                # self.docs[reference(audio, name.replace('Clip', 'Source'))]) in source.
                source = self.docs[reference(audio, name.replace('Clip', 'Source'))][1]
                # Expect serialized field 'm_Resource' read from parsed metadata from the original unrigged glTF
                # by test_collision_assets.field to equal expected.
                self.assertEqual(field(source, 'm_Resource'), expected, path)

    # Check that lose buttons have click targets and loadable destinations.
    def test_lose_buttons_have_click_targets_and_loadable_destinations(self):
        # Store the serialized 'LoseSceneController' MonoBehaviour found in the test documents in lose.
        lose = self.component('LoseSceneController')
        # Store text read from file ROOT / 'ProjectSettings/EditorBuildSettings.asset' in builds.
        builds = (ROOT / 'ProjectSettings/EditorBuildSettings.asset').read_text()
        # Process each (button_name, scene_field) from (('tryAgainButton', 'retrySceneName'), ('exitButton',
        # 'exitSceneName')).
        for button_name, scene_field in (('tryAgainButton', 'retrySceneName'), ('exitButton', 'exitSceneName')):
            # Store self.docs[reference(lose, button_name)][1] (read from self.docs[reference(lose, button_name)])
            # in button.
            button = self.docs[reference(lose, button_name)][1]
            # Expect serialized field 'm_Enabled' read from button by test_collision_assets.field to equal '1'.
            self.assertEqual(field(button, 'm_Enabled'), '1')
            # Expect serialized field 'm_Interactable' read from button by test_collision_assets.field to equal
            # '1'.
            self.assertEqual(field(button, 'm_Interactable'), '1')
            # Store self.docs[reference(button, 'm_TargetGraphic')][1] (read from self.docs[reference(button,
            # 'm_TargetGraphic')]) in graphic.
            graphic = self.docs[reference(button, 'm_TargetGraphic')][1]
            # Expect serialized field 'm_RaycastTarget' read from graphic by test_collision_assets.field to equal
            # '1'.
            self.assertEqual(field(graphic, 'm_RaycastTarget'), '1')
            # Store serialized field scene_field read from lose by test_collision_assets.field in destination.
            destination = field(lose, scene_field)
            # Expect 'enabled: 1\n    path: Assets/Scenes/' + destination + '.unity' (calculated from destination)
            # to appear in builds.
            self.assertIn('enabled: 1\n    path: Assets/Scenes/' + destination + '.unity', builds)
        # Store the first matching entry from sequence of b using (k, b) from values stored in self.docs filtered
        # by k == 223 and reference(b, 'm_GameObject') == reference(lose, 'loseCanvas') in canvas.
        canvas = next(b for k, b in self.docs.values()
                      if k == 223 and reference(b, 'm_GameObject') == reference(lose, 'loseCanvas'))
        # Expect serialized field 'm_SortingOrder' read from canvas by test_collision_assets.field to equal '100'.
        self.assertEqual(field(canvas, 'm_SortingOrder'), '100')
