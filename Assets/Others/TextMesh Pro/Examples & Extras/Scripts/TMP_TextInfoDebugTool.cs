// Purpose: Draws Editor guides around text characters, words, links and lines.
// Use: Attach to sample TMP text and enable the desired debug guides in the Inspector.
// How it works: Read generated text geometry and draw its bounds with Gizmos and Editor handles.
// Context: bundled TextMesh Pro example; these notes explain its sample behavior.

// System supplies .NET types, delegates and attributes such as Serializable.
using System;
// UnityEngine supplies GameObjects, components, transforms, time, math and physics used here.
using UnityEngine;
// System.Collections supplies IEnumerator so coroutines can pause and resume across frames.
using System.Collections;
// UnityEditor supplies Edit Mode tools and asset/prefab operations; this code runs in the Editor.
using UnityEditor;


namespace TMPro.Examples
{

    public class TMP_TextInfoDebugTool : MonoBehaviour
    {
        // Since this script is used for debugging, we exclude it from builds.
        // TODO: Rework this script to make it into an editor utility.
        #if UNITY_EDITOR
        // ShowCharacters: show characters. Assigned in the scene/prefab Inspector.
        public bool ShowCharacters;
        // ShowWords: show words. Assigned in the scene/prefab Inspector.
        public bool ShowWords;
        // ShowLinks: show links. Assigned in the scene/prefab Inspector.
        public bool ShowLinks;
        // ShowLines: show lines. Assigned in the scene/prefab Inspector.
        public bool ShowLines;
        // ShowMeshBounds: show mesh bounds. Assigned in the scene/prefab Inspector.
        public bool ShowMeshBounds;
        // ShowTextBounds: show text bounds. Assigned in the scene/prefab Inspector.
        public bool ShowTextBounds;
        // ObjectStats: object stats. Updated by OnDrawGizmos.
        [Space(10)]
        [TextArea(2, 2)]
        public string ObjectStats;

        // m_TextComponent: text component. OnDrawGizmos retrieves it from the first TMP_Text component on this
        // GameObject. Inspector assignment may supply its initial value.
        [SerializeField]
        private TMP_Text m_TextComponent;

        // m_Transform: transform. OnDrawGizmos sets it from Transform of m_TextComponent.
        private Transform m_Transform;
        // m_TextInfo: text info. OnDrawGizmos sets it from m_TextComponent generated TMP character/line/mesh
        // information.
        private TMP_TextInfo m_TextInfo;

        // m_ScaleMultiplier: scale multiplier. Updated by OnDrawGizmos.
        private float m_ScaleMultiplier;
        // m_HandleSize: handle size. Updated by OnDrawGizmos.
        private float m_HandleSize;


        // In the Editor, draw only the text debug guides enabled in this component settings.
        void OnDrawGizmos()
        {
            // If m_TextComponent is missing, run the following block.
            if (m_TextComponent == null)
            {
                // Store the first TMP_Text component on this GameObject in m_TextComponent.
                m_TextComponent = GetComponent<TMP_Text>();

                // Stop this method early when m_TextComponent is missing.
                if (m_TextComponent == null)
                    // Return now; skip the rest of this method.
                    return;
            }

            // Set m_Transform to Transform of m_TextComponent.
            m_Transform = m_TextComponent.transform;

            // Get a reference to the text object's textInfo
            // Set m_TextInfo to m_TextComponent generated TMP character/line/mesh information.
            m_TextInfo = m_TextComponent.textInfo;

            // Update Text Statistics
            // Recalculate ObjectStats from "Characters: " + m_TextInfo.characterCount + " Words: " +
            // m_TextInfo.wordCount + " Spaces: " + m_TextInfo.spaceCount + " Sprites: " + m_TextInfo.spriteCount +
            // " Links: " + m_TextInfo.linkCount + "\nLines: " + m_TextInfo.lineCount + " Pages: " +
            // m_TextInfo.pageCount (calculated from the values shown).
            ObjectStats = "Characters: " + m_TextInfo.characterCount + "   Words: " + m_TextInfo.wordCount + "   Spaces: " + m_TextInfo.spaceCount + "   Sprites: " + m_TextInfo.spriteCount + "   Links: " + m_TextInfo.linkCount
                          + "\nLines: " + m_TextInfo.lineCount + "   Pages: " + m_TextInfo.pageCount;

            // Get the handle size for drawing the various
            // Set m_ScaleMultiplier to 1 when runtime type information for m_TextComponent equals the runtime type
            // TextMeshPro; otherwise 0.1.
            m_ScaleMultiplier = m_TextComponent.GetType() == typeof(TextMeshPro) ? 1 : 0.1f;
            // Recalculate m_HandleSize from HandleUtility.GetHandleSize(m_Transform.position) * m_ScaleMultiplier
            // (calculated from the values shown).
            m_HandleSize = HandleUtility.GetHandleSize(m_Transform.position) * m_ScaleMultiplier;

            // Draw line metrics
            #region Draw Lines
            // If ShowLines, run the following block.
            if (ShowLines)
                // Call TMP_TextInfoDebugTool.DrawLineBounds; draw Editor guides for line text bounds using
                // generated geometry.
                DrawLineBounds();
            #endregion

            // Draw word metrics
            #region Draw Words
            // If ShowWords, run the following block.
            if (ShowWords)
                // Call TMP_TextInfoDebugTool.DrawWordBounds; draw Editor guides for word text bounds using
                // generated geometry.
                DrawWordBounds();
            #endregion

            // Draw character metrics
            #region Draw Characters
            // If ShowCharacters, run the following block.
            if (ShowCharacters)
                // Call TMP_TextInfoDebugTool.DrawCharactersBounds; draw Editor guides for characters text bounds
                // using generated geometry.
                DrawCharactersBounds();
            #endregion

            // Draw Quads around each of the words
            #region Draw Links
            // If ShowLinks, run the following block.
            if (ShowLinks)
                // Call TMP_TextInfoDebugTool.DrawLinkBounds; draw Editor guides for link text bounds using
                // generated geometry.
                DrawLinkBounds();
            #endregion

            // Draw Quad around the bounds of the text
            #region Draw Bounds
            // If ShowMeshBounds, run the following block.
            if (ShowMeshBounds)
                // Call TMP_TextInfoDebugTool.DrawBounds; draw Editor guides for text bounds using generated
                // geometry.
                DrawBounds();
            #endregion

            // Draw Quad around the rendered region of the text.
            #region Draw Text Bounds
            // If ShowTextBounds, run the following block.
            if (ShowTextBounds)
                // Call TMP_TextInfoDebugTool.DrawTextBounds; draw Editor guides for text text bounds using
                // generated geometry.
                DrawTextBounds();
            #endregion
        }


        // Draw Editor guides for characters text bounds using generated geometry.
        /// <summary>
        /// Method to draw a rectangle around each character.
        /// </summary>
        /// <param name="text"></param>
        void DrawCharactersBounds()
        {
            // Store m_TextInfo.characterCount in local characterCount.
            int characterCount = m_TextInfo.characterCount;

            // Repeat this block while i is below characterCount; start from int i = 0.
            for (int i = 0; i < characterCount; i++)
            {
                // Draw visible as well as invisible characters
                // Store m_TextInfo.characterInfo[i] (entry read from m_TextInfo.characterInfo) in local
                // characterInfo.
                TMP_CharacterInfo characterInfo = m_TextInfo.characterInfo[i];

                // Store i is below m_TextComponent.maxVisibleCharacters and characterInfo.lineNumber is below
                // m_TextComponent.maxVisibleLines and i is at least m_TextComponent.firstVisibleCharacter in local
                // isCharacterVisible.
                bool isCharacterVisible = i < m_TextComponent.maxVisibleCharacters &&
                                          characterInfo.lineNumber < m_TextComponent.maxVisibleLines &&
                                          i >= m_TextComponent.firstVisibleCharacter;

                // If m_TextComponent.overflowMode equals TextOverflowModes.Page, run the following block.
                if (m_TextComponent.overflowMode == TextOverflowModes.Page)
                    // Set isCharacterVisible to isCharacterVisible and characterInfo.pageNumber plus 1 equals
                    // m_TextComponent.pageToDisplay.
                    isCharacterVisible = isCharacterVisible && characterInfo.pageNumber + 1 == m_TextComponent.pageToDisplay;

                // Skip this loop entry when not (isCharacterVisible).
                if (!isCharacterVisible)
                    // Skip this entry and continue with the next loop iteration.
                    continue;

                // Initialize dottedLineSize to 6.
                float dottedLineSize = 6;

                // Get Bottom Left and Top Right position of the current character
                // Store characterInfo.bottomLeft converted from m_Transform local coordinates into world
                // coordinates in local bottomLeft.
                Vector3 bottomLeft = m_Transform.TransformPoint(characterInfo.bottomLeft);
                // Store a Vector3 built from characterInfo.topLeft.x, characterInfo.topLeft.y, 0 converted from
                // m_Transform local coordinates into world coordinates in local topLeft.
                Vector3 topLeft = m_Transform.TransformPoint(new Vector3(characterInfo.topLeft.x, characterInfo.topLeft.y, 0));
                // Store characterInfo.topRight converted from m_Transform local coordinates into world coordinates
                // in local topRight.
                Vector3 topRight = m_Transform.TransformPoint(characterInfo.topRight);
                // Store a Vector3 built from characterInfo.bottomRight.x, characterInfo.bottomRight.y, 0 converted
                // from m_Transform local coordinates into world coordinates in local bottomRight.
                Vector3 bottomRight = m_Transform.TransformPoint(new Vector3(characterInfo.bottomRight.x, characterInfo.bottomRight.y, 0));

                // Draw character bounds
                // If characterInfo.isVisible, run the following block.
                if (characterInfo.isVisible)
                {
                    // Store Color.green in local color.
                    Color color = Color.green;
                    // Call TMP_TextInfoDebugTool.DrawDottedRectangle with bottomLeft, topRight, color; draw
                    // Rectangles.
                    DrawDottedRectangle(bottomLeft, topRight, color);
                }
                else
                {
                    // Store Color.grey in local color.
                    Color color = Color.grey;

                    // Store characterInfo.xAdvance when the result of Math.Abs using characterInfo.origin -
                    // characterInfo.xAdvance is above 0.01; otherwise characterInfo.origin +
                    // (characterInfo.ascender - characterInfo.descender) * 0.03f (calculated from the values
                    // shown) in local whiteSpaceAdvance.
                    float whiteSpaceAdvance = Math.Abs(characterInfo.origin - characterInfo.xAdvance) > 0.01f ? characterInfo.xAdvance : characterInfo.origin + (characterInfo.ascender - characterInfo.descender) * 0.03f;
                    // Call TMP_TextInfoDebugTool.DrawDottedRectangle with new Vector3(characterInfo.origin,
                    // characterInfo.descender, 0) converted from m_Transform local coordinates into world
                    // coordinates, new Vector3(whiteSpaceAdvance, characterInfo.ascender, 0) converted from
                    // m_Transform local coordinates into world coordinates, color, 4; draw Rectangles.
                    DrawDottedRectangle(m_Transform.TransformPoint(new Vector3(characterInfo.origin, characterInfo.descender, 0)), m_Transform.TransformPoint(new Vector3(whiteSpaceAdvance, characterInfo.ascender, 0)), color, 4);
                }

                // Store characterInfo.origin in local origin.
                float origin = characterInfo.origin;
                // Store characterInfo.xAdvance in local advance.
                float advance = characterInfo.xAdvance;
                // Store characterInfo.ascender in local ascentline.
                float ascentline = characterInfo.ascender;
                // Store characterInfo.baseLine in local baseline.
                float baseline = characterInfo.baseLine;
                // Store characterInfo.descender in local descentline.
                float descentline = characterInfo.descender;

                //Draw Ascent line
                // Store a Vector3 built from origin, ascentline, 0 converted from m_Transform local coordinates
                // into world coordinates in local ascentlineStart.
                Vector3 ascentlineStart = m_Transform.TransformPoint(new Vector3(origin, ascentline, 0));
                // Store a Vector3 built from advance, ascentline, 0 converted from m_Transform local coordinates
                // into world coordinates in local ascentlineEnd.
                Vector3 ascentlineEnd = m_Transform.TransformPoint(new Vector3(advance, ascentline, 0));

                // Set Handles.color to Color.cyan.
                Handles.color = Color.cyan;
                // Call Handles.DrawDottedLine using ascentlineStart, ascentlineEnd, dottedLineSize.
                Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);

                // Draw Cap Height & Mean line
                // Store 0 when characterInfo.fontAsset is missing; otherwise baseline +
                // characterInfo.fontAsset.faceInfo.capLine * characterInfo.scale (calculated from the values
                // shown) in local capline.
                float capline = characterInfo.fontAsset == null ? 0 : baseline + characterInfo.fontAsset.faceInfo.capLine * characterInfo.scale;
                // Store a Vector3 built from topLeft X component, m_Transform.TransformPoint(new Vector3(0,
                // capline, 0)) Y component, 0 in local capHeightStart.
                Vector3 capHeightStart = new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0, capline, 0)).y, 0);
                // Store a Vector3 built from topRight X component, m_Transform.TransformPoint(new Vector3(0,
                // capline, 0)) Y component, 0 in local capHeightEnd.
                Vector3 capHeightEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0, capline, 0)).y, 0);

                // Store 0 when characterInfo.fontAsset is missing; otherwise baseline +
                // characterInfo.fontAsset.faceInfo.meanLine * characterInfo.scale (calculated from the values
                // shown) in local meanline.
                float meanline = characterInfo.fontAsset == null ? 0 : baseline + characterInfo.fontAsset.faceInfo.meanLine * characterInfo.scale;
                // Store a Vector3 built from topLeft X component, m_Transform.TransformPoint(new Vector3(0,
                // meanline, 0)) Y component, 0 in local meanlineStart.
                Vector3 meanlineStart = new Vector3(topLeft.x, m_Transform.TransformPoint(new Vector3(0, meanline, 0)).y, 0);
                // Store a Vector3 built from topRight X component, m_Transform.TransformPoint(new Vector3(0,
                // meanline, 0)) Y component, 0 in local meanlineEnd.
                Vector3 meanlineEnd = new Vector3(topRight.x, m_Transform.TransformPoint(new Vector3(0, meanline, 0)).y, 0);

                // If characterInfo.isVisible, run the following block.
                if (characterInfo.isVisible)
                {
                    // Cap line
                    // Set Handles.color to Color.cyan.
                    Handles.color = Color.cyan;
                    // Call Handles.DrawDottedLine using capHeightStart, capHeightEnd, dottedLineSize.
                    Handles.DrawDottedLine(capHeightStart, capHeightEnd, dottedLineSize);

                    // Mean line
                    // Set Handles.color to Color.cyan.
                    Handles.color = Color.cyan;
                    // Call Handles.DrawDottedLine using meanlineStart, meanlineEnd, dottedLineSize.
                    Handles.DrawDottedLine(meanlineStart, meanlineEnd, dottedLineSize);
                }

                //Draw Base line
                // Store a Vector3 built from origin, baseline, 0 converted from m_Transform local coordinates into
                // world coordinates in local baselineStart.
                Vector3 baselineStart = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                // Store a Vector3 built from advance, baseline, 0 converted from m_Transform local coordinates
                // into world coordinates in local baselineEnd.
                Vector3 baselineEnd = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));

                // Set Handles.color to Color.cyan.
                Handles.color = Color.cyan;
                // Call Handles.DrawDottedLine using baselineStart, baselineEnd, dottedLineSize.
                Handles.DrawDottedLine(baselineStart, baselineEnd, dottedLineSize);

                //Draw Descent line
                // Store a Vector3 built from origin, descentline, 0 converted from m_Transform local coordinates
                // into world coordinates in local descentlineStart.
                Vector3 descentlineStart = m_Transform.TransformPoint(new Vector3(origin, descentline, 0));
                // Store a Vector3 built from advance, descentline, 0 converted from m_Transform local coordinates
                // into world coordinates in local descentlineEnd.
                Vector3 descentlineEnd = m_Transform.TransformPoint(new Vector3(advance, descentline, 0));

                // Set Handles.color to Color.cyan.
                Handles.color = Color.cyan;
                // Call Handles.DrawDottedLine using descentlineStart, descentlineEnd, dottedLineSize.
                Handles.DrawDottedLine(descentlineStart, descentlineEnd, dottedLineSize);

                // Draw Origin
                // Store a Vector3 built from origin, baseline, 0 converted from m_Transform local coordinates into
                // world coordinates in local originPosition.
                Vector3 originPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                // Call TMP_TextInfoDebugTool.DrawCrosshair with originPosition, 0.05 divided by m_ScaleMultiplier,
                // Color.cyan; draw a crosshair guide at the supplied positions in the Editor.
                DrawCrosshair(originPosition, 0.05f / m_ScaleMultiplier, Color.cyan);

                // Draw Horizontal Advance
                // Store a Vector3 built from advance, baseline, 0 converted from m_Transform local coordinates
                // into world coordinates in local advancePosition.
                Vector3 advancePosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));
                // Call TMP_TextInfoDebugTool.DrawSquare with advancePosition, 0.025 divided by m_ScaleMultiplier,
                // Color.yellow; draw a square guide at the supplied positions in the Editor.
                DrawSquare(advancePosition, 0.025f / m_ScaleMultiplier, Color.yellow);
                // Call TMP_TextInfoDebugTool.DrawCrosshair with advancePosition, 0.0125 divided by
                // m_ScaleMultiplier, Color.yellow; draw a crosshair guide at the supplied positions in the Editor.
                DrawCrosshair(advancePosition, 0.0125f / m_ScaleMultiplier, Color.yellow);

                // Draw text labels for metrics
               // If m_HandleSize is below 0.5, run the following block.
               if (m_HandleSize < 0.5f)
               {
                   // Store a new GUIStyle initialized with the result of GUI.skin.GetStyle using "Label" in local
                   // style.
                   GUIStyle style = new GUIStyle(GUI.skin.GetStyle("Label"));
                   // Set style.normal.textColor to a Color built from 0.6, 0.6, 0.6, 1.0.
                   style.normal.textColor = new Color(0.6f, 0.6f, 0.6f, 1.0f);
                   // Set style.fontSize to 12.
                   style.fontSize = 12;
                   // Set style.fixedWidth to 200.
                   style.fixedWidth = 200;
                   // Set style.fixedHeight to 20.
                   style.fixedHeight = 20;

                   // Declare local labelPosition to hold Vector3 data supplied later in this method.
                   Vector3 labelPosition;
                   // Store (origin plus advance) divided by 2 in local center.
                   float center = (origin + advance) / 2;

                   //float baselineMetrics = 0;
                   //float ascentlineMetrics = ascentline - baseline;
                   //float caplineMetrics = capline - baseline;
                   //float meanlineMetrics = meanline - baseline;
                   //float descentlineMetrics = descentline - baseline;

                   // Ascent Line
                   // Store a Vector3 built from center, ascentline, 0 converted from m_Transform local coordinates
                   // into world coordinates in labelPosition.
                   labelPosition = m_Transform.TransformPoint(new Vector3(center, ascentline, 0));
                   // Set style.alignment to TextAnchor.UpperCenter.
                   style.alignment = TextAnchor.UpperCenter;
                   // Call Handles.Label using labelPosition, "Ascent Line", style.
                   Handles.Label(labelPosition, "Ascent Line", style);
                   //Handles.Label(labelPosition, "Ascent Line (" + ascentlineMetrics.ToString("f3") + ")" , style);

                   // Base Line
                   // Store a Vector3 built from center, baseline, 0 converted from m_Transform local coordinates
                   // into world coordinates in labelPosition.
                   labelPosition = m_Transform.TransformPoint(new Vector3(center, baseline, 0));
                   // Call Handles.Label using labelPosition, "Base Line", style.
                   Handles.Label(labelPosition, "Base Line", style);
                   //Handles.Label(labelPosition, "Base Line (" + baselineMetrics.ToString("f3") + ")" , style);

                   // Descent line
                   // Store a Vector3 built from center, descentline, 0 converted from m_Transform local
                   // coordinates into world coordinates in labelPosition.
                   labelPosition = m_Transform.TransformPoint(new Vector3(center, descentline, 0));
                   // Call Handles.Label using labelPosition, "Descent Line", style.
                   Handles.Label(labelPosition, "Descent Line", style);
                   //Handles.Label(labelPosition, "Descent Line (" + descentlineMetrics.ToString("f3") + ")" , style);

                   // If characterInfo.isVisible, run the following block.
                   if (characterInfo.isVisible)
                   {
                       // Cap Line
                       // Store a Vector3 built from center, capline, 0 converted from m_Transform local
                       // coordinates into world coordinates in labelPosition.
                       labelPosition = m_Transform.TransformPoint(new Vector3(center, capline, 0));
                       // Set style.alignment to TextAnchor.UpperCenter.
                       style.alignment = TextAnchor.UpperCenter;
                       // Call Handles.Label using labelPosition, "Cap Line", style.
                       Handles.Label(labelPosition, "Cap Line", style);
                       //Handles.Label(labelPosition, "Cap Line (" + caplineMetrics.ToString("f3") + ")" , style);

                       // Mean Line
                       // Store a Vector3 built from center, meanline, 0 converted from m_Transform local
                       // coordinates into world coordinates in labelPosition.
                       labelPosition = m_Transform.TransformPoint(new Vector3(center, meanline, 0));
                       // Set style.alignment to TextAnchor.UpperCenter.
                       style.alignment = TextAnchor.UpperCenter;
                       // Call Handles.Label using labelPosition, "Mean Line", style.
                       Handles.Label(labelPosition, "Mean Line", style);
                       //Handles.Label(labelPosition, "Mean Line (" + ascentlineMetrics.ToString("f3") + ")" , style);

                       // Origin
                       // Store a Vector3 built from origin, baseline, 0 converted from m_Transform local
                       // coordinates into world coordinates in labelPosition.
                       labelPosition = m_Transform.TransformPoint(new Vector3(origin, baseline, 0));
                       // Set style.alignment to TextAnchor.UpperRight.
                       style.alignment = TextAnchor.UpperRight;
                       // Call Handles.Label using labelPosition, "Origin ", style.
                       Handles.Label(labelPosition, "Origin ", style);

                       // Advance
                       // Store a Vector3 built from advance, baseline, 0 converted from m_Transform local
                       // coordinates into world coordinates in labelPosition.
                       labelPosition = m_Transform.TransformPoint(new Vector3(advance, baseline, 0));
                       // Set style.alignment to TextAnchor.UpperLeft.
                       style.alignment = TextAnchor.UpperLeft;
                       // Call Handles.Label using labelPosition, " Advance", style.
                       Handles.Label(labelPosition, "  Advance", style);
                   }
               }
            }
        }


        // Draw Editor guides for word text bounds using generated geometry.
        /// <summary>
        /// Method to draw rectangles around each word of the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawWordBounds()
        {
            // Repeat this block while i is below m_TextInfo.wordCount; start from int i = 0.
            for (int i = 0; i < m_TextInfo.wordCount; i++)
            {
                // Store m_TextInfo.wordInfo[i] (entry read from m_TextInfo.wordInfo) in local wInfo.
                TMP_WordInfo wInfo = m_TextInfo.wordInfo[i];

                // Initialize isBeginRegion to false.
                bool isBeginRegion = false;

                // Store the zero vector (no movement or offset) in local bottomLeft.
                Vector3 bottomLeft = Vector3.zero;
                // Store the zero vector (no movement or offset) in local topLeft.
                Vector3 topLeft = Vector3.zero;
                // Store the zero vector (no movement or offset) in local bottomRight.
                Vector3 bottomRight = Vector3.zero;
                // Store the zero vector (no movement or offset) in local topRight.
                Vector3 topRight = Vector3.zero;

                // Store negative Mathf.Infinity in local maxAscender.
                float maxAscender = -Mathf.Infinity;
                // Store Mathf.Infinity in local minDescender.
                float minDescender = Mathf.Infinity;

                // Store Color.green in local wordColor.
                Color wordColor = Color.green;

                // Iterate through each character of the word
                // Repeat this block while j is below wInfo.characterCount; start from int j = 0.
                for (int j = 0; j < wInfo.characterCount; j++)
                {
                    // Store wInfo.firstCharacterIndex plus j in local characterIndex.
                    int characterIndex = wInfo.firstCharacterIndex + j;
                    // Store m_TextInfo.characterInfo[characterIndex] (entry read from m_TextInfo.characterInfo) in
                    // local currentCharInfo.
                    TMP_CharacterInfo currentCharInfo = m_TextInfo.characterInfo[characterIndex];
                    // Store currentCharInfo.lineNumber in local currentLine.
                    int currentLine = currentCharInfo.lineNumber;

                    // Store false when characterIndex > m_TextComponent.maxVisibleCharacters or
                    // currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines or
                    // (m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 !=
                    // m_TextComponent.pageToDisplay); otherwise true in local isCharacterVisible.
                    bool isCharacterVisible = characterIndex > m_TextComponent.maxVisibleCharacters ||
                                              currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines ||
                                             (m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 != m_TextComponent.pageToDisplay) ? false : true;

                    // Track Max Ascender and Min Descender
                    // Store the largest of maxAscender, currentCharInfo.ascender in maxAscender.
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    // Store the smallest of minDescender, currentCharInfo.descender in minDescender.
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    // If isBeginRegion equals false and isCharacterVisible, run the following block.
                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        // Set isBeginRegion to true.
                        isBeginRegion = true;

                        // Set bottomLeft to a Vector3 built from currentCharInfo.bottomLeft X component,
                        // currentCharInfo.descender, 0.
                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        // Set topLeft to a Vector3 built from currentCharInfo.bottomLeft X component,
                        // currentCharInfo.ascender, 0.
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Word is one character
                        // If wInfo.characterCount equals 1, run the following block.
                        if (wInfo.characterCount == 1)
                        {
                            // Set isBeginRegion to false.
                            isBeginRegion = false;

                            // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform
                            // local coordinates into world coordinates in topLeft.
                            topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                            // local coordinates into world coordinates in bottomLeft.
                            bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted
                            // from m_Transform local coordinates into world coordinates in bottomRight.
                            bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                            // m_Transform local coordinates into world coordinates in topRight.
                            topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                            // Draw Region
                            // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                            // bottomRight, wordColor; draw Rectangles.
                            DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Word
                    // If isBeginRegion and j equals wInfo.characterCount minus 1, run the following block.
                    if (isBeginRegion && j == wInfo.characterCount - 1)
                    {
                        // Set isBeginRegion to false.
                        isBeginRegion = false;

                        // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform local
                        // coordinates into world coordinates in topLeft.
                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                        // local coordinates into world coordinates in bottomLeft.
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted from
                        // m_Transform local coordinates into world coordinates in bottomRight.
                        bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                        // m_Transform local coordinates into world coordinates in topRight.
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                        // bottomRight, wordColor; draw Rectangles.
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Word is split on more than one line.
                    else if (isBeginRegion && currentLine != m_TextInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        // Set isBeginRegion to false.
                        isBeginRegion = false;

                        // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform local
                        // coordinates into world coordinates in topLeft.
                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                        // local coordinates into world coordinates in bottomLeft.
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted from
                        // m_Transform local coordinates into world coordinates in bottomRight.
                        bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                        // m_Transform local coordinates into world coordinates in topRight.
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                        // bottomRight, wordColor; draw Rectangles.
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, wordColor);
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        // Set maxAscender to negative Mathf.Infinity.
                        maxAscender = -Mathf.Infinity;
                        // Set minDescender to Mathf.Infinity.
                        minDescender = Mathf.Infinity;

                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }


        }


        // Draw Editor guides for link text bounds using generated geometry.
        /// <summary>
        /// Draw rectangle around each of the links contained in the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawLinkBounds()
        {
            // Store m_TextComponent generated TMP character/line/mesh information in local textInfo.
            TMP_TextInfo textInfo = m_TextComponent.textInfo;

            // Repeat this block while i is below textInfo.linkCount; start from int i = 0.
            for (int i = 0; i < textInfo.linkCount; i++)
            {
                // Store textInfo.linkInfo[i] (entry read from textInfo.linkInfo) in local linkInfo.
                TMP_LinkInfo linkInfo = textInfo.linkInfo[i];

                // Initialize isBeginRegion to false.
                bool isBeginRegion = false;

                // Store the zero vector (no movement or offset) in local bottomLeft.
                Vector3 bottomLeft = Vector3.zero;
                // Store the zero vector (no movement or offset) in local topLeft.
                Vector3 topLeft = Vector3.zero;
                // Store the zero vector (no movement or offset) in local bottomRight.
                Vector3 bottomRight = Vector3.zero;
                // Store the zero vector (no movement or offset) in local topRight.
                Vector3 topRight = Vector3.zero;

                // Store negative Mathf.Infinity in local maxAscender.
                float maxAscender = -Mathf.Infinity;
                // Store Mathf.Infinity in local minDescender.
                float minDescender = Mathf.Infinity;

                // Store Color.cyan in local linkColor.
                Color32 linkColor = Color.cyan;

                // Iterate through each character of the link text
                // Repeat this block while j is below linkInfo.linkTextLength; start from int j = 0.
                for (int j = 0; j < linkInfo.linkTextLength; j++)
                {
                    // Store linkInfo.linkTextfirstCharacterIndex plus j in local characterIndex.
                    int characterIndex = linkInfo.linkTextfirstCharacterIndex + j;
                    // Store textInfo.characterInfo[characterIndex] (entry read from textInfo.characterInfo) in
                    // local currentCharInfo.
                    TMP_CharacterInfo currentCharInfo = textInfo.characterInfo[characterIndex];
                    // Store currentCharInfo.lineNumber in local currentLine.
                    int currentLine = currentCharInfo.lineNumber;

                    // Store false when characterIndex > m_TextComponent.maxVisibleCharacters or
                    // currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines or
                    // (m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 !=
                    // m_TextComponent.pageToDisplay); otherwise true in local isCharacterVisible.
                    bool isCharacterVisible = characterIndex > m_TextComponent.maxVisibleCharacters ||
                                              currentCharInfo.lineNumber > m_TextComponent.maxVisibleLines ||
                                             (m_TextComponent.overflowMode == TextOverflowModes.Page && currentCharInfo.pageNumber + 1 != m_TextComponent.pageToDisplay) ? false : true;

                    // Track Max Ascender and Min Descender
                    // Store the largest of maxAscender, currentCharInfo.ascender in maxAscender.
                    maxAscender = Mathf.Max(maxAscender, currentCharInfo.ascender);
                    // Store the smallest of minDescender, currentCharInfo.descender in minDescender.
                    minDescender = Mathf.Min(minDescender, currentCharInfo.descender);

                    // If isBeginRegion equals false and isCharacterVisible, run the following block.
                    if (isBeginRegion == false && isCharacterVisible)
                    {
                        // Set isBeginRegion to true.
                        isBeginRegion = true;

                        // Set bottomLeft to a Vector3 built from currentCharInfo.bottomLeft X component,
                        // currentCharInfo.descender, 0.
                        bottomLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.descender, 0);
                        // Set topLeft to a Vector3 built from currentCharInfo.bottomLeft X component,
                        // currentCharInfo.ascender, 0.
                        topLeft = new Vector3(currentCharInfo.bottomLeft.x, currentCharInfo.ascender, 0);

                        //Debug.Log("Start Word Region at [" + currentCharInfo.character + "]");

                        // If Link is one character
                        // If linkInfo.linkTextLength equals 1, run the following block.
                        if (linkInfo.linkTextLength == 1)
                        {
                            // Set isBeginRegion to false.
                            isBeginRegion = false;

                            // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform
                            // local coordinates into world coordinates in topLeft.
                            topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                            // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                            // local coordinates into world coordinates in bottomLeft.
                            bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                            // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted
                            // from m_Transform local coordinates into world coordinates in bottomRight.
                            bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                            // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                            // m_Transform local coordinates into world coordinates in topRight.
                            topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                            // Draw Region
                            // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                            // bottomRight, linkColor; draw Rectangles.
                            DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                            //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                        }
                    }

                    // Last Character of Link
                    // If isBeginRegion and j equals linkInfo.linkTextLength minus 1, run the following block.
                    if (isBeginRegion && j == linkInfo.linkTextLength - 1)
                    {
                        // Set isBeginRegion to false.
                        isBeginRegion = false;

                        // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform local
                        // coordinates into world coordinates in topLeft.
                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                        // local coordinates into world coordinates in bottomLeft.
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted from
                        // m_Transform local coordinates into world coordinates in bottomRight.
                        bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                        // m_Transform local coordinates into world coordinates in topRight.
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                        // bottomRight, linkColor; draw Rectangles.
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                    // If Link is split on more than one line.
                    else if (isBeginRegion && currentLine != textInfo.characterInfo[characterIndex + 1].lineNumber)
                    {
                        // Set isBeginRegion to false.
                        isBeginRegion = false;

                        // Store a Vector3 built from topLeft.x, maxAscender, 0 converted from m_Transform local
                        // coordinates into world coordinates in topLeft.
                        topLeft = m_Transform.TransformPoint(new Vector3(topLeft.x, maxAscender, 0));
                        // Store a Vector3 built from bottomLeft.x, minDescender, 0 converted from m_Transform
                        // local coordinates into world coordinates in bottomLeft.
                        bottomLeft = m_Transform.TransformPoint(new Vector3(bottomLeft.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, minDescender, 0 converted from
                        // m_Transform local coordinates into world coordinates in bottomRight.
                        bottomRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, minDescender, 0));
                        // Store a Vector3 built from currentCharInfo.topRight.x, maxAscender, 0 converted from
                        // m_Transform local coordinates into world coordinates in topRight.
                        topRight = m_Transform.TransformPoint(new Vector3(currentCharInfo.topRight.x, maxAscender, 0));

                        // Draw Region
                        // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topLeft, topRight,
                        // bottomRight, linkColor; draw Rectangles.
                        DrawRectangle(bottomLeft, topLeft, topRight, bottomRight, linkColor);

                        // Set maxAscender to negative Mathf.Infinity.
                        maxAscender = -Mathf.Infinity;
                        // Set minDescender to Mathf.Infinity.
                        minDescender = Mathf.Infinity;
                        //Debug.Log("End Word Region at [" + currentCharInfo.character + "]");
                    }
                }

                //Debug.Log(wInfo.GetWord(m_TextMeshPro.textInfo.characterInfo));
            }
        }


        // Draw Editor guides for line text bounds using generated geometry.
        /// <summary>
        /// Draw Rectangles around each lines of the text.
        /// </summary>
        /// <param name="text"></param>
        void DrawLineBounds()
        {
            // Store m_TextInfo.lineCount in local lineCount.
            int lineCount = m_TextInfo.lineCount;

            // Repeat this block while i is below lineCount; start from int i = 0.
            for (int i = 0; i < lineCount; i++)
            {
                // Store m_TextInfo.lineInfo[i] (entry read from m_TextInfo.lineInfo) in local lineInfo.
                TMP_LineInfo lineInfo = m_TextInfo.lineInfo[i];
                // Store m_TextInfo.characterInfo[lineInfo.firstCharacterIndex] (entry read from
                // m_TextInfo.characterInfo) in local firstCharacterInfo.
                TMP_CharacterInfo firstCharacterInfo = m_TextInfo.characterInfo[lineInfo.firstCharacterIndex];
                // Store m_TextInfo.characterInfo[lineInfo.lastCharacterIndex] (entry read from
                // m_TextInfo.characterInfo) in local lastCharacterInfo.
                TMP_CharacterInfo lastCharacterInfo = m_TextInfo.characterInfo[lineInfo.lastCharacterIndex];

                // Store false when (lineInfo.characterCount == 1 && (firstCharacterInfo.character == 10 ||
                // firstCharacterInfo.character == 11 || firstCharacterInfo.character == 0x2028 ||
                // firstCharacterInfo.character == 0x2029)) or i > m_TextComponent.maxVisibleLines or
                // (m_TextComponent.overflowMode == TextOverflowModes.Page && firstCharacterInfo.pageNumber + 1 !=
                // m_TextComponent.pageToDisplay); otherwise true in local isLineVisible.
                bool isLineVisible = (lineInfo.characterCount == 1 && (firstCharacterInfo.character == 10 || firstCharacterInfo.character == 11 || firstCharacterInfo.character == 0x2028 || firstCharacterInfo.character == 0x2029)) ||
                                      i > m_TextComponent.maxVisibleLines ||
                                     (m_TextComponent.overflowMode == TextOverflowModes.Page && firstCharacterInfo.pageNumber + 1 != m_TextComponent.pageToDisplay) ? false : true;

                // If not (isLineVisible), skip this entry and continue with the next loop iteration.
                if (!isLineVisible) continue;

                // Store firstCharacterInfo.bottomLeft X component in local lineBottomLeft.
                float lineBottomLeft = firstCharacterInfo.bottomLeft.x;
                // Store lastCharacterInfo.topRight X component in local lineTopRight.
                float lineTopRight = lastCharacterInfo.topRight.x;

                // Store lineInfo.ascender in local ascentline.
                float ascentline = lineInfo.ascender;
                // Store lineInfo.baseline in local baseline.
                float baseline = lineInfo.baseline;
                // Store lineInfo.descender in local descentline.
                float descentline = lineInfo.descender;

                // Initialize dottedLineSize to 12.
                float dottedLineSize = 12;

                // Draw line extents
                // Call TMP_TextInfoDebugTool.DrawDottedRectangle with lineInfo.lineExtents.min converted from
                // m_Transform local coordinates into world coordinates, lineInfo.lineExtents.max converted from
                // m_Transform local coordinates into world coordinates, Color.green, 4; draw Rectangles.
                DrawDottedRectangle(m_Transform.TransformPoint(lineInfo.lineExtents.min), m_Transform.TransformPoint(lineInfo.lineExtents.max), Color.green, 4);

                // Draw Ascent line
                // Store a Vector3 built from lineBottomLeft, ascentline, 0 converted from m_Transform local
                // coordinates into world coordinates in local ascentlineStart.
                Vector3 ascentlineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0));
                // Store a Vector3 built from lineTopRight, ascentline, 0 converted from m_Transform local
                // coordinates into world coordinates in local ascentlineEnd.
                Vector3 ascentlineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, ascentline, 0));

                // Set Handles.color to Color.yellow.
                Handles.color = Color.yellow;
                // Call Handles.DrawDottedLine using ascentlineStart, ascentlineEnd, dottedLineSize.
                Handles.DrawDottedLine(ascentlineStart, ascentlineEnd, dottedLineSize);

                // Draw Base line
                // Store a Vector3 built from lineBottomLeft, baseline, 0 converted from m_Transform local
                // coordinates into world coordinates in local baseLineStart.
                Vector3 baseLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0));
                // Store a Vector3 built from lineTopRight, baseline, 0 converted from m_Transform local
                // coordinates into world coordinates in local baseLineEnd.
                Vector3 baseLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, baseline, 0));

                // Set Handles.color to Color.yellow.
                Handles.color = Color.yellow;
                // Call Handles.DrawDottedLine using baseLineStart, baseLineEnd, dottedLineSize.
                Handles.DrawDottedLine(baseLineStart, baseLineEnd, dottedLineSize);

                // Draw Descent line
                // Store a Vector3 built from lineBottomLeft, descentline, 0 converted from m_Transform local
                // coordinates into world coordinates in local descentLineStart.
                Vector3 descentLineStart = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0));
                // Store a Vector3 built from lineTopRight, descentline, 0 converted from m_Transform local
                // coordinates into world coordinates in local descentLineEnd.
                Vector3 descentLineEnd = m_Transform.TransformPoint(new Vector3(lineTopRight, descentline, 0));

                // Set Handles.color to Color.yellow.
                Handles.color = Color.yellow;
                // Call Handles.DrawDottedLine using descentLineStart, descentLineEnd, dottedLineSize.
                Handles.DrawDottedLine(descentLineStart, descentLineEnd, dottedLineSize);

                // Draw text labels for metrics
                // If m_HandleSize is below 1.0, run the following block.
                if (m_HandleSize < 1.0f)
                {
                    // Store a new GUIStyle instance in local style.
                    GUIStyle style = new GUIStyle();
                    // Set style.normal.textColor to a Color built from 0.8, 0.8, 0.8, 1.0.
                    style.normal.textColor = new Color(0.8f, 0.8f, 0.8f, 1.0f);
                    // Set style.fontSize to 12.
                    style.fontSize = 12;
                    // Set style.fixedWidth to 200.
                    style.fixedWidth = 200;
                    // Set style.fixedHeight to 20.
                    style.fixedHeight = 20;
                    // Declare local labelPosition to hold Vector3 data supplied later in this method.
                    Vector3 labelPosition;

                    // Ascent Line
                    // Store a Vector3 built from lineBottomLeft, ascentline, 0 converted from m_Transform local
                    // coordinates into world coordinates in labelPosition.
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, ascentline, 0));
                    // Set style.padding to a new RectOffset initialized with 0, 10, 0, 5.
                    style.padding = new RectOffset(0, 10, 0, 5);
                    // Set style.alignment to TextAnchor.MiddleRight.
                    style.alignment = TextAnchor.MiddleRight;
                    // Call Handles.Label using labelPosition, "Ascent Line", style.
                    Handles.Label(labelPosition, "Ascent Line", style);

                    // Base Line
                    // Store a Vector3 built from lineBottomLeft, baseline, 0 converted from m_Transform local
                    // coordinates into world coordinates in labelPosition.
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, baseline, 0));
                    // Call Handles.Label using labelPosition, "Base Line", style.
                    Handles.Label(labelPosition, "Base Line", style);

                    // Descent line
                    // Store a Vector3 built from lineBottomLeft, descentline, 0 converted from m_Transform local
                    // coordinates into world coordinates in labelPosition.
                    labelPosition = m_Transform.TransformPoint(new Vector3(lineBottomLeft, descentline, 0));
                    // Call Handles.Label using labelPosition, "Descent Line", style.
                    Handles.Label(labelPosition, "Descent Line", style);
                }
            }
        }


        // Draw Editor guides for text bounds using generated geometry.
        /// <summary>
        /// Draw Rectangle around the bounds of the text object.
        /// </summary>
        void DrawBounds()
        {
            // Store m_TextComponent world bounding box in local meshBounds.
            Bounds meshBounds = m_TextComponent.bounds;

            // Get Bottom Left and Top Right position of each word
            // Store m_TextComponent.transform.position + meshBounds.min (calculated from the values shown) in
            // local bottomLeft.
            Vector3 bottomLeft = m_TextComponent.transform.position + meshBounds.min;
            // Store m_TextComponent.transform.position + meshBounds.max (calculated from the values shown) in
            // local topRight.
            Vector3 topRight = m_TextComponent.transform.position + meshBounds.max;

            // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topRight, a Color built from 1, 0.5, 0;
            // draw Rectangles.
            DrawRectangle(bottomLeft, topRight, new Color(1, 0.5f, 0));
        }


        // Draw Editor guides for text text bounds using generated geometry.
        void DrawTextBounds()
        {
            // Store m_TextComponent.textBounds in local textBounds.
            Bounds textBounds = m_TextComponent.textBounds;

            // Store m_TextComponent.transform.position + (textBounds.center - textBounds.extents) (calculated from
            // the values shown) in local bottomLeft.
            Vector3 bottomLeft = m_TextComponent.transform.position + (textBounds.center - textBounds.extents);
            // Store m_TextComponent.transform.position + (textBounds.center + textBounds.extents) (calculated from
            // the values shown) in local topRight.
            Vector3 topRight = m_TextComponent.transform.position + (textBounds.center + textBounds.extents);

            // Call TMP_TextInfoDebugTool.DrawRectangle with bottomLeft, topRight, a Color built from 0, 0.5, 0.5;
            // draw Rectangles.
            DrawRectangle(bottomLeft, topRight, new Color(0f, 0.5f, 0.5f));
        }


        // Draw a rectangle guide at the supplied positions in the Editor.
        // Draw Rectangles
        // BL is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // TR is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        void DrawRectangle(Vector3 BL, Vector3 TR, Color color)
        {
            // Copy color (argument from the caller) into Gizmos.color.
            Gizmos.color = color;

            // Call Gizmos.DrawLine using a Vector3 built from BL X component, BL Y component, 0, a Vector3 built
            // from BL X component, TR Y component, 0.
            Gizmos.DrawLine(new Vector3(BL.x, BL.y, 0), new Vector3(BL.x, TR.y, 0));
            // Call Gizmos.DrawLine using a Vector3 built from BL X component, TR Y component, 0, a Vector3 built
            // from TR X component, TR Y component, 0.
            Gizmos.DrawLine(new Vector3(BL.x, TR.y, 0), new Vector3(TR.x, TR.y, 0));
            // Call Gizmos.DrawLine using a Vector3 built from TR X component, TR Y component, 0, a Vector3 built
            // from TR X component, BL Y component, 0.
            Gizmos.DrawLine(new Vector3(TR.x, TR.y, 0), new Vector3(TR.x, BL.y, 0));
            // Call Gizmos.DrawLine using a Vector3 built from TR X component, BL Y component, 0, a Vector3 built
            // from BL X component, BL Y component, 0.
            Gizmos.DrawLine(new Vector3(TR.x, BL.y, 0), new Vector3(BL.x, BL.y, 0));
        }

        // Draw a dotted rectangle guide at the supplied positions in the Editor.
        // bottomLeft is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // topRight is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // size is a float argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        void DrawDottedRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5.0f)
        {
            // Copy color (argument from the caller) into Handles.color.
            Handles.color = color;
            // Call Handles.DrawDottedLine using bottomLeft, a Vector3 built from bottomLeft X component, topRight
            // Y component, bottomLeft Z component, size.
            Handles.DrawDottedLine(bottomLeft, new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), size);
            // Call Handles.DrawDottedLine using a Vector3 built from bottomLeft X component, topRight Y component,
            // bottomLeft Z component, topRight, size.
            Handles.DrawDottedLine(new Vector3(bottomLeft.x, topRight.y, bottomLeft.z), topRight, size);
            // Call Handles.DrawDottedLine using topRight, a Vector3 built from topRight X component, bottomLeft Y
            // component, bottomLeft Z component, size.
            Handles.DrawDottedLine(topRight, new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), size);
            // Call Handles.DrawDottedLine using a Vector3 built from topRight X component, bottomLeft Y component,
            // bottomLeft Z component, bottomLeft, size.
            Handles.DrawDottedLine(new Vector3(topRight.x, bottomLeft.y, bottomLeft.z), bottomLeft, size);
        }

        // Draw a solid rectangle guide at the supplied positions in the Editor.
        // bottomLeft is a Vector3 argument supplied by the code, event or Inspector callback invoking
        // DrawSolidRectangle.
        // topRight is a Vector3 argument supplied by the code, event or Inspector callback invoking
        // DrawSolidRectangle.
        // color is a Color argument supplied by the code, event or Inspector callback invoking DrawSolidRectangle.
        // size is a float argument supplied by the code, event or Inspector callback invoking DrawSolidRectangle.
        void DrawSolidRectangle(Vector3 bottomLeft, Vector3 topRight, Color color, float size = 5.0f)
        {
            // Copy color (argument from the caller) into Handles.color.
            Handles.color = color;
            // Store a new Rect initialized with bottomLeft, topRight minus bottomLeft in local rect.
            Rect rect = new Rect(bottomLeft, topRight - bottomLeft);
            // Call Handles.DrawSolidRectangleWithOutline using rect, color, Color.black.
            Handles.DrawSolidRectangleWithOutline(rect, color, Color.black);
        }

        // Draw a square guide at the supplied positions in the Editor.
        // position is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        // size is a float argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        void DrawSquare(Vector3 position, float size, Color color)
        {
            // Copy color (argument from the caller) into Handles.color.
            Handles.color = color;
            // Store a Vector3 built from position X component minus size, position Y component minus size,
            // position Z component in local bottomLeft.
            Vector3 bottomLeft = new Vector3(position.x - size, position.y - size, position.z);
            // Store a Vector3 built from position X component minus size, position Y component plus size, position
            // Z component in local topLeft.
            Vector3 topLeft = new Vector3(position.x - size, position.y + size, position.z);
            // Store a Vector3 built from position X component plus size, position Y component plus size, position
            // Z component in local topRight.
            Vector3 topRight = new Vector3(position.x + size, position.y + size, position.z);
            // Store a Vector3 built from position X component plus size, position Y component minus size, position
            // Z component in local bottomRight.
            Vector3 bottomRight = new Vector3(position.x + size, position.y - size, position.z);

            // Call Handles.DrawLine using bottomLeft, topLeft.
            Handles.DrawLine(bottomLeft, topLeft);
            // Call Handles.DrawLine using topLeft, topRight.
            Handles.DrawLine(topLeft, topRight);
            // Call Handles.DrawLine using topRight, bottomRight.
            Handles.DrawLine(topRight, bottomRight);
            // Call Handles.DrawLine using bottomRight, bottomLeft.
            Handles.DrawLine(bottomRight, bottomLeft);
        }

        // Draw a crosshair guide at the supplied positions in the Editor.
        // position is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        // size is a float argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds.
        void DrawCrosshair(Vector3 position, float size, Color color)
        {
            // Copy color (argument from the caller) into Handles.color.
            Handles.color = color;

            // Call Handles.DrawLine using a Vector3 built from position.x minus size, position Y component,
            // position Z component, a Vector3 built from position.x plus size, position Y component, position Z
            // component.
            Handles.DrawLine(new Vector3(position.x - size, position.y, position.z), new Vector3(position.x + size, position.y, position.z));
            // Call Handles.DrawLine using a Vector3 built from position X component, position.y minus size,
            // position Z component, a Vector3 built from position X component, position.y plus size, position Z
            // component.
            Handles.DrawLine(new Vector3(position.x, position.y - size, position.z), new Vector3(position.x, position.y + size, position.z));
        }


        // Draw a rectangle guide at the supplied positions in the Editor.
        // Draw Rectangles
        // bl is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // tl is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // tr is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // br is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawBounds,
        // TMP_TextInfoDebugTool.DrawLinkBounds, TMP_TextInfoDebugTool.DrawTextBounds or another caller.
        void DrawRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            // Copy color (argument from the caller) into Gizmos.color.
            Gizmos.color = color;

            // Call Gizmos.DrawLine using bl, tl.
            Gizmos.DrawLine(bl, tl);
            // Call Gizmos.DrawLine using tl, tr.
            Gizmos.DrawLine(tl, tr);
            // Call Gizmos.DrawLine using tr, br.
            Gizmos.DrawLine(tr, br);
            // Call Gizmos.DrawLine using br, bl.
            Gizmos.DrawLine(br, bl);
        }


        // Draw a dotted rectangle guide at the supplied positions in the Editor.
        // Draw Rectangles
        // bl is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // tl is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // tr is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // br is a Vector3 argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        // color is a Color argument supplied by TMP_TextInfoDebugTool.DrawCharactersBounds,
        // TMP_TextInfoDebugTool.DrawLineBounds.
        void DrawDottedRectangle(Vector3 bl, Vector3 tl, Vector3 tr, Vector3 br, Color color)
        {
            // Store Camera.current in local cam.
            var cam = Camera.current;
            // Store (cam.WorldToScreenPoint(br).x - cam.WorldToScreenPoint(bl).x) / 75f (calculated from the
            // values shown) in local dotSpacing.
            float dotSpacing = (cam.WorldToScreenPoint(br).x - cam.WorldToScreenPoint(bl).x) / 75f;
            // Copy color (argument from the caller) into UnityEditor.Handles.color.
            UnityEditor.Handles.color = color;

            // Call UnityEditor.Handles.DrawDottedLine using bl, tl, dotSpacing.
            UnityEditor.Handles.DrawDottedLine(bl, tl, dotSpacing);
            // Call UnityEditor.Handles.DrawDottedLine using tl, tr, dotSpacing.
            UnityEditor.Handles.DrawDottedLine(tl, tr, dotSpacing);
            // Call UnityEditor.Handles.DrawDottedLine using tr, br, dotSpacing.
            UnityEditor.Handles.DrawDottedLine(tr, br, dotSpacing);
            // Call UnityEditor.Handles.DrawDottedLine using br, bl, dotSpacing.
            UnityEditor.Handles.DrawDottedLine(br, bl, dotSpacing);
        }
        #endif
    }
}

