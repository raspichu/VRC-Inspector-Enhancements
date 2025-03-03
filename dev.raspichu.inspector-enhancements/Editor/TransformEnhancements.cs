using UnityEditor;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Linq;

namespace raspichu.inspector_enhancements.editor
{
    public class TransformEnhancements : EditorWindow
    {
        private static bool showOperations = true; // State for foldout
        private static bool enhancementsEnabled = true; // Track whether enhancements are enabled
        private static Dictionary<int, Transform> persistentSelectedTransforms = new Dictionary<int, Transform>(); // Dictionary to store persistently selected transforms

        private static float lastSpacing = 0.5f;


        [MenuItem("Tools/Pichu/Enable Transform Enhancements")]
        private static void ToggleTransformEnhancements()
        {
            enhancementsEnabled = !enhancementsEnabled; // Toggle the state
            InternalEditorUtility.RepaintAllViews(); // Refresh the Inspector
        }

        [MenuItem("Tools/Pichu/Enable Transform Enhancements", true)]
        private static bool ToggleTransformEnhancementsValidation()
        {
            Menu.SetChecked("Tools/Pichu/Enable Transform Enhancements", enhancementsEnabled);
            return true; // Always enable the menu item
        }
        public static void OnInspectorGUI(UnityEditor.Editor __instance)
        {
            if (!enhancementsEnabled) return; // Exit if enhancements are not enabled

            // Update persistent selected transforms
            UpdatePersistentSelectedTransforms(__instance.targets);

            // Foldout for operations
            showOperations = EditorGUILayout.Foldout(showOperations, "Operations", true);

            if (showOperations)
            {
                EditorGUILayout.BeginHorizontal();
                // Add the "Reset Transform" button
                if (GUILayout.Button("Reset Transform"))
                {
                    ApplyToPersistentSelectedTransforms(ResetTransform);
                }

                if (persistentSelectedTransforms.Count > 1)
                {
                    // Create a button rect
                    Rect buttonRect = GUILayoutUtility.GetRect(new GUIContent("Distribute"), GUI.skin.button);

                    // Render the button only for left-click
                    if (Event.current.type == EventType.MouseDown && buttonRect.Contains(Event.current.mousePosition))
                    {
                        if (Event.current.button == 0) // Left-click
                        {
                            // Default left-click action
                            DistributeTransforms(
                                persistentSelectedTransforms.Values.OrderBy(t => t.GetSiblingIndex()),
                                lastSpacing
                            );
                            Event.current.Use();
                        }
                        else if (Event.current.button == 1) // Right-click
                        {
                            // Show context menu on right-click
                            ShowContextMenu();
                            Event.current.Use();
                        }
                    }

                    // Draw the button for visuals only (no interaction here)
                    GUI.Button(buttonRect, new GUIContent("Distribute", "Right-click for options"));
                }

                EditorGUILayout.EndHorizontal();

                // Begin horizontal layout for buttons
                EditorGUILayout.BeginHorizontal();

                // Add the "Set 0 Position" button
                if (GUILayout.Button("Set 0 Position"))
                {
                    ApplyToPersistentSelectedTransforms(SetPositionToZero);
                }

                // Add the "Set 0 Rotation" button
                if (GUILayout.Button("Set 0 Rotation"))
                {
                    ApplyToPersistentSelectedTransforms(SetRotationToZero);
                }

                // Add the "Set 1 Scale" button
                if (GUILayout.Button("Set 1 Scale"))
                {
                    ApplyToPersistentSelectedTransforms(SetScaleToOne);
                }

                // End horizontal layout
                EditorGUILayout.EndHorizontal();
            }
        }

        private static void ShowContextMenu()
        {
            GenericMenu menu = new GenericMenu();

             // Sort transforms by hierarchy order
            var sortedTransforms = persistentSelectedTransforms.Values.OrderBy(t => t.GetSiblingIndex());

            // Add options for spacing values
            float[] spacings = { 0.2f, 0.25f, 0.35f, 0.5f, 0.65f, 0.75f, 1f};
            foreach (float spacing in spacings)
            {
                menu.AddItem(
                    new GUIContent($"X+{spacing}"), 
                    spacing == lastSpacing, // Highlight if this is the last used spacing
                    () => {
                    lastSpacing = spacing;
                    DistributeTransforms(sortedTransforms, spacing);
                }
                );
            }

            menu.ShowAsContext();
        }


       private static void DistributeTransforms(IEnumerable<Transform> transforms, float spacing)
        {
            // Group transforms by Z axis (we will ignore small variations in Z for grouping)
            var groupedTransforms = new Dictionary<float, List<Transform>>();
            
            foreach (var transform in transforms)
            {
                // Get the Z position (rounding or ignoring small differences is up to you)
                float zPosition = transform.localPosition.z;
                if (!groupedTransforms.ContainsKey(zPosition))
                {
                    groupedTransforms[zPosition] = new List<Transform>();
                }
                groupedTransforms[zPosition].Add(transform);
            }

            // Now for each Z group, we will distribute the transforms
            foreach (var group in groupedTransforms)
            {
                float currentPositionX = 0f;

                // Loop through each transform in the current Z group
                foreach (var transform in group.Value)
                {
                    Undo.RecordObject(transform, "Distribute Transforms");

                    // We only adjust the X and Y position in this case
                    transform.localPosition = new Vector3(currentPositionX, transform.localPosition.y, transform.localPosition.z);
                    // Move the current X position for the next transform in the same group
                    currentPositionX += spacing;
                }

                // After distributing the transforms in one Z group, move the X position for the next group
                currentPositionX += spacing;
            }
        }

        private static void UpdatePersistentSelectedTransforms(Object[] targets)
        {
            // Only update if the inspector window is not locked
            // Clear the list to ensure it only holds currently selected transforms
            persistentSelectedTransforms.Clear();

            // Add currently selected transforms from the targets array
            foreach (var target in targets)
            {
                if (target is Transform transform) // Check if the target is a Transform
                {
                    // Use the instance ID as the key
                    int instanceId = transform.GetInstanceID();
                    // Add the transform to the dictionary if it's not already present
                    persistentSelectedTransforms[instanceId] = transform;
                }
            }
        }


        private static void ApplyToPersistentSelectedTransforms(System.Action<Transform> action)
        {
            foreach (var transform in persistentSelectedTransforms.Values)
            {
                if (transform != null)
                {
                    Undo.RecordObject(transform, action.Method.Name);
                    action(transform);
                }
            }
        }

        private static void ResetTransform(Transform transform)
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        private static void SetPositionToZero(Transform transform)
        {
            transform.localPosition = Vector3.zero;
        }

        private static void SetRotationToZero(Transform transform)
        {
            transform.localRotation = Quaternion.identity;
        }

        private static void SetScaleToOne(Transform transform)
        {
            transform.localScale = Vector3.one;
            Debug.Log($"Set scale to one for: {transform.name}");
        }
    }
}
