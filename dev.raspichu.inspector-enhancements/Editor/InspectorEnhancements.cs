using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Avatars.Components;
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Linq;

public class InspectorEnhancements : EditorWindow
{
    private GameObject[] selectedObjects;

    private Dictionary<string, bool> showBox = new Dictionary<string, bool>()
    {
        { "selectedObjects", false },
        { "transform", true },
        { "blendshapes", false },
        { "skinnedMeshOperations", false },
        { "physBonesOperations", false},
    };


    // Different types of components can be added here

    // Skinned mesh
    private SkinnedMeshRenderer[] skinnedMeshRenderers;
    private string blendShapeSearch = "";
    private Vector2 blendShapeScrollPosition = Vector2.zero;

    // Physbones
    private VRCPhysBone[] physBones;
    private enum PhysBoneSetup
    {
        Soft,
        Default,
        Hard
    }
    private PhysBoneSetup selectedPhysBoneSetup = PhysBoneSetup.Default; // Default setup

    [MenuItem("Window/Pichu/Inspector Enhancements")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(InspectorEnhancements), false, "Inspector Enhancements");
    }

    private void OnSelectionChange()
    {
        // Called whenever selection changes in the Unity Editor
        selectedObjects = Selection.gameObjects;

        // Update skinnedMeshRenderers array for the selectedObjects
        skinnedMeshRenderers = GetSkinnedMeshRenderers(selectedObjects);

        // Update physBone for the selectedObject
        physBones = GetPhysBones(selectedObjects);



        // Set showBox dictionary based on current selection
        SetShowBox();

        // Trigger GUI repaint
        Repaint();
    }

    private void OnGUI()
    {

        DrawSelectedGameObjects(selectedObjects);

        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            return;
        }

        // Transform box
        DrawTransformOperationsSection();

        // Physbone box
        if (skinnedMeshRenderers.Length != selectedObjects.Length || (physBones != null && physBones.Length > 0))
            DrawPhysBoneOperationsSection();

        // Skinned mesh box
        if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
            DrawSkinnedMeshOperationsSection();

    }


    private void SetShowBox()
    {
        bool allSelectedAreSkinnedMesh = true;

        foreach (GameObject obj in selectedObjects)
        {
            if (obj.GetComponent<SkinnedMeshRenderer>() == null)
            {
                allSelectedAreSkinnedMesh = false;
            }
            // Early exit if both conditions are already false
            if (!allSelectedAreSkinnedMesh)
            {
                break;
            }
        }

        showBox["transform"] = false;
        showBox["physBonesOperations"] = !allSelectedAreSkinnedMesh;
        showBox["blendshapes"] = allSelectedAreSkinnedMesh;
        showBox["skinnedMeshOperations"] = allSelectedAreSkinnedMesh;

        // If no other component is found, show the transform operations
        if (!allSelectedAreSkinnedMesh)
        {
            showBox["transform"] = true;
        }
    }


    private void DrawSelectedGameObjects(GameObject[] objects)
    {
        if (objects != null && objects.Length > 0)
        {
            EditorGUILayout.BeginHorizontal();

            // Display the first selected object using ObjectField
            objects[0] = EditorGUILayout.ObjectField("Selected", objects[0], typeof(GameObject), true) as GameObject;

            if (objects.Length > 1)
            {
                // Create the foldout label
                string label = $"+ {objects.Length - 1} more";
                GUILayout.Label(label);

                // Detect if the label is clicked
                Rect labelRect = GUILayoutUtility.GetLastRect();
                EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

                if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                {
                    showBox["selectedObjects"] = !showBox["selectedObjects"];
                    Event.current.Use();
                }
            }

            EditorGUILayout.EndHorizontal();

            if (objects.Length > 1 && showBox["selectedObjects"])
            {
                EditorGUI.indentLevel++;
                for (int i = 1; i < objects.Length; i++)
                {
                    EditorGUILayout.ObjectField($"Selected {i + 1}", objects[i], typeof(GameObject), true);
                }
                EditorGUI.indentLevel--;
            }
        }
        else
        {
            EditorGUILayout.LabelField("No GameObjects selected.");
        }
    }



    private SkinnedMeshRenderer[] GetSkinnedMeshRenderers(GameObject[] objects)
    {
        List<SkinnedMeshRenderer> renderers = new List<SkinnedMeshRenderer>();
        foreach (GameObject obj in objects)
        {
            SkinnedMeshRenderer renderer = obj.GetComponent<SkinnedMeshRenderer>();
            if (renderer != null)
            {
                renderers.Add(renderer);
            }
        }
        return renderers.ToArray();
    }

    private VRCPhysBone[] GetPhysBones(GameObject[] objects)
    {
        List<VRCPhysBone> physBones = new List<VRCPhysBone>();
        foreach (GameObject obj in objects)
        {
            VRCPhysBone physBone = obj.GetComponent<VRCPhysBone>();
            if (physBone != null)
            {
                physBones.Add(physBone);
            }
        }
        return physBones.ToArray();
    }

    private void DrawPhysBoneOperationsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Toggle arrow for physbone operations section
        GUIContent physBonesOperationsContent = new GUIContent("PhysBones Operations", EditorGUIUtility.FindTexture("ArrowRight"));
        Rect physBonesOperationsToggleRect = GUILayoutUtility.GetRect(physBonesOperationsContent, EditorStyles.foldout);
        if (GUI.Button(physBonesOperationsToggleRect, physBonesOperationsContent, EditorStyles.foldout))
        {
            showBox["physBonesOperations"] = !showBox["physBonesOperations"];
        }

        if (showBox["physBonesOperations"])
        {
            EditorGUI.indentLevel++;

            // Determine if all selected objects have PhysBone components
            bool allHavePhysBone = selectedObjects.All(obj => obj.GetComponent<VRCPhysBone>() != null);

            EditorGUILayout.BeginHorizontal();

            // Enum selector for PhysBone setup (use the first object's setup as reference)
            selectedPhysBoneSetup = (PhysBoneSetup)EditorGUILayout.EnumPopup("Forces", selectedPhysBoneSetup);

            // Button to setup or create PhysBone for each selected object
            string buttonLabel = allHavePhysBone ? "Set" : "Create";
            if (GUILayout.Button(buttonLabel, GUILayout.Width(100)))
            {
                foreach (var obj in selectedObjects)
                {
                    Debug.Log("Setting up PhysBone for GameObject: " + obj.name);


                    VRCPhysBone physBoneComponent = obj.GetComponent<VRCPhysBone>();
                    if (!physBoneComponent)
                    {
                        physBoneComponent = obj.AddComponent<VRCPhysBone>();
                        Debug.Log("PhysBone component added to GameObject: " + obj.name);
                        Undo.RegisterCreatedObjectUndo(physBoneComponent, "Setup PhysBone");
                    }
                    else
                    {
                        Undo.RecordObject(physBoneComponent, "Setup PhysBone");
                    }


                    physBoneComponent.integrationType = VRC.Dynamics.VRCPhysBoneBase.IntegrationType.Advanced;
                    // Implement logic to setup PhysBone based on selectedPhysBoneSetup
                    switch (selectedPhysBoneSetup)
                    {
                        case PhysBoneSetup.Soft:
                            // Add setup logic for soft parameters
                            physBoneComponent.pull = 0.05f;
                            physBoneComponent.spring = 0.95f;
                            physBoneComponent.stiffness = 0.05f;
                            break;
                        case PhysBoneSetup.Default:
                            // Add setup logic for medium parameters
                            physBoneComponent.pull = 0.2f;
                            physBoneComponent.spring = 0.2f;
                            physBoneComponent.stiffness = 0.2f;
                            break;
                        case PhysBoneSetup.Hard:
                            // Add setup logic for hard parameters
                            physBoneComponent.pull = 0.95f;
                            physBoneComponent.spring = 0.05f;
                            physBoneComponent.stiffness = 0.95f;
                            break;
                        default:
                            break;
                    }

                    // Record prefab modifications
                    PrefabUtility.RecordPrefabInstancePropertyModifications(physBoneComponent);
                }
            }

            // Button to remove PhysBone for each selected object
            if (allHavePhysBone && GUILayout.Button("Remove", GUILayout.Width(100)))
            {
                foreach (var obj in selectedObjects)
                {
                    VRCPhysBone physBoneComponent = obj.GetComponent<VRCPhysBone>();
                    if (physBoneComponent)
                    {
                        Undo.DestroyObjectImmediate(physBoneComponent);
                        Debug.Log("PhysBone component removed from GameObject: " + obj.name);
                    }
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the transform operations section in the inspector.
    /// </summary>
    private void DrawTransformOperationsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Toggle arrow for transform operations section
        GUIContent transformOperationsContent = new GUIContent("Transform Operations", EditorGUIUtility.FindTexture("ArrowRight"));
        Rect transformOperationsToggleRect = GUILayoutUtility.GetRect(transformOperationsContent, EditorStyles.foldout);
        if (GUI.Button(transformOperationsToggleRect, transformOperationsContent, EditorStyles.foldout))
        {
            showBox["transform"] = !showBox["transform"];
        }

        if (showBox["transform"])
        {
            EditorGUI.indentLevel++;

            // Display button for resetting everything
            GUIContent resetTransformContent = new GUIContent("Reset Transform", "Resets the transform to default values (position: 0, rotation: 0, scale: 1).");
            if (GUILayout.Button(resetTransformContent))
            {
                foreach (var obj in selectedObjects)
                {
                    Undo.RecordObject(obj.transform, "Reset Transform");
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = Vector3.one;
                }
            }

            // Display buttons for setting position, rotation, and scale
            EditorGUILayout.BeginHorizontal();

            GUIContent setPositionContent = new GUIContent("Set 0 Position", "Sets the local position to (0, 0, 0).");
            if (GUILayout.Button(setPositionContent))
            {
                foreach (var obj in selectedObjects)
                {
                    Undo.RecordObject(obj.transform, "Set 0 Position");
                    obj.transform.localPosition = Vector3.zero;
                }
            }

            GUIContent setRotationContent = new GUIContent("Set 0 Rotation", "Sets the local rotation to (0, 0, 0).");
            if (GUILayout.Button(setRotationContent))
            {
                foreach (var obj in selectedObjects)
                {
                    Undo.RecordObject(obj.transform, "Set 0 Rotation");
                    obj.transform.localRotation = Quaternion.identity;
                }
            }

            GUIContent setScaleContent = new GUIContent("Set 1 Scale", "Sets the local scale to (1, 1, 1).");
            if (GUILayout.Button(setScaleContent))
            {
                foreach (var obj in selectedObjects)
                {
                    Undo.RecordObject(obj.transform, "Set 1 Scale");
                    obj.transform.localScale = Vector3.one;
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawSkinnedMeshOperationsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Toggle arrow for skinned mesh operations section
        GUIContent skinnedMeshOperationsContent = new GUIContent("Skinned Mesh Operations", EditorGUIUtility.FindTexture("ArrowRight"));
        Rect skinnedMeshOperationsToggleRect = GUILayoutUtility.GetRect(skinnedMeshOperationsContent, EditorStyles.foldout);
        if (GUI.Button(skinnedMeshOperationsToggleRect, skinnedMeshOperationsContent, EditorStyles.foldout))
        {
            showBox["skinnedMeshOperations"] = !showBox["skinnedMeshOperations"];
        }

        if (showBox["skinnedMeshOperations"])
        {
            EditorGUI.indentLevel++;

            GUIContent centerBoundsContent = new GUIContent("Center Bounds", "Set the local bounds of the SkinnedMeshRenderer to center (0, 0, 0) with an extend of (1, 1, 1).");
            if (GUILayout.Button(centerBoundsContent))
            {
                Undo.RecordObjects(skinnedMeshRenderers, "Center Bounds");
                foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
                {
                    renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2);
                }
            }

            DrawBlendShapeSearchSection();

            EditorGUI.indentLevel--;
        }

        EditorGUILayout.EndVertical();
    }

    /// <summary>
    /// Draws the blend shape search section in the inspector.
    /// </summary>
    private class BlendShapeInfo
    {
        public SkinnedMeshRenderer Renderer;
        public int Index;
        public string Name;
    }

    private List<BlendShapeInfo> blendShapeInfos = new List<BlendShapeInfo>();

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

    private void DrawBlendShapeSearchSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        // Toggle arrow for blendshape search section
        GUIStyle toggleStyle = new GUIStyle(EditorStyles.foldout);
        GUIContent toggleContent = new GUIContent("Search BlendShapes", EditorGUIUtility.FindTexture("ArrowRight"));
        Rect toggleRect = GUILayoutUtility.GetRect(toggleContent, toggleStyle);
        if (GUI.Button(toggleRect, toggleContent, toggleStyle))
        {
            showBox["blendshapes"] = !showBox["blendshapes"];
        }
        CollectBlendShapes();

        if (showBox["blendshapes"])
        {
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

                    // Display blendshape name label
                    Rect nameLabelRect = EditorGUILayout.GetControlRect();
                    EditorGUI.LabelField(nameLabelRect, new GUIContent(blendShapeInfo.Name));

                    // Handle right-click context menu
                    Event currentEvent = Event.current;
                    if (currentEvent.type == EventType.ContextClick && nameLabelRect.Contains(currentEvent.mousePosition))
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

                        menu.AddSeparator("");

                        // Add to MA blend Sync
                        menu.AddItem(new GUIContent("Add to MA blend Sync"), false, () => AddBlendshapeToMASync(blendShapeInfo.Renderer, blendShapeInfo.Index));

                        menu.AddSeparator("");

                        menu.ShowAsContext();
                        currentEvent.Use();
                    }

                    // Display blendshape slider
                    float blendShapeValue = blendShapeInfo.Renderer.GetBlendShapeWeight(blendShapeInfo.Index);
                    float newBlendShapeValue = EditorGUILayout.Slider(blendShapeValue, 0, 100);
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
            Debug.Log($"BlendShape value '{blendShapeValue}' copied to clipboard.");
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
                Undo.RecordObject(renderer, "Paste Blend Shape Value");
                renderer.SetBlendShapeWeight(blendShapeIndex, newValue);
                Debug.Log($"BlendShape value '{newValue}' pasted from clipboard.");
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
            Undo.RecordObject(renderer, $"Set Blend Shape {blendShapeIndex} to {value}");
            renderer.SetBlendShapeWeight(blendShapeIndex, value);
        }
    }

    /// <summary>
    /// Finds the avatar descriptor in the parent hierarchy of the specified transform.
    /// </summary>
    /// <param name="startTransform">The transform to start the search from.</param>
    /// <returns>The avatar descriptor GameObject if found; otherwise, <c>null</c>.</returns>
    private GameObject FindAvatarDescriptor(Transform startTransform)
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

    /// <summary>
    ///  Finds the SkinnedMeshRenderer with the specified blendshape name in the parent hierarchy of the specified transform.
    /// </summary>
    /// <param name="parent"> The parent transform to start the search from.</param>
    /// <param name="blendshapeName"> The name of the blendshape to search for.</param> 
    /// <param name="ignoreRenderer"> The SkinnedMeshRenderer to ignore in the search.</param>
    /// <returns> The SkinnedMeshRenderer with the specified blendshape name if found; otherwise, <c>null</c>.</returns>
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

    /// <summary>
    /// Adds a blend shape binding to the ModularAvatarBlendshapeSync component on each selected object.
    /// </summary>
    /// <param name="blendShapeIndex">The index of the blend shape to add to the bindings.</param>
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
}
