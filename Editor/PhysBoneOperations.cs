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
        private enum PhysBoneSetup
        {
            Soft,
            Default,
            Hard
        }
        private PhysBoneSetup selectedPhysBoneSetup = PhysBoneSetup.Default;
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
                    CreatePhysBones();
                }
                EditorGUILayout.EndHorizontal();


                if (allHavePhysBone)
                {
                    if (GUILayout.Button("Set Floor Collider"))
                    {
                        SetFloorCollider();
                    }

                }

            }

            EditorGUILayout.EndVertical();
        }

        private void CreatePhysBones()
        {
            if (selectedObjects.Length == 0) return;

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

        private void SetFloorCollider()
        {

            // Search for a Floor_Collider gameObject in the root of the avatar
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject obj = selectedObjects[i];
                VRCPhysBone physBoneComponent = obj.GetComponent<VRCPhysBone>();
                if (physBoneComponent == null) continue;


                GameObject avatar = FindAvatarDescriptor(obj.transform);
                if (avatar == null)
                {
                    Debug.LogWarning("No Avatar Descriptor found in the hierarchy of the selected GameObjects.");
                    continue;
                }

                GameObject floorColliderObject = avatar.transform.Find("IE_Floor_Collider")?.gameObject;
                if (floorColliderObject == null)
                {
                    floorColliderObject = new GameObject("IE_Floor_Collider");
                    floorColliderObject.transform.SetParent(avatar.transform, false);
                    floorColliderObject.transform.localPosition = Vector3.zero;
                    floorColliderObject.transform.localRotation = Quaternion.identity;
                    floorColliderObject.transform.localScale = Vector3.one;
                    Undo.RegisterCreatedObjectUndo(floorColliderObject, "Create Floor Collider");
                    // Add VRCPhysBoneCollider component to the object
                    VRCPhysBoneCollider physBoneColliderTemp = floorColliderObject.AddComponent<VRCPhysBoneCollider>();

                    // shapeType to Enum:Plane
                    physBoneColliderTemp.shapeType = VRC.Dynamics.VRCPhysBoneColliderBase.ShapeType.Plane;

                }

                VRCPhysBoneCollider physBoneCollider = floorColliderObject.GetComponent<VRCPhysBoneCollider>();

                // If the physbone already has the collider, skip
                if (physBoneComponent.colliders.Contains(physBoneCollider)) continue;

                // Add collider to the physbone
                physBoneComponent.colliders.Add(physBoneCollider);
            }
        }
    }
}


