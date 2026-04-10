using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using VRC.SDK3.Avatars.Components;
#if MA_EXISTS
using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
#endif

namespace raspichu.inspector_enhancements.editor
{
    public class MergeArmatureEnhancements : EditorWindow
    {
        private const string MergeArmatureKey = "Pichu_MergeArmatureEnhancementsEnabled";
        private static bool enhancementsEnabled = EditorPrefs.GetBool(MergeArmatureKey, true);

        [MenuItem("Tools/Pichu/Options/Enable Merge Armature Enhancements")]
        private static void ToggleMergeArmatureEnhancements()
        {
            enhancementsEnabled = !enhancementsEnabled;
            EditorPrefs.SetBool(MergeArmatureKey, enhancementsEnabled);

            InternalEditorUtility.RepaintAllViews();
        }

        [MenuItem("Tools/Pichu/Options/Enable Merge Armature Enhancements", true)]
        private static bool ToggleMergeArmatureEnhancementsValidation()
        {
            Menu.SetChecked(
                "Tools/Pichu/Options/Enable Merge Armature Enhancements",
                enhancementsEnabled
            );
            return true;
        }

        public static void OnInspectorGUI(UnityEditor.Editor __instance)
        {
            if (!enhancementsEnabled)
                return; // Exit if enhancements are not enabled
            if (__instance.GetType().Name != "MergeArmatureEditor")
                return; // Exit if the editor is not MAEditorBase

            // Add small space and a separator line with title in the middle
            EditorGUILayout.Space(10);

            Rect totalRect = EditorGUILayout.GetControlRect(false, 20);
            GUIStyle labelStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
            };
            GUIContent content = new GUIContent("Pichu Inspector Enhancements");

            float textWidth = labelStyle.CalcSize(content).x + 10;
            float lineWidth = (totalRect.width - textWidth) / 2;

            // Draw lines and title
            Color lineColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            EditorGUI.DrawRect(
                new Rect(totalRect.x, totalRect.y + totalRect.height / 2, lineWidth, 1),
                lineColor
            );
            EditorGUI.LabelField(totalRect, content, labelStyle);
            EditorGUI.DrawRect(
                new Rect(
                    totalRect.x + lineWidth + textWidth,
                    totalRect.y + totalRect.height / 2,
                    lineWidth,
                    1
                ),
                lineColor
            );

            EditorGUILayout.Space(5);

            // Buttons

            // Create button content with tooltip for hover description
            GUIContent buttonContent = new GUIContent(
                "Copy MA Scale Adjuster to Armature",
                "Copies the MA Scale Adjuster components from the avatar to the clothing armature."
            );

            // Add button with hover tooltip
            if (GUILayout.Button(buttonContent))
            {
                CopyAvatarScaleAdjusterToArmature(__instance);
            }
        }

        private static void CopyAvatarScaleAdjusterToArmature(UnityEditor.Editor __instance)
        {
            // Get Merge Target from MAEditorBase
            var editor = __instance.target as ModularAvatarMergeArmature;
            if (editor == null)
                return;

            var instanceTransform = editor.transform;

            // Access the mergeTargetObject from the MergeArmatureEditor
            var target = editor.mergeTargetObject;
            if (target == null)
                return;

            // Get all ModularAvatarScaleAdjuster components on target
            var scaleAdjusterInstance =
                instanceTransform.GetComponentsInChildren<ModularAvatarScaleAdjuster>(true);
            Debug.Log("Found " + scaleAdjusterInstance.Length + " MAScaleAdjusters on instance");
            // Delete all MAScaleAdjusters on instance
            foreach (var scaleAdjuster in scaleAdjusterInstance)
            {
                Undo.DestroyObjectImmediate(scaleAdjuster);
            }

            // target is an armature, need to find all the children with MAScaleAdjuster on it
            var scaleAdjusters = target.GetComponentsInChildren<ModularAvatarScaleAdjuster>(true);
            Debug.Log("Found " + scaleAdjusters.Length + " MAScaleAdjusters");

            // Now the fun part, we need to copy the scale adjusters component from the avatar to the instance armature
            // They need to be on the same position
            // So, if Hips/Spine/Chest1 have a MAScaleAdjuster, we need to find the same bones on the armature and copy the values
            // We can use the bone names to find the corresponding bones on the armature

            foreach (var scaleAdjuster in scaleAdjusters)
            {
                // Get the bone name
                var boneName = scaleAdjuster.transform.name;
                Debug.Log("Bone name: " + boneName);
                // Get the route of that transform relative to target
                var route = GetRoute(scaleAdjuster.transform, target.transform);
                Debug.Log("Route: " + route);

                // Now that we have the route, we can find the corresponding bone on the instance armature
                var instanceBone = instanceTransform.Find(route);
                if (instanceBone == null)
                {
                    continue;
                }

                // Prevent Unity from updating the avatar automatically
                Undo.RecordObject(instanceBone.gameObject, "Copy MAScaleAdjuster");

                // Now we need to copy the values
                var instanceScaleAdjuster = instanceBone.GetComponent<ModularAvatarScaleAdjuster>();
                if (instanceScaleAdjuster != null)
                {
                    // Check if the component have the same values
                    if (instanceScaleAdjuster.Scale == scaleAdjuster.Scale)
                    {
                        continue;
                    }
                    // Destroy the component if it already exists
                    Undo.DestroyObjectImmediate(instanceScaleAdjuster);
                }
                // Create the component
                instanceScaleAdjuster = Undo.AddComponent<ModularAvatarScaleAdjuster>(
                    instanceBone.gameObject
                );

                // Copy the values of Scale from scaleAdjuster to instanceScaleAdjuster
                instanceScaleAdjuster.Scale = scaleAdjuster.Scale;
            }
        }

        private static string GetRoute(Transform transform, Transform endTransform)
        {
            string route = transform.name;
            while (transform.parent != null && transform.parent != endTransform)
            {
                transform = transform.parent;
                route = transform.name + "/" + route;
            }
            return route;
        }
    }
}
