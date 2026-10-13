// Purpose: Demonstrates formatting input into a phone-number layout.
// Use: Assign an instance as a TMP input field custom validator in a sample.
// How it works: Insert parentheses, spacing and a dash based on the current character position.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// System supplies .NET types, delegates and attributes such as Serializable.
using System;

namespace TMPro
{
    /// <summary>
    /// Example of a Custom Character Input Validator to only allow phone number in the (800) 555-1212 format.
    /// </summary>
    [Serializable]
    //[CreateAssetMenu(fileName = "InputValidator - Phone Numbers.asset", menuName = "TextMeshPro/Input Validators/Phone Numbers")]
    public class TMP_PhoneNumberValidator : TMP_InputValidator
    {
        // Format the next input character and set the insertion position for the phone-number layout.
        // Custom text input validation function
        // text is a string argument supplied by the code, event or Inspector callback invoking Validate.
        // pos is a int argument supplied by the code, event or Inspector callback invoking Validate.
        // ch is a char argument supplied by the code, event or Inspector callback invoking Validate.
        public override char Validate(ref string text, ref int pos, char ch)
        {
            // Write the diagnostic message to the Unity Console; it does not update game UI.
            Debug.Log("Trying to validate...");
            
            // This sample checks whether ch is below 0 AND above 9 at the same time. No character can meet
            // both conditions, so this existing check never rejects input.
            if (ch < '0' && ch > '9') return (char)0;

            // Store length of text in local length.
            int length = text.Length;

            // Enforce Phone Number format for every character input.
            // Repeat this block while i is below length plus 1; start from int i = 0.
            for (int i = 0; i < length + 1; i++)
            {
                // Choose the matching branch using i.
                switch (i)
                {
                    case 0:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Set text to "(" plus ch.
                            text = "(" + ch;
                        // Set pos to 2.
                        pos = 2;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 1:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 2.
                        pos = 2;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 2:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 3.
                        pos = 3;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 3:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch plus ") " to text and store the updated value there.
                            text += ch + ") ";
                        // Set pos to 6.
                        pos = 6;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 4:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ") " plus ch to text and store the updated value there.
                            text += ") " + ch;
                        // Set pos to 7.
                        pos = 7;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 5:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add " " plus ch to text and store the updated value there.
                            text += " " + ch;
                        // Set pos to 7.
                        pos = 7;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 6:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 7.
                        pos = 7;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 7:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 8.
                        pos = 8;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 8:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch plus "-" to text and store the updated value there.
                            text += ch + "-";
                        // Set pos to 10.
                        pos = 10;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 9:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add "-" plus ch to text and store the updated value there.
                            text += "-" + ch;
                        // Set pos to 11.
                        pos = 11;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 10:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 11.
                        pos = 11;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 11:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 12.
                        pos = 12;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 12:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 13.
                        pos = 13;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                    case 13:
                        // If i equals length, run the following block.
                        if (i == length)
                            // Add ch to text and store the updated value there.
                            text += ch;
                        // Set pos to 14.
                        pos = 14;
                        // Leave this loop or switch case so no later statements in that block run.
                        break;
                }
            }

            // Return ch to the method caller.
            return ch;
        }
    }
}
