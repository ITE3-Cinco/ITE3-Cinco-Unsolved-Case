// Purpose: Accepts only characters from 0 to 9 in a sample input field.
// Use: Assign an instance as a TMP input field custom validator.
// How it works: Validation appends accepted digits and advances the insertion position.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System supplies .NET types, delegates and attributes such as Serializable.
using System;


namespace TMPro
{
    /// <summary>
    /// EXample of a Custom Character Input Validator to only allow digits from 0 to 9.
    /// </summary>
    [Serializable]
    //[CreateAssetMenu(fileName = "InputValidator - Digits.asset", menuName = "TextMeshPro/Input Validators/Digits", order = 100)]
    public class TMP_DigitValidator : TMP_InputValidator
    {
        // Accept a digit by appending it and advancing pos; return a zero character to reject other input.
        // Custom text input validation function
        // text is a string argument supplied by the code, event or Inspector callback invoking Validate.
        // pos is a int argument supplied by the code, event or Inspector callback invoking Validate.
        // ch is a char argument supplied by the code, event or Inspector callback invoking Validate.
        public override char Validate(ref string text, ref int pos, char ch)
        {
            // If ch is at least '0' and ch is at most '9', run the following block.
            if (ch >= '0' && ch <= '9')
            {
                // Add ch to text and store the updated value there.
                text += ch;
                // Add 1 to pos and store the updated value there.
                pos += 1;
                // Return ch to the method caller.
                return ch;
            }

            // Return 0 viewed as char to the method caller.
            return (char)0;
        }
    }
}
