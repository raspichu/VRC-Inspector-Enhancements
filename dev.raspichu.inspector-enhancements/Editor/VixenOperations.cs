using UnityEditor;
using UnityEngine;
using Resilience.Vixen.Components;
using System.Collections.Generic;

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {

        private bool showVixenOperations = false;
        private bool forEach = false;

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
                    CreateVixenObjects(true);
                }

                if (GUILayout.Button("Toggle on"))
                {
                    CreateVixenObjects(false);
                }
                EditorGUILayout.EndHorizontal(); // End horizontal layout

                if (selectedObjects.Length > 1)
                {
                    forEach = EditorGUILayout.Toggle("For each", forEach);
                    // tooltip
                    EditorGUILayout.HelpBox("If enabled, it will create a toggle for each selected object. If disabled, one toggle for all", MessageType.Info);
                }
            }

            EditorGUILayout.EndVertical();
        }

        private void CreateVixenObjects(bool toggleOn)
        {
            if (selectedObjects.Length == 0) return;

            string toggleStateName = toggleOn ? "Off" : "On";

            GameObject lastSelectedObject = selectedObjects[selectedObjects.Length - 1];

            // Create a list to hold all created GameObjects
            List<GameObject> createdObjects = new List<GameObject>();

            if (forEach)
            {
                for (int i = 0; i < selectedObjects.Length; i++)
                {
                    string name = $"{selectedObjects[i].name} - {toggleStateName}";

                    GameObject newObject = createVixenFrom(name, new GameObject[] { selectedObjects[i] }, toggleOn, lastSelectedObject.transform);

                    createdObjects.Add(newObject);
                }
            }
            else
            {
                string selectedObjectsNames = GetNameFrom(selectedObjects);
                string name = $"{selectedObjectsNames} - {toggleStateName}";

                GameObject newObject = createVixenFrom(name, selectedObjects, toggleOn, lastSelectedObject.transform);

                createdObjects.Add(newObject);

            }

            // Convert the list of created GameObjects to an array
            GameObject[] createdObjectsArray = createdObjects.ToArray();

            // Select all the created GameObjects
            Selection.objects = createdObjectsArray;
        }

        private GameObject createVixenFrom(string name, GameObject[] toggleObjects, bool toggleOn, Transform position)
        {
            GameObject newObject = new GameObject(name);
            newObject.transform.SetParent(position.parent, false);
            newObject.transform.SetSiblingIndex(position.transform.GetSiblingIndex() + 1);

            VixenControl vixenControl = newObject.AddComponent<VixenControl>();

            List<Component> componentsToAdd = new List<Component>();
            foreach (var iObject in toggleObjects)
            {
                if (iObject != null)
                {
                    componentsToAdd.Add(iObject.transform);
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

            return newObject;
        }

        private string GetNameFrom(GameObject[] gameObject)
        {
            if (gameObject.Length == 0) return "Unknown";

            List<string> names = new List<string>();
            foreach (var selectedObject in gameObject)
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