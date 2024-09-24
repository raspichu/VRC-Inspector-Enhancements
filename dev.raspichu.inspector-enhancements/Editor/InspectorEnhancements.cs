using UnityEditor;
using UnityEngine;
using HarmonyLib;
namespace raspichu.inspector_enhancements.editor
{
    public class InspectorEnhancements : EditorWindow
    {

        [InitializeOnLoadMethod]
        static void MyInspectorEnhancements()
        {
            // Harmony patching for SkinnedMeshRendererEditor
            var harmonySkinned = new Harmony("raspichu.inspector_enhancements.editor_skinnedmesh_harmony");
            var targetTypeSkinned = AccessTools.TypeByName("UnityEditor.SkinnedMeshRendererEditor");
            var onBlendShapeUISkinned = AccessTools.Method(targetTypeSkinned, "OnBlendShapeUI");

            harmonySkinned.Patch(onBlendShapeUISkinned, new(typeof(SkinnedMeshEnhancements), nameof(SkinnedMeshEnhancements.OnBlendShapeUI)));
            AssemblyReloadEvents.beforeAssemblyReload += () => harmonySkinned.UnpatchAll();

            // Harmony patching for TransformEnhancements
            var harmonyTransform = new Harmony("raspichu.inspector_enhancements.editor_transform_harmony");
            var targetTypeTransform = AccessTools.TypeByName("UnityEditor.TransformInspector");
            var onInspectorGUITransform = AccessTools.Method(targetTypeTransform, "OnInspectorGUI");

            harmonyTransform.Patch(onInspectorGUITransform, null, new(typeof(TransformEnhancements), nameof(TransformEnhancements.OnInspectorGUI)));
            AssemblyReloadEvents.beforeAssemblyReload += () => harmonyTransform.UnpatchAll();

#if VIXEN_EXISTS
            // Harmony patching for VixenEnhancements
            var harmonyVixen = new Harmony("raspichu.inspector_enhancements.editor_vixen_harmony");
            var targetTypeVixen = AccessTools.TypeByName("UnityEditor.TransformInspector");
            var onInspectorGUIVixen = AccessTools.Method(targetTypeVixen, "OnInspectorGUI");

            harmonyVixen.Patch(onInspectorGUIVixen, null, new(typeof(VixenEnhancements), nameof(VixenEnhancements.OnInspectorGUI)));
            AssemblyReloadEvents.beforeAssemblyReload += () => harmonyVixen.UnpatchAll();
#endif
        }

    }

}