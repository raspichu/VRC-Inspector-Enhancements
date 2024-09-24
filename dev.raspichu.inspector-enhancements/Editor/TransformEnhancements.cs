using UnityEditor;
using UnityEngine;
using HarmonyLib;
using System.Collections.Generic;
using UnityEditorInternal;

namespace raspichu.inspector_enhancements.editor
{
    public class TransformEnhancements : EditorWindow
    {
        private static bool showOperations = true; // State for foldout
        private static bool enhancementsEnabled = true; // Track whether enhancements are enabled
        private static Dictionary<int, Transform> persistentSelectedTransforms = new Dictionary<int, Transform>(); // Dictionary to store persistently selected transforms


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
                // Add the "Reset Transform" button
                if (GUILayout.Button("Reset Transform"))
                {
                    ApplyToPersistentSelectedTransforms(ResetTransform);
                }

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
