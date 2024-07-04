using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

#if MA_EXISTS
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {
        private bool showSkinnedMeshOperations = false;
        private bool showBlendShapes = false;

        private string blendShapeSearch = "";
        private Vector2 blendShapeScrollPosition = Vector2.zero;

        private class BlendShapeInfo
        {
            public SkinnedMeshRenderer Renderer;
            public int Index;
            public string Name;
        }
        private List<BlendShapeInfo> blendShapeInfos = new List<BlendShapeInfo>();

        private void DrawSkinnedMeshOperationsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIContent skinnedMeshOperationsContent = new GUIContent("Skinned Mesh Operations", EditorGUIUtility.FindTexture("ArrowRight"));
            Rect skinnedMeshOperationsToggleRect = GUILayoutUtility.GetRect(skinnedMeshOperationsContent, EditorStyles.foldout);
            if (GUI.Button(skinnedMeshOperationsToggleRect, skinnedMeshOperationsContent, EditorStyles.foldout))
            {
                showSkinnedMeshOperations = !showSkinnedMeshOperations;
            }

            if (showSkinnedMeshOperations)
            {
                GUIContent centerBoundsContent = new GUIContent("Center Bounds", "Set the local bounds of the SkinnedMeshRenderer to center (0, 0, 0) with an extend of (1, 1, 1).");
                if (GUILayout.Button(centerBoundsContent))
                {
                    Undo.RecordObjects(skinnedMeshRenderers, "Center Bounds");
                    foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
                    {
                        renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2);
                    }
                }
                DrawBlendShapesSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBlendShapesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showBlendShapes = EditorGUILayout.Foldout(showBlendShapes, "BlendShapes");

            if (showBlendShapes)
            {
                CollectBlendShapes();

                // Indent search bar
                EditorGUI.indentLevel++;
                blendShapeSearch = EditorGUILayout.TextField("Search", blendShapeSearch);

                // Start ScrollView for blend shape list
                blendShapeScrollPosition = EditorGUILayout.BeginScrollView(blendShapeScrollPosition);

                // Display blendshapes
                string lastObject = null;
                foreach (var blendShapeInfo in blendShapeInfos)
                {
                    if (string.IsNullOrEmpty(blendShapeSearch) || blendShapeInfo.Name.ToLower().Contains(blendShapeSearch.ToLower()))
                    {
                        // Check if we need to show a new object header
                        if (blendShapeInfo.Renderer.gameObject.name != lastObject && selectedObjects.Length > 1)
                        {
                            // Draw object selector for the mesh
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.ObjectField(blendShapeInfo.Renderer.gameObject, typeof(GameObject), true);
                            EditorGUI.EndDisabledGroup();
                            lastObject = blendShapeInfo.Renderer.gameObject.name;
                        }

                        EditorGUILayout.BeginHorizontal();

                        // Display blendshape name label with fixed width
                        float labelWidth = EditorGUIUtility.labelWidth; // Store the original label width
                        EditorGUIUtility.labelWidth = 150f; // Set to a fixed width
                        Rect nameLabelRect = EditorGUILayout.GetControlRect(GUILayout.Width(150f));
                        EditorGUI.LabelField(nameLabelRect, new GUIContent(blendShapeInfo.Name));
                        EditorGUIUtility.labelWidth = labelWidth; // Restore the original label width

                        // Handle right-click context menu for name label
                        HandleContextMenu(blendShapeInfo, nameLabelRect);

                        // Display blendshape slider
                        float blendShapeValue = blendShapeInfo.Renderer.GetBlendShapeWeight(blendShapeInfo.Index);
                        float newBlendShapeValue = EditorGUILayout.Slider(blendShapeValue, 0, 100);

                        // Handle right-click context menu for slider
                        Rect sliderRect = GUILayoutUtility.GetLastRect();
                        HandleContextMenu(blendShapeInfo, sliderRect);

                        if (newBlendShapeValue != blendShapeValue)
                        {
                            Undo.RecordObject(blendShapeInfo.Renderer, "Change Blend Shape Weight");
                            blendShapeInfo.Renderer.SetBlendShapeWeight(blendShapeInfo.Index, newBlendShapeValue);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                }

                // End ScrollView for blend shape list
                EditorGUILayout.EndScrollView();

                EditorGUI.indentLevel--;
            }

            EditorGUILayout.EndVertical();
        }

        private void HandleContextMenu(BlendShapeInfo blendShapeInfo, Rect rect)
        {
            Event currentEvent = Event.current;
            if (currentEvent.type == EventType.ContextClick && rect.Contains(currentEvent.mousePosition))
            {
                GenericMenu menu = new GenericMenu();
                // Copy name
                menu.AddItem(new GUIContent("Copy Name"), false, () => CopyBlendShapeNameToClipboard(blendShapeInfo.Name));

                menu.AddSeparator("");

                // Copy and paste value
                menu.AddItem(new GUIContent("Copy Value"), false, () => CopyBlendShapeValueToClipboard(blendShapeInfo.Renderer, blendShapeInfo.Index));
                menu.AddItem(new GUIContent("Paste Value"), false, () => PasteBlendShapeValueFromClipboard(blendShapeInfo.Renderer, blendShapeInfo.Index));

                if (!IsClipboardContainingNumber())
                {
                    menu.AddDisabledItem(new GUIContent("Paste Value"));
                }

                menu.AddSeparator("");

                // Set to 0 and 100
                menu.AddItem(new GUIContent("Set to 0"), false, () => SetBlendShapeToValue(blendShapeInfo.Renderer, blendShapeInfo.Index, 0));
                menu.AddItem(new GUIContent("Set to 100"), false, () => SetBlendShapeToValue(blendShapeInfo.Renderer, blendShapeInfo.Index, 100));

                // Add to MA blend Sync
#if MA_EXISTS
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Add to MA blend Sync"), false, () => AddBlendshapeToMASync(blendShapeInfo.Renderer, blendShapeInfo.Index));
#endif

#if VIXEN_EXISTS
        // Add to Vixen blend Sync
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Add to Vixen blend Sync"), false, () => AddBlendshapeToVixenSync(blendShapeInfo.Renderer, blendShapeInfo.Index));
#endif

                menu.ShowAsContext();
                currentEvent.Use();
            }
        }

        private void CollectBlendShapes()
        {
            blendShapeInfos.Clear();

            foreach (var renderer in skinnedMeshRenderers)
            {
                if (renderer != null && renderer.sharedMesh != null)
                {
                    int blendShapeCount = renderer.sharedMesh.blendShapeCount;
                    for (int i = 0; i < blendShapeCount; i++)
                    {
                        string blendShapeName = renderer.sharedMesh.GetBlendShapeName(i);
                        blendShapeInfos.Add(new BlendShapeInfo
                        {
                            Renderer = renderer,
                            Index = i,
                            Name = blendShapeName
                        });
                    }
                }
            }
        }


        /// <summary>
        /// Checks if the clipboard contains a number.
        /// </summary>
        /// <returns><c>true</c> if the clipboard contains a number; otherwise, <c>false</c>.</returns>
        private bool IsClipboardContainingNumber()
        {
            string clipboardContent = EditorGUIUtility.systemCopyBuffer;
            float result;
            return float.TryParse(clipboardContent, out result);
        }

        /// <summary>
        /// Copies the specified blend shape name to the system clipboard.
        /// </summary>
        /// <param name="blendShapeName">The name of the blend shape to copy.</param>
        private void CopyBlendShapeNameToClipboard(string blendShapeName)
        {
            EditorGUIUtility.systemCopyBuffer = blendShapeName;
            Debug.Log($"BlendShape name '{blendShapeName}' copied to clipboard.");
        }

        /// <summary>
        /// Copies the value of a blend shape from the provided SkinnedMeshRenderer to the system clipboard.
        /// </summary>
        /// <param name="renderer">The SkinnedMeshRenderer containing the blend shape.</param>
        /// <param name="blendShapeIndex">The index of the blend shape.</param>
        private void CopyBlendShapeValueToClipboard(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            if (renderer != null && renderer.sharedMesh != null)
            {
                float blendShapeValue = renderer.GetBlendShapeWeight(blendShapeIndex);
                EditorGUIUtility.systemCopyBuffer = blendShapeValue.ToString();
                Debug.Log($"BlendShape value '{blendShapeValue}' copied to clipboard from {renderer.name}");
            }
        }

        /// <summary>
        /// Pastes the blend shape value from the system clipboard to the provided SkinnedMeshRenderer and blend shape index.
        /// </summary>
        /// <param name="renderer">The SkinnedMeshRenderer to paste the blend shape value to.</param>
        /// <param name="blendShapeIndex">The index of the blend shape.</param>
        private void PasteBlendShapeValueFromClipboard(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            if (renderer != null && renderer.sharedMesh != null)
            {
                string clipboardContent = EditorGUIUtility.systemCopyBuffer;
                float newValue;
                if (float.TryParse(clipboardContent, out newValue))
                {
                    Debug.Log($"BlendShape value '{newValue}' pasted from clipboard from {renderer.name}");
                    Undo.RecordObject(renderer, "Paste Blend Shape Value");
                    renderer.SetBlendShapeWeight(blendShapeIndex, newValue);
                }
                else
                {
                    Debug.LogWarning("Clipboard does not contain a valid blendshape value.");
                }
            }
        }

        /// <summary>
        /// Sets the blend shape weight to a specific value for the provided SkinnedMeshRenderer with proper undo recording.
        /// </summary>
        /// <param name="renderer">The SkinnedMeshRenderer to set the blend shape weight for.</param>
        /// <param name="blendShapeIndex">The index of the blend shape.</param>
        /// <param name="value">The value to set the blend shape weight to.</param>
        private void SetBlendShapeToValue(SkinnedMeshRenderer renderer, int blendShapeIndex, float value)
        {
            if (renderer != null)
            {
                Debug.Log($"Set Blend Shape {blendShapeIndex} to {value} from {renderer.name}");
                Undo.RecordObject(renderer, $"Set Blend Shape {blendShapeIndex} to {value}");
                renderer.SetBlendShapeWeight(blendShapeIndex, value);
            }
        }

#if MA_EXISTS
        // ###### MA FUNCTIONS ###### 
        private void AddBlendshapeToMASync(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            // Get or add ModularAvatarBlendshapeSync component to the current selected object
            GameObject selectedObject = renderer.gameObject;

            ModularAvatarBlendshapeSync blendshapeSync = selectedObject.GetComponent<ModularAvatarBlendshapeSync>();
            if (!blendshapeSync)
            {
                blendshapeSync = Undo.AddComponent<ModularAvatarBlendshapeSync>(selectedObject);
                Debug.Log("ModularAvatarBlendshapeSync component added to GameObject: " + selectedObject.name);
            }
            else
            {
                Undo.RecordObject(blendshapeSync, "Add Blendshape Binding");
            }

            // Get the name of the blend shape using the index
            string blendshapeName = renderer.sharedMesh.GetBlendShapeName(blendShapeIndex);
            Debug.Log($"Searching blendshape '{blendshapeName}' to ModularAvatarBlendshapeSync bindings on avatar");

            // Find a SkinnedMeshRenderer with the same blend shape in the avatar hierarchy
            GameObject avatar = FindAvatarDescriptor(selectedObject.transform);
            SkinnedMeshRenderer referenceSkinnedMesh = FindSkinnedMeshRendererWithBlendshape(avatar.transform, blendshapeName, renderer);
            Debug.Log($"Found possible reference on SkinnedMeshRenderer '{(referenceSkinnedMesh != null ? referenceSkinnedMesh.name : null)}' with blendshape '{blendshapeName}'.");

            // Calculate the transform path of the referenceSkinnedMesh if found
            string referencePath = referenceSkinnedMesh ? AnimationUtility.CalculateTransformPath(referenceSkinnedMesh.transform, avatar.transform) : null;

            // Create a new binding and add it to the list
            BlendshapeBinding newBinding = new BlendshapeBinding
            {
                ReferenceMesh = referencePath != null ? new AvatarObjectReference { referencePath = referencePath } : null,
                Blendshape = blendshapeName,
                LocalBlendshape = blendshapeName // You can set this to something specific if needed
            };

            // Add the new binding to the blendshapeSync component
            blendshapeSync.Bindings.Add(newBinding);

            // Mark the blendshapeSync component as dirty to ensure changes are saved
            EditorUtility.SetDirty(blendshapeSync);

            Debug.Log($"Blendshape '{blendshapeName}' added to ModularAvatarBlendshapeSync bindings on GameObject: " + selectedObject.name);
        }

        private SkinnedMeshRenderer FindSkinnedMeshRendererWithBlendshape(Transform parent, string blendshapeName, SkinnedMeshRenderer ignoreRenderer = null)
        {
            SkinnedMeshRenderer skinnedMeshRenderer = parent.GetComponent<SkinnedMeshRenderer>();
            if (
                skinnedMeshRenderer != null &&
                skinnedMeshRenderer.sharedMesh != null &&
                skinnedMeshRenderer != ignoreRenderer
            )
            {
                Mesh sharedMesh = skinnedMeshRenderer.sharedMesh;
                int blendShapeCount = sharedMesh.blendShapeCount;
                for (int i = 0; i < blendShapeCount; i++)
                {
                    string currentBlendshapeName = sharedMesh.GetBlendShapeName(i);
                    if (currentBlendshapeName.Equals(blendshapeName))
                    {
                        return skinnedMeshRenderer;
                    }
                }
            }

            foreach (Transform child in parent)
            {
                skinnedMeshRenderer = FindSkinnedMeshRendererWithBlendshape(child, blendshapeName, ignoreRenderer);
                if (skinnedMeshRenderer != null)
                {
                    return skinnedMeshRenderer;
                }
            }

            return null;
        }
#endif

#if VIXEN_EXISTS

        private void AddBlendshapeToVixenSync(SkinnedMeshRenderer renderer, int blendShapeIndex)
        {
            string blandshapeName = renderer.sharedMesh.GetBlendShapeName(blendShapeIndex);
            string name = "Toggle " + blandshapeName;

            float value = renderer.GetBlendShapeWeight(blendShapeIndex) > 0 ? 0 : 100;

            Debug.Log($"Creating Vixen for blendshape '{blandshapeName}' with value '{value}', '{renderer.GetBlendShapeWeight(blendShapeIndex)}'");

            GameObject newObject = createVixenFromBlendshape(name, renderer, blendShapeIndex, renderer.transform, value);

            // Add the new object to the selection
            Selection.activeGameObject = newObject;
        }

#endif

    }
}
