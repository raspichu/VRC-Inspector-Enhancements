#if VIXEN_EXISTS
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;
using System.Collections.Generic;
using Resilience.Vixen.Components;
using HarmonyLib;
using System.Linq;

namespace raspichu.inspector_enhancements.editor
{
    public class VixenEnhancements : EditorWindow
    {
        private static bool showOperations = true; // State for foldout
        private static bool forEach = false;

        private static Dictionary<int, GameObject> persistentSelectedGameObject =
            new Dictionary<int, GameObject>(); // Dictionary to store persistently selected transforms

        private const string VixenEnhancementsKey = "Pichu_VixenEnhancementsEnabled";
        private static bool enhancementsEnabled = EditorPrefs.GetBool(VixenEnhancementsKey, true);

        [MenuItem("Tools/Pichu/Enable Vixen Enhancements")]
        private static void ToggleVixenEnhancements()
        {
            enhancementsEnabled = !enhancementsEnabled;
            EditorPrefs.SetBool(VixenEnhancementsKey, enhancementsEnabled); 

            string status = enhancementsEnabled ? "enabled" : "disabled";
            Debug.Log($"Vixen Enhancements {status}");

            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("Tools/Pichu/Enable Vixen Enhancements", true)]
        private static bool ToggleVixenEnhancementsValidation()
        {
            Menu.SetChecked("Tools/Pichu/Enable Vixen Enhancements", enhancementsEnabled);
            return true; // Siempre habilitado
        }

        public static void OnInspectorGUI(UnityEditor.Editor __instance)
        {
            if (!enhancementsEnabled)
                return; // Exit if enhancements are not enabled

            UpdatePersistentSelectedTransforms(__instance.targets);

            // Check if any selected GameObject already has a VixenControl component
            bool hasVixenControl = persistentSelectedGameObject.Values.All(go =>
                go.GetComponent<VixenControl>() != null
            );
            if (persistentSelectedGameObject.Count == 0 || hasVixenControl)
                return;

            // Foldout for operations
            showOperations = EditorGUILayout.Foldout(showOperations, "Vixen", true);

            if (showOperations)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Toggle off"))
                {
                    CreateVixenObjects(true);
                }

                if (GUILayout.Button("Toggle on"))
                {
                    CreateVixenObjects(false);
                }
                EditorGUILayout.EndHorizontal();

                if (persistentSelectedGameObject.Count > 1)
                {
                    forEach = EditorGUILayout.Toggle("For each", forEach);
                }
            }
        }

        private static void UpdatePersistentSelectedTransforms(Object[] targets)
        {
            // Only update if the inspector window is not locked
            // Clear the list to ensure it only holds currently selected transforms
            persistentSelectedGameObject.Clear();

            // Add currently selected transforms from the targets array
            foreach (var target in targets)
            {
                // Get target's game object
                if (target is Transform transform)
                {
                    GameObject gameObject = transform.gameObject;
                    // Use the instance ID as the key
                    int instanceId = gameObject.GetInstanceID();
                    // Add the transform to the dictionary if it's not already present
                    persistentSelectedGameObject[instanceId] = gameObject;
                }
            }
        }

        private static void CreateVixenObjects(bool toggleOn)
        {
            if (persistentSelectedGameObject.Count == 0)
                return;
            string toggleStateName = toggleOn ? "Off" : "On";

            List<GameObject> selectedGameObjects = persistentSelectedGameObject.Values.ToList();
            selectedGameObjects.Reverse();

            GameObject lastSelectedObject = selectedGameObjects.Last();
            List<GameObject> createdObjects = new List<GameObject>();

            if (forEach)
            {
                for (int i = 0; i < selectedGameObjects.Count; i++)
                {
                    string name = $"{selectedGameObjects[i].name} - {toggleStateName}";

                    lastSelectedObject =
                        selectedGameObjects.Count > 0
                            ? selectedGameObjects.Last()
                            : lastSelectedObject;
                    GameObject newObject = createVixenFrom(
                        name,
                        new GameObject[] { selectedGameObjects[i] },
                        toggleOn,
                        lastSelectedObject.transform
                    );

                    createdObjects.Add(newObject);
                }
            }
            else
            {
                string persistentSelectedGameObjectNames = GetNameFrom(
                    selectedGameObjects.ToArray()
                );
                string name = $"{persistentSelectedGameObjectNames} - {toggleStateName}";

                GameObject newObject = createVixenFrom(
                    name,
                    selectedGameObjects.ToArray(),
                    toggleOn,
                    lastSelectedObject.transform
                );

                createdObjects.Add(newObject);
            }

            // Convert the list of created GameObjects to an array
            GameObject[] createdObjectsArray = createdObjects.ToArray();

            // Select all the created GameObjects
            Selection.objects = createdObjectsArray;
        }

        private static GameObject createVixenFrom(
            string name,
            GameObject[] toggleObjects,
            bool toggleOn,
            Transform position
        )
        {
            GameObject newObject = new GameObject(name);
            newObject.transform.SetParent(position.parent, false);
            newObject.transform.SetSiblingIndex(position.transform.GetSiblingIndex() + 1);
            Undo.RegisterCreatedObjectUndo(newObject, "Create " + name);

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

        private static string GetNameFrom(GameObject[] gameObject)
        {
            if (gameObject.Length == 0)
                return "Unknown";

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

#endif
