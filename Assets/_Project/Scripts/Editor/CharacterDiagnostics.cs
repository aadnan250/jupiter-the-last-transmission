using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Jupiter.EditorTools
{
    /// <summary>
    /// Prints height, bounds, material and arm-angle information for each generated character, used to verify
    /// scale and tune the relaxed arm pose. Menu: Jupiter > Diagnose Characters.
    /// </summary>
    public static class CharacterDiagnostics
    {
        [MenuItem("Jupiter/Diagnose Characters")]
        public static void Run()
        {
            var sb = new StringBuilder("[CharacterDiagnostics]\n");
            foreach (var id in new[] { "sam", "ashford", "luca", "mara", "hana" })
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(CharacterModelImporter.ModelPath(id));
                if (asset == null) { sb.AppendLine($"{id}: missing"); continue; }
                var go = Object.Instantiate(asset);
                var renderers = go.GetComponentsInChildren<Renderer>();
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);

                var t = go.GetComponentsInChildren<Transform>();
                Transform F(string n) => t.FirstOrDefault(x => x.name == n);
                var ua = F("upperarm01.L"); var la = F("lowerarm01.L"); var jaw = F("jaw"); var head = F("head");
                string arm = "n/a";
                if (ua != null && la != null)
                {
                    Vector3 dir = (la.position - ua.position).normalized;
                    arm = $"dir {dir:F2}, {Vector3.Angle(dir, Vector3.down):F0} deg from straight down";
                }
                string mats = string.Join(", ", renderers.SelectMany(r => r.sharedMaterials).Where(m => m != null).Select(m => $"{m.name}[{m.shader.name.Split('/').Last()}{(m.IsKeywordEnabled("_ALPHATEST_ON") ? ",clip" : "")}{(m.mainTexture != null ? ",tex" : ",NOTEX")}]").Distinct());
                sb.AppendLine($"{id}: height {b.size.y:F2} m, width {b.size.x:F2}, depth {b.size.z:F2}, feet y {b.min.y:F2}, root rot {go.transform.GetChild(0).localEulerAngles:F0}");
                sb.AppendLine($"   left upper arm: {arm}; jaw {(jaw != null ? "ok" : "MISSING")}; head y {(head != null ? head.position.y.ToString("F2") : "MISSING")}");
                sb.AppendLine($"   upperarm local axes: right {ua?.right:F2} up {ua?.up:F2} fwd {ua?.forward:F2}");
                sb.AppendLine($"   materials: {mats}");
                Object.DestroyImmediate(go);
            }
            Debug.Log(sb.ToString());
        }
    }
}
