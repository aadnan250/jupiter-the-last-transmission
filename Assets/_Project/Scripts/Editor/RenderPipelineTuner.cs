using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Applies the performance budget for integrated graphics (Intel Iris Xe) to the PC URP asset:
    /// shorter shadow distance, fewer cascades, smaller shadow map, MSAA off.
    /// See docs/preproduction/06-technical-plan.md §6.
    /// </summary>
    public static class RenderPipelineTuner
    {
        const string PcAsset = "Assets/Settings/PC_RPAsset.asset";

        [MenuItem("Jupiter/Apply Iris Xe Render Settings")]
        public static void Apply()
        {
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(PcAsset);
            if (asset == null)
            {
                Debug.LogError($"[RenderPipelineTuner] {PcAsset} not found");
                return;
            }

            var so = new SerializedObject(asset);
            SetFloat(so, "m_ShadowDistance", 30f);
            SetInt(so, "m_ShadowCascadeCount", 2);
            SetInt(so, "m_MainLightShadowmapResolution", 1024);
            SetInt(so, "m_MSAA", 1); // 1 = disabled
            SetFloat(so, "m_RenderScale", 1f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[RenderPipelineTuner] Iris Xe render settings applied.");
        }

        static void SetFloat(SerializedObject so, string name, float v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.floatValue = v; else Debug.LogWarning($"[RenderPipelineTuner] {name} not found");
        }

        static void SetInt(SerializedObject so, string name, int v)
        {
            var p = so.FindProperty(name);
            if (p != null) p.intValue = v; else Debug.LogWarning($"[RenderPipelineTuner] {name} not found");
        }
    }
}
