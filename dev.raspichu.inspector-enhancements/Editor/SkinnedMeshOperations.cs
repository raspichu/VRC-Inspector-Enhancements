using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {
        private bool showSkinnedMeshOperations = false;
        private bool showBlendShapes = false;

        private string blendShapeSearch = "";
        private Vector2 blendShapeScrollPosition = Vector2.zero;

        private void DrawSkinnedMeshOperationsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIContent skinnedMeshOperationsContent = new GUIContent("Skinned Mesh Operations", EditorGUIUtility.FindTexture("ArrowRight"));
            Rect skinnedMeshOperationsToggleRect = GUILayoutUtility.GetRect(skinnedMeshOperationsContent, EditorStyles.foldout);
            if (GUI.Button(skinnedMeshOperationsToggleRect, skinnedMeshOperationsContent, EditorStyles.foldout))
            {
                showSkinnedMeshOperations = !showSkinnedMeshOperations;
            }

            if (showSkinnedMeshOperations)
            {
                GUIContent centerBoundsContent = new GUIContent("Center Bounds", "Set the local bounds of the SkinnedMeshRenderer to center (0, 0, 0) with an extend of (1, 1, 1).");
                if (GUILayout.Button(centerBoundsContent))
                {
                    Undo.RecordObjects(skinnedMeshRenderers, "Center Bounds");
                    foreach (SkinnedMeshRenderer renderer in skinnedMeshRenderers)
                    {
                        renderer.localBounds = new Bounds(Vector3.zero, Vector3.one * 2);
                    }
                }
                DrawBlendShapesSection();
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawBlendShapesSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            showBlendShapes = EditorGUILayout.Foldout(showBlendShapes, "BlendShapes");

            if (showBlendShapes)
            {
                blendShapeSearch = EditorGUILayout.TextField("Search", blendShapeSearch);
                blendShapeScrollPosition = EditorGUILayout.BeginScrollView(blendShapeScrollPosition);

                foreach (var renderer in skinnedMeshRenderers)
                {
                    for (int i = 0; i < renderer.sharedMesh.blendShapeCount; i++)
                    {
                        string blendShapeName = renderer.sharedMesh.GetBlendShapeName(i);
                        if (string.IsNullOrEmpty(blendShapeSearch) || blendShapeName.ToLower().Contains(blendShapeSearch.ToLower()))
                        {
                            EditorGUILayout.BeginHorizontal();
                            GUILayout.Label(blendShapeName);
                            float value = EditorGUILayout.Slider(renderer.GetBlendShapeWeight(i), 0f, 100f);
                            renderer.SetBlendShapeWeight(i, value);
                            EditorGUILayout.EndHorizontal();
                        }
                    }
                }

                EditorGUILayout.EndScrollView();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
