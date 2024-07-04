using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using VRC.SDK3.Dynamics.PhysBone.Components;
using VRC.SDK3.Avatars.Components;

using System.Linq;

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements : EditorWindow
    {
        private GameObject[] selectedObjects;

        private bool showSelectedObjects = false;

        private SkinnedMeshRenderer[] skinnedMeshRenderers;
        private VRCPhysBone[] physBones;

        [MenuItem("Window/Pichu/Inspector Enhancements")]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(InspectorEnhancements), false, "Inspector Enhancements");
        }

        private void OnSelectionChange()
        {
            selectedObjects = Selection.gameObjects;
            skinnedMeshRenderers = GetSkinnedMeshRenderers(selectedObjects);
            physBones = GetPhysBones(selectedObjects);
            Repaint();
        }

        private void OnGUI()
        {
            if (selectedObjects == null || selectedObjects.Length == 0) return;

            DrawSelectedGameObjects(selectedObjects);

            DrawTransformOperationsSection();


#if VIXEN_EXISTS
            DrawVixenOperationsSection();
#endif

            if (skinnedMeshRenderers.Length != selectedObjects.Length || (physBones != null && physBones.Length > 0))
                DrawPhysBoneOperationsSection();
            if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
                DrawSkinnedMeshOperationsSection();



        }

        private void DrawSelectedGameObjects(GameObject[] objects)
        {
            if (objects != null && objects.Length > 0)
            {
                EditorGUILayout.BeginHorizontal();
                objects[0] = EditorGUILayout.ObjectField("Selected", objects[0], typeof(GameObject), true) as GameObject;

                if (objects.Length > 1)
                {
                    string label = $"+ {objects.Length - 1} more";
                    GUILayout.Label(label);

                    Rect labelRect = GUILayoutUtility.GetLastRect();
                    EditorGUIUtility.AddCursorRect(labelRect, MouseCursor.Link);

                    if (Event.current.type == EventType.MouseDown && labelRect.Contains(Event.current.mousePosition))
                    {
                        showSelectedObjects = !showSelectedObjects;
                        Event.current.Use();
                    }
                }
                EditorGUILayout.EndHorizontal();

                if (objects.Length > 1 && showSelectedObjects)
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
            return objects.Select(obj => obj.GetComponent<SkinnedMeshRenderer>()).Where(renderer => renderer != null).ToArray();
        }

        private VRCPhysBone[] GetPhysBones(GameObject[] objects)
        {
            return objects.Select(obj => obj.GetComponent<VRCPhysBone>()).Where(physBone => physBone != null).ToArray();
        }

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
    }
}