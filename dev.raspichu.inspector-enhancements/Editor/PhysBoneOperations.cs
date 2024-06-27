using UnityEditor;
using UnityEngine;
using VRC.SDK3.Dynamics.PhysBone.Components;
using System.Linq;
#if MA_EXISTS
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

namespace raspichu.inspector_enhancements.editor
{
    public partial class InspectorEnhancements
    {
        private bool showPhysBoneOperations = false;
        private void DrawPhysBoneOperationsSection()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            GUIContent physBonesOperationsContent = new GUIContent("PhysBones Operations", EditorGUIUtility.FindTexture("ArrowRight"));
            Rect physBonesOperationsToggleRect = GUILayoutUtility.GetRect(physBonesOperationsContent, EditorStyles.foldout);
            if (GUI.Button(physBonesOperationsToggleRect, physBonesOperationsContent, EditorStyles.foldout))
            {
                showPhysBoneOperations = !showPhysBoneOperations;
            }

            if (showPhysBoneOperations)
            {
                bool allHavePhysBone = selectedObjects.All(obj => obj.GetComponent<VRCPhysBone>() != null);
                EditorGUILayout.BeginHorizontal();

                GUIContent forcesLabel = new GUIContent("Forces");
                float labelWidth = GUI.skin.label.CalcSize(forcesLabel).x; // Calculate the width needed for the label text
                EditorGUILayout.LabelField(forcesLabel, GUILayout.Width(labelWidth));
                
                selectedPhysBoneSetup = (PhysBoneSetup)EditorGUILayout.EnumPopup(selectedPhysBoneSetup);

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
                        switch (selectedPhysBoneSetup)
                        {
                            case PhysBoneSetup.Soft:
                                physBoneComponent.pull = 0.05f;
                                physBoneComponent.spring = 0.95f;
                                physBoneComponent.stiffness = 0.05f;
                                break;
                            case PhysBoneSetup.Default:
                                physBoneComponent.pull = 0.2f;
                                physBoneComponent.spring = 0.8f;
                                physBoneComponent.stiffness = 0.5f;
                                break;
                            case PhysBoneSetup.Hard:
                                physBoneComponent.pull = 0.4f;
                                physBoneComponent.spring = 0.6f;
                                physBoneComponent.stiffness = 0.8f;
                                break;
                        }
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndVertical();
        }
    }
}
