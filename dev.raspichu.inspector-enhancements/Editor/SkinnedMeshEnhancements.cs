using UnityEditor;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditor.IMGUI.Controls;
using System.Linq;

using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Avatars.Components;

#if MA_EXISTS
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

#if VIXEN_EXISTS
using Resilience.Vixen.Components;
#endif


namespace raspichu.inspector_enhancements.editor
{
    public class SkinnedMeshEnhancements : EditorWindow
    {
        private static string blendShapeSearch = "";
        private static List<BlendShapeInfo> blendShapes = new List<BlendShapeInfo>();
        private static bool isFoldedOut = true; // To control the fold-out behavior
        private static SearchField searchField = new SearchField();

        // Dictionary to store selected SkinnedMeshRenderers
        private static Dictionary<int, SkinnedMeshRenderer> persistentSelectedSkinnedMeshes = new Dictionary<int, SkinnedMeshRenderer>();

        private static bool enhancementsEnabled = true; // Track whether enhancements are enabled

        private struct BlendShapeInfo
        {
            public int Index;
            public string Name;
            public GUIContent Content;

            public float Weight;
            public SkinnedMeshRenderer Renderer;
        }


        [MenuItem("Tools/Pichu/Enable SkinnedMesh Enhancements")]
        private static void ToggleSkinnedMeshEnhancements()
        {
            enhancementsEnabled = !enhancementsEnabled; // Toggle the state
            string status = enhancementsEnabled ? "enabled" : "disabled";

            // Refresh the Inspector
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("Tools/Pichu/Enable SkinnedMesh Enhancements", true)]
        private static bool ToggleSkinnedMeshEnhancementsValidation()
        {
            Menu.SetChecked("Tools/Pichu/Enable SkinnedMesh Enhancements", enhancementsEnabled);
            return true; // Always enable the menu item
        }

        public static bool OnBlendShapeUI(UnityEditor.Editor __instance, SerializedProperty ___m_BlendShapeWeights)
        {
            if (!enhancementsEnabled) return true;

            // Update persistent selected SkinnedMeshRenderers
            UpdatePersistentSelectedSkinnedMeshes(__instance.targets);

            // Clear the blend shapes and collect from all selected renderers
            blendShapes.Clear();
            foreach (var renderer in persistentSelectedSkinnedMeshes.Values)
            {
                CollectBlendShapes(renderer);
            }

            DrawBlendShapeUI();

            return false;
        }

        private static void UpdatePersistentSelectedSkinnedMeshes(Object[] targets)
        {
            // Clear the dictionary to ensure it only holds currently selected SkinnedMeshRenderers
            persistentSelectedSkinnedMeshes.Clear();

            // Add currently selected SkinnedMeshRenderers from the targets array
            foreach (var target in targets)
            {
                if (target is SkinnedMeshRenderer renderer) // Check if the target is a SkinnedMeshRenderer
                {
                    // Use the instance ID as the key
                    int instanceId = renderer.GetInstanceID();
                    // Add the renderer to the dictionary if it's not already present
                    persistentSelectedSkinnedMeshes[instanceId] = renderer;
                }
            }
        }

        private static void CollectBlendShapes(SkinnedMeshRenderer renderer)
        {
            if (renderer.sharedMesh == null) return;

            Mesh mesh = renderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                var blendShapeName = mesh.GetBlendShapeName(i);

                // Check if blend shape already exists in the list
                if (!blendShapes.Exists(bs => bs.Name == blendShapeName && bs.Renderer == renderer))
                {
                    blendShapes.Add(new BlendShapeInfo
                    {
                        Index = i,
                        Name = blendShapeName,
                        Content = new GUIContent(blendShapeName),
                        Weight = renderer.GetBlendShapeWeight(i),
                        Renderer = renderer
                    });
                }
            }
        }


        private static void DrawBlendShapeUI()
        {

            // Create a Rect for the "Center Bounds" button
            Rect buttonRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
            buttonRect.width = buttonRect.width / 1.6f; // Set the button width to half of the available width
            buttonRect.x += (EditorGUIUtility.currentViewWidth - buttonRect.width); // Position it to the right

            if (GUI.Button(buttonRect, "Center Bounds"))
            {
                CenterBounds();
            }



            isFoldedOut = EditorGUILayout.Foldout(isFoldedOut, new GUIContent("BlendShapes"), true);

            if (isFoldedOut)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                float searchBoxHeight = EditorGUIUtility.singleLineHeight;
                Rect searchRect = EditorGUILayout.GetControlRect(false, searchBoxHeight);

                blendShapeSearch = searchField.OnGUI(searchRect, blendShapeSearch);

                var groupedBlendShapes = GroupBlendShapesByRenderer();

                // Reverse list
                List<SkinnedMeshRenderer> keys = new List<SkinnedMeshRenderer>(groupedBlendShapes.Keys);
                keys.Reverse();

                foreach (var kvp in keys)
                {
                    var renderer = kvp;
                    var shapes = groupedBlendShapes[renderer];

                    if (shapes.Count == 0) continue;

                    if (shapes.Count > 0 && persistentSelectedSkinnedMeshes.Count > 1)
                    {
                        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel)
                        {
                            alignment = TextAnchor.MiddleCenter,
                            fontSize = 12,
                            normal = { textColor = Color.white }
                        };

                        Rect titleRect = EditorGUILayout.GetControlRect(false, 20);
                        titleRect.height = 20;

                        EditorGUI.LabelField(titleRect, renderer.name, titleStyle);
                    }

                    GUILayout.Space(2); // Reduce the space between the title and the list

                    ReorderableList blendshapeList = new ReorderableList(shapes, typeof(BlendShapeInfo), false, false, false, false)
                    {
                        elementHeight = EditorGUIUtility.singleLineHeight + 2, // Minimize the height
                        drawElementCallback = (rect, index, isActive, isFocused) =>
                        {
                            var blendShape = shapes[index];

                            EditorGUI.LabelField(new Rect(rect.x, rect.y, 150f, EditorGUIUtility.singleLineHeight), blendShape.Content);
                            float currentWeight = blendShape.Weight;

                            float newWeight = EditorGUI.Slider(new Rect(rect.x + 160f, rect.y, rect.width - 160f, EditorGUIUtility.singleLineHeight), blendShape.Weight, 0, 100);

                            if (!Mathf.Approximately(currentWeight, newWeight))
                            {
                                Undo.RecordObject(blendShape.Renderer, "Change Blend Shape Weight");
                                blendShape.Renderer.SetBlendShapeWeight(blendShape.Index, newWeight);
                            }

                            // Right-click context menu
                            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                            {
                                ShowBlendShapeContextMenu(blendShape);
                            }
                        }
                    };

                    blendshapeList.DoLayoutList();

                    GUILayout.Space(-EditorGUIUtility.singleLineHeight);

                }

                EditorGUILayout.EndVertical();
            }
        }

        private static Dictionary<SkinnedMeshRenderer, List<BlendShapeInfo>> GroupBlendShapesByRenderer()
        {
            var groupedBlendShapes = new Dictionary<SkinnedMeshRenderer, List<BlendShapeInfo>>();

            foreach (var renderer in persistentSelectedSkinnedMeshes.Values)
            {
                if (renderer.sharedMesh == null) continue;

                // Prepare a list to hold the blend shapes for this renderer
                var blendShapeList = new List<BlendShapeInfo>();

                Mesh mesh = renderer.sharedMesh;
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    var blendShapeName = mesh.GetBlendShapeName(i);
                    var blendShapeInfo = new BlendShapeInfo
                    {
                        Index = i,
                        Name = blendShapeName,
                        Content = new GUIContent(blendShapeName),
                        Weight = renderer.GetBlendShapeWeight(i),
                        Renderer = renderer
                    };

                    // Only add to the list if it's not already present
                    if (
                        !blendShapeList.Exists(bs => bs.Name == blendShapeName)
                        )
                    {
                        if (!string.IsNullOrEmpty(blendShapeSearch) && !blendShapeName.ToLower().Contains(blendShapeSearch.ToLower()))
                        {
                            continue;
                        }
                        blendShapeList.Add(blendShapeInfo);
                    }
                }

                // Add the list to the grouped blend shapes
                groupedBlendShapes[renderer] = blendShapeList;
            }

            return groupedBlendShapes;
        }


        // Method to center bounds for all selected renderers
        private static void CenterBounds()
        {
            foreach (var renderer in persistentSelectedSkinnedMeshes.Values)
            {
                if (renderer == null) continue;
                Undo.RecordObject(renderer, "Center Bounds");
                renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2);
            }
            InternalEditorUtility.RepaintAllViews();
        }

        // Show context menu for the blend shape
        private static void ShowBlendShapeContextMenu(BlendShapeInfo blendShape)
        {
            if (blendShape.Index < 0) return; // Ensure we have a valid index

            GenericMenu menu = new GenericMenu();

            // Add Copy Name option
            menu.AddItem(new GUIContent("Copy Name"), false, () => CopyBlendShapeName(blendShape));

            // Add Copy Value option
            menu.AddItem(new GUIContent("Copy Value"), false, () => CopyBlendShapeValue(blendShape));

            // Add Paste Value option (disabled if clipboard is not a valid number)
            menu.AddItem(new GUIContent("Paste Value"), false, () => PasteBlendShapeValue(blendShape));
            if (!IsClipboardContainingNumber())
            {
                menu.AddDisabledItem(new GUIContent("Paste Value"));
            }

            // Add separator
            menu.AddSeparator("");

            // Add Set to 0 option
            menu.AddItem(new GUIContent("Set to 0"), false, () => SetBlendShapeValue(blendShape, 0));

            // Add Set to 100 option
            menu.AddItem(new GUIContent("Set to 100"), false, () => SetBlendShapeValue(blendShape, 100));

#if MA_EXISTS
            menu.AddSeparator("");
            menu.AddItem(new GUIContent("Add to MA Blend Sync"), false, () => AddBlendshapeToMASync(blendShape));
#endif

#if VIXEN_EXISTS
        // Add to Vixen
        menu.AddSeparator("");
        menu.AddItem(new GUIContent("Make vixen toggle"), false, () => AddBlendshapeToVixenSync(blendShape));
#endif


            menu.ShowAsContext();
        }

        // Method to copy blend shape name
        private static void CopyBlendShapeName(BlendShapeInfo blendShape)
        {
            EditorGUIUtility.systemCopyBuffer = blendShape.Name;
            Debug.Log($"Copied blend shape name: {blendShape.Name}");
        }

        // Method to copy blend shape value
        private static void CopyBlendShapeValue(BlendShapeInfo blendShape)
        {

            EditorGUIUtility.systemCopyBuffer = blendShape.Weight.ToString();
        }

        // Method to paste blend shape value
        private static void PasteBlendShapeValue(BlendShapeInfo blendShape)
        {
            if (float.TryParse(EditorGUIUtility.systemCopyBuffer, out float newValue))
            {
                Undo.RecordObject(blendShape.Renderer, "Paste Blend Shape Value");
                blendShape.Renderer.SetBlendShapeWeight(blendShape.Index, newValue);
                Debug.Log($"Pasted blend shape value: {newValue} to {blendShape.Name}");
            }
        }

        // Method to set blend shape value
        private static void SetBlendShapeValue(BlendShapeInfo blendShape, float value)
        {
            Undo.RecordObject(blendShape.Renderer, $"Set Blend Shape Value to {value} for {blendShape.Name}");
            blendShape.Renderer.SetBlendShapeWeight(blendShape.Index, value);
            Debug.Log($"Set blend shape value of {blendShape.Name} to: {value}");
        }

        private static bool IsClipboardContainingNumber()
        {
            string clipboardContent = EditorGUIUtility.systemCopyBuffer;
            float result;
            return float.TryParse(clipboardContent, out result);
        }

#if MA_EXISTS

        // ###### MA FUNCTIONS ###### 
        private static void AddBlendshapeToMASync(BlendShapeInfo blendShape)
        {
            // Get or add ModularAvatarBlendshapeSync component to the current selected object
            SkinnedMeshRenderer renderer = blendShape.Renderer;
            GameObject selectedObject = renderer.gameObject;

            ModularAvatarBlendshapeSync blendshapeSync = selectedObject.GetComponent<ModularAvatarBlendshapeSync>();
            if (!blendshapeSync)
            {
                blendshapeSync = Undo.AddComponent<ModularAvatarBlendshapeSync>(selectedObject);
            }
            else
            {
                Undo.RecordObject(blendshapeSync, "Add Blendshape Binding");
            }

            // Get the name of the blend shape using the index
            string blendshapeName = renderer.sharedMesh.GetBlendShapeName(blendShape.Index);

            // Find a SkinnedMeshRenderer with the same blend shape in the avatar hierarchy
            GameObject avatar = FindAvatarDescriptor(selectedObject.transform);
            SkinnedMeshRenderer referenceSkinnedMesh = FindSkinnedMeshRendererWithBlendshape(avatar.transform, blendshapeName, renderer);

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
        }
#endif

#if VIXEN_EXISTS
        // ###### VIXEN FUNCTIONS ###### 
        private static void AddBlendshapeToVixenSync(BlendShapeInfo blendShape)
        {
            SkinnedMeshRenderer renderer = blendShape.Renderer;
            int blendShapeIndex = blendShape.Index;

            string blandshapeName = renderer.sharedMesh.GetBlendShapeName(blendShapeIndex);
            string name = "Toggle " + blandshapeName;

            float value = renderer.GetBlendShapeWeight(blendShapeIndex) > 0 ? 0 : 100;

            Debug.Log($"Creating Vixen for blendshape '{blandshapeName}' with value '{value}', '{renderer.GetBlendShapeWeight(blendShapeIndex)}'");

            GameObject newObject = createVixenFromBlendshape(name, renderer, blendShapeIndex, renderer.transform, value);

            // Add the new object to the selection
            Selection.activeGameObject = newObject;
        }

        private static GameObject createVixenFromBlendshape(string name, SkinnedMeshRenderer render, int blendshapeIndex, Transform position, float value)
        {
            GameObject newObject = new GameObject(name);
            newObject.transform.SetParent(position.parent, false);
            newObject.transform.SetSiblingIndex(position.transform.GetSiblingIndex() + 1);
            Undo.RegisterCreatedObjectUndo(newObject, "Create " + name);

            string blendshapeName = render.sharedMesh.GetBlendShapeName(blendshapeIndex);

            VixenControl vixenControl = newObject.AddComponent<VixenControl>();

            // Create and populate the VixenProperty
            VixenProperty blendShapeProperty = new VixenProperty
            {
                fullClassName = "UnityEngine.SkinnedMeshRenderer",
                propertyName = $"blendShape.{blendshapeName}",
                valueType = VixenValueType.Float,
                floatValue = value,
                flip = false,
                unboundBaked = false
            };

            VixenSubject vixenSubject = new VixenSubject
            {
                selection = VixenSelection.Normal,
                targets = new GameObject[] { render.gameObject },
                childrenOf = new GameObject[] { position.gameObject },
                exceptions = new GameObject[0], // Assuming no exceptions
                properties = new VixenProperty[] { blendShapeProperty }
            };

            // Assign the VixenSubject to the VixenControl
            vixenControl.subjects = new VixenSubject[] { vixenSubject };
            return newObject;
        }

#endif

        private static GameObject FindAvatarDescriptor(Transform startTransform)
        {
            Transform parentTransform = startTransform;
            while (parentTransform != null)
            {
                VRCAvatarDescriptor avatarDescriptor = parentTransform.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor != null)
                {
                    return avatarDescriptor.gameObject;
                }
                parentTransform = parentTransform.parent;
            }

            return null; // No avatar descriptor found in the parent hierarchy
        }

        private static SkinnedMeshRenderer FindSkinnedMeshRendererWithBlendshape(Transform parent, string blendshapeName, SkinnedMeshRenderer ignoreRenderer = null)
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
    }
}
