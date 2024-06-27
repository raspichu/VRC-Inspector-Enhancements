using UnityEditor;
using UnityEngine;

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {
        private bool showTransformOperations = false;
        private void DrawTransformOperationsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUIContent transformOperationsContent = new GUIContent("Transform Operations", EditorGUIUtility.FindTexture("ArrowRight"));
            Rect transformOperationsToggleRect = GUILayoutUtility.GetRect(transformOperationsContent, EditorStyles.foldout);
            if (GUI.Button(transformOperationsToggleRect, transformOperationsContent, EditorStyles.foldout))
            {
                showTransformOperations = !showTransformOperations;
            }

            if (showTransformOperations)
            {
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
            }
            EditorGUILayout.EndVertical();
        }
    }
}
