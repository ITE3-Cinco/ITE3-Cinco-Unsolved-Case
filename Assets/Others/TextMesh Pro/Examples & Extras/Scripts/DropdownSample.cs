// Purpose: Displays the selected values of two dropdown controls.
// Use: Assign the two dropdowns and text, then connect a button to OnButtonClick.
// How it works: Read both values and show a selection message or the missing-selection message.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// TextMesh Pro supplies the text components, fonts and text-mesh information used here.
using TMPro;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;

public class DropdownSample: MonoBehaviour
{
	// text: text. Assigned in the scene/prefab Inspector; the initializer below is the default.
	[SerializeField]
	private TextMeshProUGUI text = null;

	// dropdownWithoutPlaceholder: dropdown without placeholder. Assigned in the scene/prefab Inspector; the
	// initializer below is the default.
	[SerializeField]
	private TMP_Dropdown dropdownWithoutPlaceholder = null;

	// dropdownWithPlaceholder: dropdown with placeholder. Assigned in the scene/prefab Inspector; the initializer
	// below is the default.
	[SerializeField]
	private TMP_Dropdown dropdownWithPlaceholder = null;

	// Read the dropdown values and write the selection result into the assigned text.
	public void OnButtonClick()
	{
		// Display "Selected values:\n" + dropdownWithoutPlaceholder.value + " - " + dropdownWithPlaceholder.value
		// (calculated from the values shown) when dropdownWithPlaceholder.value is above negative 1; otherwise "Error:
		// Please make a selection" in text.text (the assigned TMP/UI text component).
		text.text = dropdownWithPlaceholder.value > -1 ? "Selected values:\n" + dropdownWithoutPlaceholder.value + " - " + dropdownWithPlaceholder.value : "Error: Please make a selection";
	}
}
