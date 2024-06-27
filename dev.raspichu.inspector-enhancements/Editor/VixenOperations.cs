#if VIXEN_EXISTS
using UnityEditor;
using UnityEngine;
using Resilience.Vixen.Components;
using System.Collections.Generic;

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {

        private bool showVixenOperations = false;

        private void DrawVixenOperationsSection()
        {
            // If there's only 1 selection and has VixenControl component, show Vixen Operations section
            if (selectedObjects.Length == 1 && selectedObjects[0].GetComponent<VixenControl>()) return;

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIContent vixenOperationsContent = new GUIContent("Vixen Operations", EditorGUIUtility.FindTexture("ArrowRight"));
            Rect vixenOperationsToggleRect = GUILayoutUtility.GetRect(vixenOperationsContent, EditorStyles.foldout);
            if (GUI.Button(vixenOperationsToggleRect, vixenOperationsContent, EditorStyles.foldout))
            {
                showVixenOperations = !showVixenOperations;
            }

            if (showVixenOperations)
            {
                EditorGUILayout.BeginHorizontal(); // Begin horizontal layout
                if (GUILayout.Button("Toggle off"))
                {
                    CreateVixenObject(true);
                }

                if (GUILayout.Button("Toggle on"))
                {
                    CreateVixenObject(false);
                }
                EditorGUILayout.EndHorizontal(); // End horizontal layout
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateVixenObject(bool toggleOn)
        {
            if (selectedObjects.Length == 0) return;

            string toggleStateName = toggleOn ? "Off" : "On";
            string selectedObjectsNames = GetSelectedObjectsNames();

            string name = $"{selectedObjectsNames} - {toggleStateName}";

            GameObject newObject = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(newObject, "Create " + name);

            Transform parentTransform = selectedObjects[selectedObjects.Length - 1].transform;
            newObject.transform.SetParent(parentTransform.parent, false);
            newObject.transform.SetSiblingIndex(parentTransform.GetSiblingIndex() + 1);

            // Add the VixenControl script to the new GameObject
            VixenControl vixenControl = newObject.AddComponent<VixenControl>();

            List<Component> componentsToAdd = new List<Component>();

            foreach (var selectedObject in selectedObjects)
            {
                if (selectedObject != null)
                {
                    componentsToAdd.Add(selectedObject.transform);
                }
            }

            if (toggleOn)
            {
                vixenControl.whenInactive = componentsToAdd.ToArray();
            }
            else
            {
                vixenControl.whenActive = componentsToAdd.ToArray();
            }


            Debug.Log($"Created {name} GameObject with VixenControl script");

            // Select new GameObject
            Selection.activeGameObject = newObject;
        }

        private string GetSelectedObjectsNames()
        {
            if (selectedObjects.Length == 0) return "Unknown";

            List<string> names = new List<string>();
            foreach (var selectedObject in selectedObjects)
            {
                if (selectedObject != null)
                {
                    names.Add(selectedObject.name);
                }
            }

            return string.Join("|", names);
        }
    }
}
#endif
