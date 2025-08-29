using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
using VRC.SDK3.Dynamics.PhysBone.Components;
#if MA_EXISTS
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

#if VIXEN_EXISTS
using Resilience.Vixen.Components;
#endif

#if PA_EXISTS
using Prefabulous.Universal.Common.Runtime;
#endif

namespace raspichu.inspector_enhancements.editor
{
    public class SkinnedMeshEnhancements : EditorWindow
    {
        private static SearchField searchField = new SearchField();
        private static string blendShapeSearch = "";
        private static bool isFoldedOut = true; // To control the fold-out behavior

        // Dictionary to store selected SkinnedMeshRenderers
        private static Dictionary<int, SkinnedMeshRenderer> persistentSelectedSkinnedMeshes =
            new Dictionary<int, SkinnedMeshRenderer>();
        private static bool filterZeroWeight = false; // Track whether to filter zero weights

        // HashSet to store the last selected SkinnedMeshRenderers
        private static HashSet<int> lastSelectedRenderers = new HashSet<int>();
        private static Dictionary<string, List<string>> groups =
            new Dictionary<string, List<string>>();
        private static string selectedGroup = null;

        private static readonly Dictionary<string, GUIStyle> _styles =
            new Dictionary<string, GUIStyle>();

        private struct BlendShapeInfo
        {
            public int Index;
            public string Name;
            public GUIContent Content;

            public float Weight;
            public SkinnedMeshRenderer Renderer;
        }

        private const string EnhancementsKey = "Pichu_SkinnedMeshEnhancementsEnabled";
        private static bool enhancementsEnabled = EditorPrefs.GetBool(EnhancementsKey, true);

        [MenuItem("Tools/Pichu/Enable SkinnedMesh Enhancements")]
        private static void ToggleSkinnedMeshEnhancements()
        {
            enhancementsEnabled = !enhancementsEnabled; // Toggle the state
            EditorPrefs.SetBool(EnhancementsKey, enhancementsEnabled);
            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("Tools/Pichu/Enable SkinnedMesh Enhancements", true)]
        private static bool ToggleSkinnedMeshEnhancementsValidation()
        {
            Menu.SetChecked("Tools/Pichu/Enable SkinnedMesh Enhancements", enhancementsEnabled);
            return true; // Always enable the menu item
        }

        static SkinnedMeshEnhancements()
        {
            // Default styles
            _styles["MenuButton"] = new GUIStyle(EditorStyles.miniButton)
            {
                padding = new RectOffset(0, 0, 0, 0),
                alignment = TextAnchor.MiddleCenter,
                fontSize = 10,
            };

            _styles["Label"] = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 12,
                normal = { textColor = Color.white },
            };
        }

        public static bool OnBlendShapeUI(
            UnityEditor.Editor __instance,
            SerializedProperty ___m_BlendShapeWeights
        )
        {
            if (!enhancementsEnabled)
                return true;

            // If the targets are not the same as the selected one
            if (SelectionChanged(__instance.targets))
            {
                UpdatePersistentSelectedSkinnedMeshes(__instance.targets);
                groups = CreateGroups();
                selectedGroup = null;
            }

            DrawBlendShapeUI();

            return false;
        }

        private static bool SelectionChanged(Object[] targets)
        {
            // Show debuglog of types
            HashSet<int> currentSelection = new HashSet<int>(
                targets.OfType<SkinnedMeshRenderer>().Select(r => r.GetInstanceID())
            );
            if (lastSelectedRenderers.SetEquals(currentSelection))
                return false;

            lastSelectedRenderers = currentSelection;
            return true;
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

        private static void DrawBlendShapeUI()
        {
            // Center Bounds button
            Rect buttonRect = EditorGUILayout.GetControlRect(
                false,
                EditorGUIUtility.singleLineHeight
            );
            buttonRect.width /= 1.6f;
            buttonRect.x += (EditorGUIUtility.currentViewWidth - buttonRect.width);
            if (GUI.Button(buttonRect, "Center Bounds"))
                CenterBounds();

            // Foldout
            SerializedObject dummySO = new SerializedObject(
                persistentSelectedSkinnedMeshes.Values.FirstOrDefault()
            );
            if (dummySO != null)
            {
                SerializedProperty dummyProp = dummySO.FindProperty("m_BlendShapeWeights");

                Rect foldoutRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(foldoutRect, new GUIContent("BlendShapes"), dummyProp);

                // Foldout header group que mantiene el menú de prefab override
                isFoldedOut = EditorGUI.BeginFoldoutHeaderGroup(
                    foldoutRect,
                    isFoldedOut,
                    new GUIContent("BlendShapes")
                );

                EditorGUI.EndFoldoutHeaderGroup();
                EditorGUI.EndProperty();
            }

            if (!isFoldedOut)
                return;

            // Default static values for the list
            GUIContent dropDownIcon = EditorGUIUtility.IconContent("d_icon dropdown");
            float padding = 5f;
            float menuButtonWidth = 16f; // width of the context menu button

            string lowerSearch = blendShapeSearch.ToLower();

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // Horizontal row for + button, search, and filter toggle
            EditorGUILayout.BeginHorizontal();

            // + Button
            if (GUILayout.Button("▼", EditorStyles.miniButton, GUILayout.Width(23)))
            {
                ShowBlendShapeContextMenuHeader();
            }

            // Filter search Box
            Rect searchRect = GUILayoutUtility.GetRect(
                1,
                EditorGUIUtility.singleLineHeight,
                GUILayout.ExpandWidth(true)
            );
            searchRect.y += 3;
            blendShapeSearch = searchField.OnGUI(searchRect, blendShapeSearch);

            // Filter groups
            List<string> currentGroupList = null;
            if (!string.IsNullOrEmpty(selectedGroup) && groups.ContainsKey(selectedGroup))
                currentGroupList = groups[selectedGroup];

            List<string> groupTitles = groups.Keys.ToList();

            groupTitles =
                (groups.Count == 1 && groups.ContainsKey("None"))
                    ? new List<string> { "All" }
                    : groupTitles.Prepend("All").ToList();
            // Disable grouptitle when is only all
            bool isGroupTitleEnabled = !(groupTitles.Count == 1 && groupTitles[0] == "All");
            

            // Determine the current index based on the selection
            int currentIndex = selectedGroup != null ? groupTitles.IndexOf(selectedGroup) : 0;

            // Show the Popup
            EditorGUI.BeginDisabledGroup(!isGroupTitleEnabled);
            int newIndex = EditorGUILayout.Popup(
                currentIndex,
                groupTitles.ToArray(),
                GUILayout.Width(100)
            );
            EditorGUI.EndDisabledGroup();

            // Update the selection if it changed
            if (newIndex != currentIndex)
            {
                selectedGroup = newIndex == 0 ? null : groupTitles[newIndex];
            }

            // Filter Toggle
            filterZeroWeight = GUILayout.Toggle(
                filterZeroWeight,
                "0",
                EditorStyles.miniButton,
                GUILayout.Width(30)
            );

            EditorGUILayout.EndHorizontal();

            // Add padding
            GUILayout.Space(5);

            // Iterate selected renderers
            foreach (var renderer in persistentSelectedSkinnedMeshes.Values.Reverse())
            {
                if (renderer == null || renderer.sharedMesh == null)
                    continue;

                SerializedObject so = new SerializedObject(renderer);
                SerializedProperty blendShapesProp = so.FindProperty("m_BlendShapeWeights");
                Mesh mesh = renderer.sharedMesh;

                // Title for multiple renderers
                if (persistentSelectedSkinnedMeshes.Count > 1)
                {
                    Rect titleRect = EditorGUILayout.GetControlRect(false, 20);
                    EditorGUI.LabelField(titleRect, renderer.name, _styles["Label"]);
                }

                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                List<int> visibleBlendShapeIndices = new List<int>();
                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);

                    // Apply filters

                    // Search filter
                    if (
                        !string.IsNullOrEmpty(lowerSearch)
                        && !blendShapeName.ToLower().Contains(lowerSearch)
                    )
                    {
                        continue;
                    }

                    // Group filter
                    if (currentGroupList != null && !currentGroupList.Contains(blendShapeName))
                    {
                        continue;
                    }

                    // Zero values filter
                    if (filterZeroWeight)
                    {
                        SerializedProperty weightProp =
                            (i < blendShapesProp.arraySize)
                                ? blendShapesProp.GetArrayElementAtIndex(i)
                                : null;
                        float weight = weightProp != null ? weightProp.floatValue : 0f;
                        if (weight == 0f)
                        {
                            continue;
                        }
                    }
                    visibleBlendShapeIndices.Add(i);
                }

                // Show "No blendshape available" if list is empty
                if (visibleBlendShapeIndices.Count == 0)
                {
                    Rect rect = EditorGUILayout.GetControlRect(
                        false,
                        EditorGUIUtility.singleLineHeight
                    );
                    EditorGUI.LabelField(rect, "No blendshapes available", _styles["Label"]);
                    EditorGUILayout.EndVertical();
                    continue;
                }

                // Iterate the filtered blendshapes
                foreach (int i in visibleBlendShapeIndices)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);

                    // Obtain the SerializedProperty if it exists
                    SerializedProperty weightProp =
                        (i < blendShapesProp.arraySize)
                            ? blendShapesProp.GetArrayElementAtIndex(i)
                            : null;

                    float weight = weightProp != null ? weightProp.floatValue : 0f;

                    Rect rect = EditorGUILayout.GetControlRect(
                        false,
                        EditorGUIUtility.singleLineHeight
                    );

                    float menuButtonHeight = rect.height; // forzamos que tenga la misma altura que la fila
                    float labelWidth = Mathf.Min(250f, rect.width * 0.4f);
                    float sliderWidth = rect.width - labelWidth - menuButtonWidth - padding;

                    if (weightProp != null)
                        EditorGUI.BeginProperty(rect, new GUIContent(blendShapeName), weightProp);

                    // Button "+" at the start
                    Rect plusRect = new Rect(rect.x, rect.y, menuButtonWidth, rect.height);
                    if (GUI.Button(plusRect, dropDownIcon, _styles["MenuButton"]))
                    {
                        ShowBlendShapeContextMenu(
                            new BlendShapeInfo
                            {
                                Index = i,
                                Name = blendShapeName,
                                Content = new GUIContent(blendShapeName),
                                Weight = weight,
                                Renderer = renderer,
                            }
                        );
                    }

                    // Label
                    EditorGUI.LabelField(
                        new Rect(
                            rect.x + menuButtonWidth + padding,
                            rect.y,
                            labelWidth,
                            rect.height
                        ),
                        blendShapeName
                    );

                    // Slider
                    EditorGUI.BeginChangeCheck();
                    float newWeight = EditorGUI.Slider(
                        new Rect(
                            rect.x + menuButtonWidth + padding + labelWidth + padding,
                            rect.y,
                            sliderWidth,
                            rect.height
                        ),
                        weight,
                        0f,
                        100f
                    );
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (weightProp != null)
                        {
                            weightProp.floatValue = newWeight;
                            weightProp.serializedObject.ApplyModifiedProperties();
                        }
                        else
                        {
                            // Create temporal value using SetBlendShapeWeight for non-serialized blend shapes
                            Undo.RecordObject(renderer, "Change BlendShape");
                            renderer.SetBlendShapeWeight(i, newWeight);
                        }
                    }

                    if (weightProp != null)
                        EditorGUI.EndProperty();
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndVertical();
        }

        // Method to center bounds for all selected renderers
        private static void CenterBounds()
        {
            foreach (var renderer in persistentSelectedSkinnedMeshes.Values)
            {
                if (renderer == null)
                    continue;
                Undo.RecordObject(renderer, "Center Bounds");
                renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2);
            }
            InternalEditorUtility.RepaintAllViews();
        }

        private static void ShowBlendShapeContextMenuHeader()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(
                new GUIContent("Randomize All Blendshapes"),
                false,
                () =>
                {
                    RandomizeBlendshapes(persistentSelectedSkinnedMeshes.Values.ToList());
                }
            );

            menu.ShowAsContext();
        }

        // Show context menu for the blend shape
        private static void ShowBlendShapeContextMenu(BlendShapeInfo blendShape)
        {
            if (blendShape.Index < 0)
                return; // Ensure we have a valid index

            GenericMenu menu = new GenericMenu();

            // Add Copy Name option
            menu.AddItem(new GUIContent("Copy Name"), false, () => CopyBlendShapeName(blendShape));

            // Add separator
            menu.AddSeparator("");

            // Add Set to 0 option
            menu.AddItem(
                new GUIContent("Set to 0"),
                false,
                () => SetBlendShapeValue(blendShape, 0)
            );

            // Add Set to 100 option
            menu.AddItem(
                new GUIContent("Set to 100"),
                false,
                () => SetBlendShapeValue(blendShape, 100)
            );

#if MA_EXISTS
            menu.AddSeparator("");
            menu.AddItem(
                new GUIContent("Add to MA Blend Sync"),
                false,
                () => AddBlendshapeToMASync(blendShape)
            );
            menu.AddItem(
                new GUIContent("Add to Delete MA Shape Changer"),
                false,
                () => AddBlendshapeToMADelete(blendShape)
            );
#endif

#if PA_EXISTS
            // Add to Prefabulous
            menu.AddSeparator("");
            menu.AddItem(
                new GUIContent("Add to PA Delete Polygon"),
                false,
                () => AddBlendshapeToPADelete(blendShape)
            );
#endif

#if VIXEN_EXISTS
            // Add to Vixen
            menu.AddSeparator("");
            menu.AddItem(
                new GUIContent("Make vixen toggle"),
                false,
                () => AddBlendshapeToVixenSync(blendShape)
            );
#endif

            menu.ShowAsContext();
        }

        // Method to copy blend shape name
        private static void CopyBlendShapeName(BlendShapeInfo blendShape)
        {
            EditorGUIUtility.systemCopyBuffer = blendShape.Name;
            Debug.Log($"Copied blend shape name: {blendShape.Name}");
        }

        // Method to set blend shape value
        private static void SetBlendShapeValue(BlendShapeInfo blendShape, float value)
        {
            Undo.RecordObject(
                blendShape.Renderer,
                $"Set Blend Shape Value to {value} for {blendShape.Name}"
            );
            blendShape.Renderer.SetBlendShapeWeight(blendShape.Index, value);
            Debug.Log($"Set blend shape value of {blendShape.Name} to: {value}");

            EditorUtility.SetDirty(blendShape.Renderer);
        }

#if MA_EXISTS

        // ###### MA FUNCTIONS ######
        private static void AddBlendshapeToMASync(BlendShapeInfo blendShape)
        {
            // Get or add ModularAvatarBlendshapeSync component to the current selected object
            SkinnedMeshRenderer renderer = blendShape.Renderer;
            GameObject selectedObject = renderer.gameObject;

            ModularAvatarBlendshapeSync blendshapeSync =
                selectedObject.GetComponent<ModularAvatarBlendshapeSync>();
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
            SkinnedMeshRenderer referenceSkinnedMesh = FindSkinnedMeshRendererWithBlendshape(
                avatar.transform,
                blendshapeName,
                renderer
            );

            // Calculate the transform path of the referenceSkinnedMesh if found
            string referencePath = referenceSkinnedMesh
                ? AnimationUtility.CalculateTransformPath(
                    referenceSkinnedMesh.transform,
                    avatar.transform
                )
                : null;

            // Create a new binding and add it to the list
            BlendshapeBinding newBinding = new BlendshapeBinding
            {
                ReferenceMesh =
                    referencePath != null
                        ? new AvatarObjectReference { referencePath = referencePath }
                        : null,
                Blendshape = blendshapeName,
                LocalBlendshape = blendshapeName, // You can set this to something specific if needed
            };

            // Add the new binding to the blendshapeSync component
            blendshapeSync.Bindings.Add(newBinding);

            // Mark the blendshapeSync component as dirty to ensure changes are saved
            EditorUtility.SetDirty(blendshapeSync);
        }

        private static void AddBlendshapeToMADelete(BlendShapeInfo blendShape)
        {
            // Get or add ModularAvatarShapeChanger component to the current selected object
            SkinnedMeshRenderer renderer = blendShape.Renderer;
            GameObject selectedObject = renderer.gameObject;

            ModularAvatarShapeChanger shapeChanger =
                selectedObject.GetComponent<ModularAvatarShapeChanger>();
            if (!shapeChanger)
            {
                shapeChanger = Undo.AddComponent<ModularAvatarShapeChanger>(selectedObject);
            }
            else
            {
                Undo.RecordObject(shapeChanger, "Add Blendshape Binding");
            }

            // Get the name of the blend shape using the index
            string blendshapeName = renderer.sharedMesh.GetBlendShapeName(blendShape.Index);
            // If the blendshape is already in the list (With the same object)
            if (
                shapeChanger.Shapes.Exists(s =>
                    s.ShapeName == blendshapeName
                    && s.Object.referencePath
                        == AnimationUtility.CalculateTransformPath(
                            renderer.transform,
                            FindAvatarDescriptor(renderer.transform).transform
                        )
                )
            )
            {
                return;
            }

            // Shape changer is a list ChangedShape
            /*
                public AvatarObjectReference Object;
                public string ShapeName;
                public ShapeChangeType ChangeType;
                public float Value;
            */
            ChangedShape newShape = new ChangedShape
            {
                Object = new AvatarObjectReference
                {
                    referencePath = AnimationUtility.CalculateTransformPath(
                        renderer.transform,
                        FindAvatarDescriptor(renderer.transform).transform
                    ),
                },
                ShapeName = blendshapeName,
                ChangeType = ShapeChangeType.Delete,
                Value = 0,
            };

            // Add the new binding to the blendshapeSync component
            shapeChanger.Shapes.Add(newShape);

            // Find a SkinnedMeshRenderer with the same blend shape in the avatar hierarchy
        }

#endif

#if PA_EXISTS
        private static void AddBlendshapeToPADelete(BlendShapeInfo blendShape)
        {
            // Get or add PrefabulousDeletePolygons component to the current selected object
            SkinnedMeshRenderer renderer = blendShape.Renderer;
            GameObject selectedObject = renderer.gameObject;

            PrefabulousDeletePolygons deletePolygons =
                selectedObject.GetComponent<PrefabulousDeletePolygons>();
            if (!deletePolygons)
            {
                deletePolygons = Undo.AddComponent<PrefabulousDeletePolygons>(selectedObject);
            }
            else
            {
                Undo.RecordObject(deletePolygons, "Add Blendshape Binding");
            }

            // Enable limitToSpecificMeshes on the component
            deletePolygons.limitToSpecificMeshes = true;

            // Generate a list of renderers if it's not already present
            if (deletePolygons.renderers == null)
            {
                deletePolygons.renderers = new SkinnedMeshRenderer[0];
            }

            // Add the renderer to the list of renderers if it's not already present
            if (!deletePolygons.renderers.Contains(renderer))
            {
                deletePolygons.renderers = deletePolygons.renderers.Append(renderer).ToArray();
            }

            // Generate a list of blendshapes if it's not already present
            if (deletePolygons.blendShapes == null)
            {
                deletePolygons.blendShapes = new string[0];
            }

            // Get the name of the blend shape using the index
            string blendshapeName = renderer.sharedMesh.GetBlendShapeName(blendShape.Index);

            // Add the blendshape to the list if it's not already present
            if (!deletePolygons.blendShapes.Contains(blendshapeName))
            {
                deletePolygons.blendShapes = deletePolygons
                    .blendShapes.Append(blendshapeName)
                    .ToArray();
            }

            // Set blendshape to 100
            renderer.SetBlendShapeWeight(blendShape.Index, 100);
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

            Debug.Log(
                $"Creating Vixen for blendshape '{blandshapeName}' with value '{value}', '{renderer.GetBlendShapeWeight(blendShapeIndex)}'"
            );

            GameObject newObject = createVixenFromBlendshape(
                name,
                renderer,
                blendShapeIndex,
                renderer.transform,
                value
            );

            // Add the new object to the selection
            Selection.activeGameObject = newObject;
        }

        private static GameObject createVixenFromBlendshape(
            string name,
            SkinnedMeshRenderer render,
            int blendshapeIndex,
            Transform position,
            float value
        )
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
                unboundBaked = false,
            };

            VixenSubject vixenSubject = new VixenSubject
            {
                selection = VixenSelection.Normal,
                targets = new GameObject[] { render.gameObject },
                childrenOf = new GameObject[] { position.gameObject },
                exceptions = new GameObject[0], // Assuming no exceptions
                properties = new VixenProperty[] { blendShapeProperty },
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
                VRCAvatarDescriptor avatarDescriptor =
                    parentTransform.GetComponent<VRCAvatarDescriptor>();
                if (avatarDescriptor != null)
                {
                    return avatarDescriptor.gameObject;
                }
                parentTransform = parentTransform.parent;
            }

            return null; // No avatar descriptor found in the parent hierarchy
        }

        private static SkinnedMeshRenderer FindSkinnedMeshRendererWithBlendshape(
            Transform parent,
            string blendshapeName,
            SkinnedMeshRenderer ignoreRenderer = null
        )
        {
            SkinnedMeshRenderer skinnedMeshRenderer = parent.GetComponent<SkinnedMeshRenderer>();
            if (
                skinnedMeshRenderer != null
                && skinnedMeshRenderer.sharedMesh != null
                && skinnedMeshRenderer != ignoreRenderer
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
                skinnedMeshRenderer = FindSkinnedMeshRendererWithBlendshape(
                    child,
                    blendshapeName,
                    ignoreRenderer
                );
                if (skinnedMeshRenderer != null)
                {
                    return skinnedMeshRenderer;
                }
            }

            return null;
        }

        private static void RandomizeBlendshapes(List<SkinnedMeshRenderer> renderers)
        {
            foreach (var renderer in renderers)
            {
                if (renderer == null || renderer.sharedMesh == null)
                    continue;
                Undo.RecordObject(renderer, "Randomize Blendshapes");
                for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                {
                    float weight = Random.Range(0, 100);
                    renderer.SetBlendShapeWeight(i, weight);
                }
            }
        }

        private static Dictionary<string, List<string>> CreateGroups()
        {
            Dictionary<string, List<string>> groupedBlendshapes =
                new Dictionary<string, List<string>>();

            foreach (var renderer in persistentSelectedSkinnedMeshes.Values)
            {
                if (renderer == null || renderer.sharedMesh == null)
                    continue;

                Mesh mesh = renderer.sharedMesh;

                string currentGroup = "None";

                for (int i = 0; i < mesh.blendShapeCount; i++)
                {
                    string blendShapeName = mesh.GetBlendShapeName(i);

                    // Detect group header → 3 or more non-letter symbols followed by text
                    if (IsGroupTitle(blendShapeName))
                    {
                        currentGroup = ExtractGroupName(blendShapeName);

                        if (!groupedBlendshapes.ContainsKey(currentGroup))
                            groupedBlendshapes[currentGroup] = new List<string>();

                        // continue; // Skip adding this as a blendshape
                    }

                    // If no title detected, add to current group
                    if (!groupedBlendshapes.ContainsKey(currentGroup))
                        groupedBlendshapes[currentGroup] = new List<string>();

                    groupedBlendshapes[currentGroup].Add(blendShapeName);
                }
            }

            return groupedBlendshapes;
        }

        // Check if name looks like a group title: >=3 same symbol then text
        private static bool IsGroupTitle(string name)
        {
            // Example match: "==== Face ====", "____ mouth ____", "+++ Hair +++"
            // Regex: start with symbols repeated 3+ times, then optional spaces, then letters
            return System.Text.RegularExpressions.Regex.IsMatch(
                name,
                @"^[^\w]{3,}\s*\w+",
                RegexOptions.IgnoreCase
            );
        }

        // Extract the actual text for the group
        private static string ExtractGroupName(string name)
        {
            // Remove all non-letter characters from start and end
            string result = System.Text.RegularExpressions.Regex.Replace(
                name,
                @"^[^\w]+|[^\w]+$",
                ""
            );

            if (string.IsNullOrEmpty(result))
            {
                return "None";
            }

            if (result.Trim().ToLower() == "mmd")
            {
                return "MMD";
            }

            return char.ToUpper(result[0]) + result.Substring(1).ToLower();
        }
    }
}
